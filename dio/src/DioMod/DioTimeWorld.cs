using System;
using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Playables;
using System.Reflection;

namespace EyesOfHeavenDio
{
    public sealed class DioTimeStopExempt : MonoBehaviour {}

    // Server-authoritative suspension; player/allied bodies remain free to act.
    // Never applies Frozen or changes global timeScale.
    [DefaultExecutionOrder(32000)]
    public sealed class DioTimeWorld : MonoBehaviour
    {
        public static DioTimeWorld Active { get; private set; }
        private static readonly FieldInfo timerAge = typeof(DestroyOnTimer).GetField("age", BindingFlags.Instance | BindingFlags.NonPublic);
        private CharacterBody owner;
        private float ends, nextScan;
        private int effectFamily;
        private double scanMilliseconds;
        private int scanCount;
        private readonly HashSet<GameObject> capturedRoots = new HashSet<GameObject>();
        private readonly HashSet<Transform> exemptRoots = new HashSet<Transform>();
        private readonly List<ProjectileController> projectiles = new List<ProjectileController>();
        private float resumeCue;
        private bool endingStarted;
        private DioAudioPlayback endingAudio;
        private readonly HashSet<UnityEngine.Object> captured = new HashSet<UnityEngine.Object>();
        private readonly List<Action> restore = new List<Action>();
        private readonly Dictionary<Transform, Pose> positions = new Dictionary<Transform, Pose>();
        public static void Begin(CharacterBody body, float seconds)
        {
            if (Active) Active.Resume();
            var world = body.gameObject.AddComponent<DioTimeWorld>();
            Active = world; world.owner = body; world.ends = Time.time + seconds;
            body.GetComponent<DioNetworkPresentation>()?.ServerTimeStop(true,seconds);
            world.resumeCue = Mathf.Min(DioPlugin.TimeResumeTickTime.Value, seconds);
            if (Run.instance)
            {
                var run = Run.instance;
                bool paused = run.NetworkisRunStopwatchForcePaused;
                run.SetForcePauseRunStopwatch(true);
                world.restore.Add(() => { if (run) run.SetForcePauseRunStopwatch(paused); });
            }
            world.ScanActors();
            world.ScanEffects(-1);
            world.nextScan = Time.time + .05f;
            DioPlugin.Log.LogInfo("Time stop started: " + seconds + "s; suspended objects=" + world.captured.Count);
        }
        internal static bool RestoreAfterStop(GameObject root, Action cleanup)
        {
            if (!Active || !Active.capturedRoots.Contains(root)) return false;
            // Insert first: restoration runs backwards, so pre-grab state wins last.
            Active.restore.Insert(0, cleanup);
            return true;
        }
        internal static void UpdateHeldPosition(Transform target)
        {
            if (Active && Active.positions.ContainsKey(target))
                Active.positions[target] = new Pose(target.position, target.rotation);
        }
        private bool IsAlly(CharacterBody body)
            => body && (body == owner || body.isPlayerControlled ||
                (body.teamComponent && owner.teamComponent && body.teamComponent.teamIndex == owner.teamComponent.teamIndex));
        internal static bool StopsProjectileOf(GameObject shooter)
        {
            if (!Active) return false;
            var body = shooter ? shooter.GetComponent<CharacterBody>() : null;
            return !body || body == Active.owner || !Active.IsAlly(body);
        }
        internal static void MarkRollerEffect(GameObject effect)
        {
            foreach(var body in CharacterBody.readOnlyInstancesList)
                if(body && body.GetComponent<DioExtraMoves>()?.RoadRollerActive==true)
                {effect.AddComponent<DioTimeStopExempt>();return;}
        }
        private bool IsExempt(Component component)
        {
            for (var current = component.transform; current; current = current.parent)
                if (exemptRoots.Contains(current) || current.GetComponent<DioTimeStopExempt>()) return true;
            return false;
        }
        private void Pause(Behaviour component)
        {
            if (!component || !component.enabled || !captured.Add(component)) return;
            component.enabled = false;
            restore.Add(() => { if (component) component.enabled = true; });
        }
        private void CaptureRoot(GameObject root, bool projectile)
        {
            if (!root || IsExempt(root.transform) || !capturedRoots.Add(root)) return;
            if (!positions.ContainsKey(root.transform)) positions.Add(root.transform, new Pose(root.transform.position, root.transform.rotation));
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string ns = behaviour.GetType().Namespace ?? "";
                if (behaviour is EntityStateMachine || behaviour is CharacterMotor || behaviour is CharacterDirection ||
                    behaviour is RigidbodyMotor || ns == "KinematicCharacterController" ||
                    (projectile && ns.StartsWith("RoR2.Projectile"))) Pause(behaviour);
            }
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (!captured.Add(rb)) continue;
                bool kinematic = rb.isKinematic; var velocity = rb.velocity; var angular = rb.angularVelocity;
                rb.isKinematic = true;
                restore.Add(() => { if (rb) { rb.isKinematic = kinematic; if (!kinematic) { rb.velocity = velocity; rb.angularVelocity = angular; } } });
            }
            // Frozen projectiles must not collide with DIO while he moves through them.
            if (projectile) foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled || !captured.Add(collider)) continue;
                collider.enabled = false; restore.Add(() => { if (collider) collider.enabled = true; });
            }
        }
        private void ScanActors()
        {
            // Body/instance registries avoid allocating scene-wide search results.
            // Mark every ally first, including models detached from their body root.
            foreach (var body in CharacterBody.readOnlyInstancesList)
            {
                if (!IsAlly(body)) continue;
                exemptRoots.Add(body.transform);
                if (body.modelLocator && body.modelLocator.modelTransform) exemptRoots.Add(body.modelLocator.modelTransform);
            }
            foreach (var body in CharacterBody.readOnlyInstancesList)
            {
                if (!body || IsAlly(body) || !body.healthComponent || !body.healthComponent.alive || capturedRoots.Contains(body.gameObject)) continue;
                CaptureRoot(body.gameObject, false);
                if (body.master) foreach (var ai in body.master.GetComponents<RoR2.CharacterAI.BaseAI>()) Pause(ai);
            }
            // CaptureRoot disables controllers and changes the instance registry:
            // snapshot into a reused list before iterating it.
            projectiles.Clear();
            projectiles.AddRange(InstanceTracker.GetInstancesList<ProjectileController>());
            foreach (var projectile in projectiles)
            {
                if (!projectile) continue;
                var shooter = projectile.owner ? projectile.owner.GetComponent<CharacterBody>() : null;
                if (shooter && shooter != owner && IsAlly(shooter))
                {
                    exemptRoots.Add(projectile.transform);
                    if (projectile.ghost) exemptRoots.Add(projectile.ghost.transform);
                    continue;
                }
                CaptureRoot(projectile.gameObject, true);
            }
        }
        private void ScanEffects(int family)
        {
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            if (family < 0 || family == 0) foreach (var director in FindObjectsOfType<CombatDirector>()) Pause(director);
            if (family < 0 || family == 1) foreach (var particle in FindObjectsOfType<ParticleSystem>())
            {
                if (captured.Contains(particle) || !particle.isPlaying || IsExempt(particle)) continue;
                captured.Add(particle);
                particle.Pause(false); restore.Add(() => { if (particle) particle.Play(false); });
            }
            if (family < 0 || family == 2) foreach (var effect in FindObjectsOfType<VisualEffect>())
            {
                if (captured.Contains(effect) || effect.pause || IsExempt(effect)) continue;
                captured.Add(effect);
                effect.pause = true; restore.Add(() => { if (effect) effect.pause = false; });
            }
            if (family < 0 || family == 3) foreach (var animator in FindObjectsOfType<Animator>())
            {
                if (captured.Contains(animator) || IsExempt(animator)) continue;
                captured.Add(animator);
                float speed = animator.speed; animator.speed = 0;
                restore.Add(() => { if (animator) animator.speed = speed; });
            }
            if (family < 0 || family == 4) foreach (var animation in FindObjectsOfType<Animation>())
            {
                if (captured.Contains(animation) || IsExempt(animation)) continue;
                captured.Add(animation);
                foreach (AnimationState state in animation)
                {
                    var saved = state; float speed = state.speed; state.speed = 0;
                    restore.Add(() => { if (animation) saved.speed = speed; });
                }
            }
            // Preserve effect lifetimes as well as particle simulation.
            if (family < 0 || family == 5) foreach (var timer in FindObjectsOfType<DestroyOnTimer>())
            {
                if (captured.Contains(timer) || !timer.enabled || IsExempt(timer)) continue;
                float age = timer.GetAge(); Pause(timer);
                timerAge?.SetValue(timer, age);
            }
            if (family < 0 || family == 6) foreach (var trail in FindObjectsOfType<TrailRenderer>())
            {
                if (captured.Contains(trail) || IsExempt(trail)) continue;
                captured.Add(trail);
                float lifetime = trail.time; bool emitting = trail.emitting;
                trail.time = 100000; trail.emitting = false;
                restore.Add(() => { if (trail) { trail.time = lifetime; trail.emitting = emitting; } });
            }
            if (family < 0 || family == 7) foreach (var director in FindObjectsOfType<PlayableDirector>())
            {
                if (captured.Contains(director) || director.state != PlayState.Playing || IsExempt(director)) continue;
                captured.Add(director);
                director.Pause(); restore.Add(() => { if (director) director.Resume(); });
            }
            scanMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            scanCount++;
        }
        private void FixedUpdate()
        {
            if (owner && owner.healthComponent && owner.healthComponent.alive) ScanActors();
        }
        private void Update()
        {
            if (!owner || !owner.healthComponent || !owner.healthComponent.alive) { Resume(); return; }
            // Play the spoken lead-in while time is still stopped. The device clock,
            // not the start of the recording, determines the actual resume moment.
            if (!endingStarted && Time.time >= ends - resumeCue - .15f)
            {
                endingStarted = true;
                endingAudio = DioFeedback.ServerSound(owner.gameObject, "time-resume", 1f);
            }
            if (endingStarted && endingAudio && endingAudio.PlaybackSeconds >= resumeCue)
            {
                DioPlugin.Log.LogInfo("Time resume tick cue: audio=" + endingAudio.PlaybackSeconds + " target=" + resumeCue);
                Resume(); return;
            }
            // Missing audio or a failed device must not leave the world suspended.
            if ((!endingAudio && Time.time >= ends) || Time.time >= ends) { Resume(); return; }
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + .05f;
                ScanEffects(effectFamily);
                effectFamily = (effectFamily + 1) % 8;
            }
        }
        private void LateUpdate()
        {
            foreach (var pair in positions) if (pair.Key && (pair.Key.position != pair.Value.position || pair.Key.rotation != pair.Value.rotation))
                pair.Key.SetPositionAndRotation(pair.Value.position, pair.Value.rotation);
        }
        private void Resume()
        {
            if (Active != this) return;
            Active = null;
            for (int i = restore.Count - 1; i >= 0; i--) try { restore[i](); } catch (Exception e) { DioPlugin.Log.LogWarning("Time resume: " + e.Message); }
            restore.Clear(); positions.Clear(); capturedRoots.Clear(); exemptRoots.Clear(); projectiles.Clear();
            if (owner) { owner.GetComponent<DioNetworkPresentation>()?.ServerTimeStop(false); DioFeedback.MainTimeTransition(true, DioInput.IsLocal(owner)); }
            DioPlugin.Log.LogInfo("Time resumed; restored " + captured.Count + " objects. Effect scans=" + scanCount + " totalMs=" + scanMilliseconds.ToString("F2"));
            Destroy(this);
        }
        private void OnDestroy() { Resume(); }
        private void OnDisable() { Resume(); }
    }
}
