using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using LocalStandCombat;

namespace EyesOfHeavenDio
{
    public sealed class DioComboTracker : MonoBehaviour
    {
        private int step;
        private float chainUntil = -10;
        public void ContinueWindow(float duration) => chainUntil = Time.time + duration + .8f;
        public int Next()
        {
            if (Time.time > chainUntil) step = 0;
            int result = step;
            step = (step + 1) % 5;
            return result;
        }
    }
    public sealed class DioPunch : BaseSkillState
    {
        private float duration;
        private int step;
        private bool fired;
        private float swingAge, pauseRemaining;
        private bool impactFeedback;
        private GameObject hitVolume;
        private OverlapAttack attack;
        private StandPrimaryController standPrimary;
        private HurtBoxReference lockedTarget;
        private bool targeted;
        private Vector3 punchDirection;
        private readonly StandAttackClock clock = new StandAttackClock();
        private int visualStep;
        // Normalized hand-extension windows sampled from the imported EOH clips.
        private static readonly float[] ContactStart = { .15f, .20f, .23f, .13f, .37f };
        private static readonly float[] ContactEnd = { .25f, .30f, .34f, .30f, .47f };
        private static readonly float[] ChainPoint = { .52f, .55f, .58f, .56f, .70f };
        public override void OnEnter()
        {
            base.OnEnter();
            var combo = GetComponent<DioComboTracker>();
            standPrimary = StandPrimaryController.Of(gameObject);
            if (isAuthority) {
                step = combo.Next();
                punchDirection = GetAimRay().direction;
                HurtBox target = standPrimary.Acquire(GetAimRay());
                targeted = target;
                lockedTarget = HurtBoxReference.FromHurtBox(target);
            }
            step = Mathf.Clamp(step, 0, 4);
            visualStep = targeted && step < 2 ? step + 2 : step;
            duration = DioPlugin.MoveDuration(new[] { 1.1f, 1.15f, 1.25f, 1.3f, 1.6f }[step], Mathf.Min(1.25f, Mathf.Sqrt(Mathf.Max(.1f, attackSpeedStat))));
            combo.ContinueWindow(duration);
            if (targeted) standPrimary.Begin(lockedTarget.ResolveHurtBox(), punchDirection, duration * ContactStart[visualStep]);
            if(targeted)GetComponent<DioVisualDriver>()?.IndependentPrimary(duration,step);
            else {
                GetComponent<DioVisualDriver>()?.Action("_atk_l" + (step + 1), duration, step >= 2);
                if(step>=2)GetComponent<DioVisualDriver>()?.StandPunch(duration,step);
            }
            if (!targeted) StartAimMode(1f);
            if (isAuthority)
            {
                hitVolume = new GameObject("DioMeleeVolume");
                var group = hitVolume.AddComponent<HitBoxGroup>();
                group.groupName = "DioPunch";
                group.hitBoxes = new[] { hitVolume.AddComponent<HitBox>() };
                attack = new OverlapAttack {
                    attacker = gameObject, inflictor = gameObject, teamIndex = GetTeam(),
                    hitBoxGroup = group, damage = damageStat * (step == 4 ? 7f : 2.8f),
                    pushAwayForce = step == 4 ? 1400f : 120f,
                    forceVector = step == 4 ? Vector3.up * 500f : Vector3.zero,
                    isCrit = RollCrit(), procCoefficient = 1f,
                    hitEffectPrefab = DioContent.PunchImpact,
                    attackerFiltering = AttackerFiltering.CustomFilter,
                    filterCallback = target => {
                        var health = target.healthComponent;
                        if (!health || !health.alive || health == characterBody.healthComponent || !health.body ||
                            !health.body.teamComponent || health.body.teamComponent.teamIndex == GetTeam()) return false;
                        if (targeted) return standPrimary.CanHit(health);
                        return !Physics.Linecast(characterBody.corePosition, health.body.corePosition,
                            LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
                    }
                };
            }
            DioPlugin.Log.LogInfo("M1 begin step=" + (step + 1) + " duration=" + duration + " time=" + Time.time);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && inputBank && !inputBank.skill1.down) { outer.SetNextStateToMain(); return; }
            if (targeted) standPrimary.Tick(Time.fixedDeltaTime);
            if (pauseRemaining > 0) {
                pauseRemaining = Mathf.Max(0, pauseRemaining - Time.fixedDeltaTime);
                if(targeted)GetComponent<DioVisualDriver>()?.PrimaryTime(swingAge,swingAge);
                return;
            }
            float previousAge = swingAge;
            swingAge = clock.Advance(Time.fixedDeltaTime, duration * ContactStart[visualStep], targeted && standPrimary.Active, targeted && standPrimary.Arrived);
            if (targeted) GetComponent<DioVisualDriver>()?.PrimaryTime(previousAge,swingAge);
            if (clock.TimedOut) { standPrimary.Release(); if(isAuthority)outer.SetNextStateToMain(); return; }
            if(!targeted && step>=2)
            {
                float phase=swingAge/duration;
                float alignment=phase<ContactStart[step]?Mathf.SmoothStep(0,1,phase/ContactStart[step]):
                    1f-Mathf.SmoothStep(0,1,(phase-ContactEnd[step])/.2f);
                GetComponent<DioVisualDriver>()?.AimPunch(new Ray(characterBody.corePosition,GetAimRay().direction),alignment);
            }
            if (isAuthority && StandPrimaryRules.CrossesContact(previousAge, swingAge, duration, ContactStart[visualStep], ContactEnd[visualStep]))
            {
                if (!fired) { fired = true; DioFeedback.Sound(gameObject, step >= 2 ? "stand-swing" : "swing", .8f); GetComponent<DioAttackVoice>()?.Play(step==4); }
                Vector3 direction = targeted ? standPrimary.Direction : GetAimRay().direction.normalized;
                Vector3 origin = targeted ? standPrimary.Position : characterBody.corePosition;
                float reach = targeted ? 3.5f : step >= 2 ? 7f : 6f;
                hitVolume.transform.SetPositionAndRotation(origin + direction * (reach * .5f), Util.QuaternionSafeLookRotation(direction));
                hitVolume.transform.localScale = new Vector3(3f, 3f, reach);
                // One OverlapAttack instance retains its hit list across the entire
                // contact window, including multi-hurtbox enemies and hit pause.
                bool hit = (!targeted || standPrimary.Arrived) && attack.Fire();
                if (hit && !impactFeedback)
                {
                    impactFeedback = true;
                    DioFeedback.Impact(origin + direction * (targeted ? 1.3f : 3f), step == 4 ? 1.5f : .8f);
                    // Remote stand punches flow through contact; authority-only
                    // hit-stop would put other clients' poses ahead of the hit clock.
                    pauseRemaining = targeted ? 0f : step == 4 ? .10f : .045f;
                    DioFeedback.Sound(gameObject, step == 4 ? "impact-heavy" : "impact", 1f);
                    GetComponent<DioVisualDriver>()?.PauseAction(pauseRemaining);
                    GetComponent<DioComboTracker>()?.ContinueWindow(duration + pauseRemaining);
                    AddRecoil(step == 4 ? -4f : -1.5f, -.5f, -.5f, .5f);
                }
                if (hit) DioPlugin.Log.LogInfo("M1 contact step=" + (step + 1) + " normalizedTime=" + swingAge / duration + " time=" + Time.time);
            }
            // Allow a held combo to cancel the recovery tail. The animation still
            // plays at its original speed, and a released swing finishes normally.
            bool chainRequested = inputBank && inputBank.skill1.down;
            float recoveryEnd = duration * (chainRequested ? ChainPoint[step] : 1f);
            if (isAuthority && swingAge >= recoveryEnd) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            if (standPrimary && targeted) standPrimary.Release();
            GetComponent<DioVisualDriver>()?.EndPunch();
            if (hitVolume) Object.Destroy(hitVolume);
            base.OnExit();
        }
        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer); writer.Write(step); writer.Write(targeted); writer.Write(lockedTarget); writer.Write(punchDirection);
        }
        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader); step = reader.ReadInt32(); targeted = reader.ReadBoolean(); lockedTarget = reader.ReadHurtBoxReference(); punchDirection = reader.ReadVector3();
        }
        // A held primary must finish its swing, rather than restart itself every frame.
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
    public sealed class DioKnives : BaseSkillState
    {
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
        private float duration;
        private bool fired;
        public override void OnEnter()
        {
            base.OnEnter();
            StartAimMode(2f);
            duration = DioPlugin.MoveDuration(.75f, attackSpeedStat);
            GetComponent<DioVisualDriver>()?.Action("_spa_02", duration, true);
            GetComponent<DioVisualDriver>()?.BeginBarrageAim(new Ray(characterBody.corePosition+Vector3.up*.45f,GetAimRay().direction));
        }
        private void Fire()
        {
            var ray = GetAimRay();
            DioFeedback.Sound(gameObject, "swing", 1);
            GetComponent<DioAttackVoice>()?.Play();
            float aimDistance = 120f;
            if (Physics.Raycast(ray, out var terrainHit, aimDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                aimDistance = terrainHit.distance;
            foreach (var hit in Physics.RaycastAll(ray, aimDistance, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide))
            {
                var hurtBox = hit.collider.GetComponent<HurtBox>();
                var health = hurtBox ? hurtBox.healthComponent : null;
                if (!health || !health.alive || health == characterBody.healthComponent || !health.body ||
                    !health.body.teamComponent || health.body.teamComponent.teamIndex == GetTeam()) continue;
                aimDistance = Mathf.Min(aimDistance, hit.distance);
            }
            Vector3 target = ray.GetPoint(Mathf.Max(.2f, aimDistance));
            Quaternion aimRotation = Util.QuaternionSafeLookRotation(ray.direction);
            for (int i = 0; i < 15; i++)
            {
                // Irregular origins and a tight aim-plane cluster avoid a single impact pileup.
                Vector2 offset = Random.insideUnitCircle;
                Vector3 origin = ray.origin + aimRotation * new Vector3(offset.x * 1.1f, offset.y * .7f,
                    Mathf.Min(Random.Range(.7f, 1.6f), aimDistance * .3f));
                Vector3 displacement = origin - ray.origin;
                if (Physics.Raycast(ray.origin, displacement.normalized, out var obstruction, displacement.magnitude,
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    origin = ray.origin + displacement.normalized * Mathf.Max(0, obstruction.distance - .2f);
                Vector2 spread=Random.insideUnitCircle*.55f;
                Vector3 aimPoint=target+aimRotation*new Vector3(spread.x,spread.y,0);
                DioKnife.Launch(gameObject, origin, (aimPoint - origin).normalized, damageStat * 1.8f, RollCrit());
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            GetComponent<DioVisualDriver>()?.SetBarrageAim(new Ray(characterBody.corePosition+Vector3.up*.45f,GetAimRay().direction));
            if (isAuthority && !fired && fixedAge >= duration * .45f) { fired = true; Fire(); }
            if (isAuthority && fixedAge >= duration) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            GetComponent<DioVisualDriver>()?.EndBarrageAim();
            base.OnExit();
        }
    }
    public sealed class DioDash : BaseSkillState
    {
        private bool teleported;
        private bool punched;
        private float impactPause;
        private Ray aim;
        public override void OnEnter()
        {
            base.OnEnter();
            aim = GetAimRay();
            StartAimMode(1f);
            GetComponent<DioVisualDriver>()?.Action("_sty_01_str", .6f, true);
            DioFeedback.TimePulse(gameObject, characterBody.corePosition, isAuthority);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!isAuthority) return;
            if (!teleported && fixedAge >= .18f)
            {
                teleported = true;
                Vector3 start = transform.position;
                Vector3 coreOffset = characterBody.corePosition - start;
                Vector3 target = aim.origin + aim.direction * DioPlugin.TimeSkipRange.Value;
                if (Physics.Raycast(aim, out var aimedHit, DioPlugin.TimeSkipRange.Value, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    target = aimedHit.point + Vector3.up * Mathf.Max(.5f, characterBody.corePosition.y - characterBody.footPosition.y) + aimedHit.normal * .15f;
                // An enemy under the crosshair becomes the destination, rather
                // than being passed through on the way to distant terrain.
                float nearest = aimedHit.collider ? aimedHit.distance : DioPlugin.TimeSkipRange.Value;
                foreach (var hit in Physics.SphereCastAll(aim, .7f, nearest, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide))
                {
                    var hurtBox = hit.collider.GetComponent<HurtBox>();
                    var victim = hurtBox ? hurtBox.healthComponent : null;
                    if (!victim || !victim.alive || victim == characterBody.healthComponent ||
                        !victim.body || !victim.body.teamComponent || victim.body.teamComponent.teamIndex == GetTeam() || hit.distance >= nearest) continue;
                    nearest = hit.distance;
                    target = aim.GetPoint(Mathf.Max(0, hit.distance - 2f));
                }
                Vector3 delta = target - characterBody.corePosition;
                float distance = Mathf.Min(delta.magnitude, DioPlugin.TimeSkipRange.Value);
                Vector3 direction = delta.normalized;
                // Reuse the stepped, floor-aware capsule path so small slopes do not
                // stop a ground-level teleport. Walls still block the full body.
                Vector3 destination=DioSpecialGeometry.SafeExMove(characterBody,start,start+direction*distance);
                distance=Vector3.Distance(start,destination);
                TeleportHelper.TeleportGameObject(gameObject, destination);
                if (characterMotor) characterMotor.velocity = Vector3.zero;
                DioFeedback.Rings(destination + coreOffset);
                GetComponent<DioVisualDriver>()?.Action("_atk_l5", .8f, true);
                DioFeedback.Sound(gameObject, "stand-swing", .9f);
                DioPlugin.Log.LogInfo("Time skip distance=" + distance + " start=" + start + " destination=" + destination);
            }
            if (teleported && !punched && fixedAge >= .48f)
            {
                punched = true;
                DioFeedback.Sound(gameObject,"shift-punch-voice",1f);
                var result = new BlastAttack {
                    attacker = gameObject, inflictor = gameObject, teamIndex = GetTeam(),
                    position = characterBody.corePosition + aim.direction * 2.5f,
                    radius = 3.5f, baseDamage = damageStat * 10f,
                    baseForce = 0f, bonusForce = aim.direction * 1800f + Vector3.up * 250f,
                    impactEffect = EffectCatalog.FindEffectIndexFromPrefab(DioContent.PunchImpact),
                    crit = RollCrit(), procCoefficient = 1f,
                    falloffModel = BlastAttack.FalloffModel.None,
                    attackerFiltering = AttackerFiltering.NeverHitSelf
                }.Fire();
                if (result.hitCount > 0)
                {
                    impactPause = .2f;
                    DioFeedback.Impact(characterBody.corePosition + aim.direction * 2.5f, 2f);
                    GetComponent<DioVisualDriver>()?.PauseAction(impactPause);
                    DioFeedback.Sound(gameObject, "impact-heavy", 1f);
                    AddRecoil(-5f, -1f, -.6f, .6f);
                }
                DioPlugin.Log.LogInfo("Time skip punch hits=" + result.hitCount);
            }
            if (fixedAge >= .98f + impactPause) outer.SetNextStateToMain();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
    public sealed class DioTimeStop : BaseSkillState
    {
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
        private float duration;
        private bool fired;
        private DioAudioPlayback callout;
        private float beatTime, stageAge, voiceLength;
        private bool voiceStage;
        private string selectedVoice;
        public override void OnSerialize(NetworkWriter writer){base.OnSerialize(writer);writer.Write(selectedVoice??string.Empty);}
        public override void OnDeserialize(NetworkReader reader){base.OnDeserialize(reader);selectedVoice=reader.ReadString();}
        public override void OnEnter()
        {
            base.OnEnter();
            beatTime = DioPlugin.TimeStopBassTime.Value;
            // Keep the callout at its original pitch; animation/attack-speed modifiers
            // must not move the freeze away from the recording's bass impact.
            duration = beatTime + .25f;
            string voice=string.IsNullOrEmpty(selectedVoice)?DioFeedback.RandomTimeStopVoice():selectedVoice;selectedVoice=voice;
            voiceStage=voice!=null;
            voiceLength=voiceStage?DioFeedback.ClipDuration(voice,0):0;
            callout=DioFeedback.Sound(gameObject,voiceStage?voice:"time-stop-r",1f);
            GetComponent<DioVisualDriver>()?.Action("_sty_01_str", voiceLength+duration, true);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stageAge+=Time.fixedDeltaTime;
            float playbackTime = callout ? callout.PlaybackSeconds : stageAge;
            if(voiceStage)
            {
                if(playbackTime<voiceLength&&stageAge<voiceLength+1f)return;
                if(callout)Object.Destroy(callout.gameObject);
                voiceStage=false;stageAge=0;
                callout=DioFeedback.Sound(gameObject,"time-stop-r",1f);
                return;
            }
            // A failed/stopped audio device must never trap DIO in the cast state.
            if (stageAge > duration + 1f) playbackTime = stageAge;
            if (!fired && playbackTime >= beatTime)
            {
                fired = true;
                DioFeedback.MainTimeTransition(false, DioInput.IsLocal(characterBody),Mathf.Min(15f,5f+Mathf.Max(0,Mathf.Floor(characterBody.level)-1f)),true,characterBody);
                if (NetworkServer.active)
                {
                    float stoppedSeconds = Mathf.Min(15f,5f + Mathf.Max(0, Mathf.Floor(characterBody.level) - 1f));
                    DioTimeWorld.Begin(characterBody, stoppedSeconds);
                    DioPlugin.Log.LogInfo("Time stop scaling: level=" + characterBody.level + " duration=" + stoppedSeconds);
                }
                DioPlugin.Log.LogInfo("Main time stop bass cue: audio=" + playbackTime + " target=" + beatTime);
            }
            if (isAuthority && fired && playbackTime >= duration) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            if (!fired && callout) Object.Destroy(callout.gameObject);
            base.OnExit();
        }
    }
}
