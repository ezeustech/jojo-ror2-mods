using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EyesOfHeavenDio
{
    [DefaultExecutionOrder(1000)]
    public sealed class DioStandJump : MonoBehaviour
    {
        public float ReadyAt { get; private set; }
        private CharacterBody body;
        private CharacterMotor motor;
        private GameObject legs;
        private bool active, leftGround, hovering;
        private float launched;
        private void Start() { body = GetComponent<CharacterBody>(); motor = GetComponent<CharacterMotor>(); }
        private bool CanControl => body && body.hasEffectiveAuthority && body.healthComponent.alive && motor && Time.timeScale > 0;
        private bool MenuOpen => !DioInput.Allowed(body);
        private void Update()
        {
            if (GetComponent<DioExtraMoves>()?.SpecialBusy == true || GetComponent<DioStandGuard>()?.Blocking == true) return;
            if (!CanControl || MenuOpen || active || Time.time < ReadyAt || !motor.isGrounded) return;
            if (!DioInput.Pressed(DioPlugin.StandJumpKey.Value,2)) return;
            ReadyAt = Time.time + 8f; active = true; leftGround = false; launched = Time.time;
            ShowLegs();
            GetComponent<DioVisualDriver>()?.Action("_bas_jump_lp", .5f, false);
            GroundImpact(3f);
            motor.Motor.ForceUnground();
            var velocity = motor.velocity; velocity.y = 30.7f; motor.velocity = velocity;
        }
        private void FixedUpdate()
        {
            if (!active) return;
            if (!CanControl) { Clear(); return; }
            if (!motor.isGrounded) leftGround = true;
            if (leftGround && motor.isGrounded && Time.time > launched + .15f)
            { GroundImpact(5f); Clear(); return; }
            if (!leftGround && Time.time > launched + 1f) { Clear(); return; }
            // Let the launch rise naturally; holding jump catches DIO at the apex.
            bool hold = !MenuOpen && body.inputBank && body.inputBank.jump.down && motor.velocity.y <= 0;
            if (hold || (hovering && !MenuOpen && body.inputBank && body.inputBank.jump.down))
            {
                if (!hovering) { var gravity = motor.gravityParameters; gravity.channeledAntiGravityGranterCount++; motor.gravityParameters = gravity; hovering = true; }
                var velocity = motor.velocity; velocity.y = 0; motor.velocity = velocity;
            }
            else RestoreGravity();
        }
        private void ShowLegs()
        {
            if (!legs)
            {
                var driver = GetComponent<DioVisualDriver>();
                legs = Instantiate(driver.StandPrefab, driver.dioRoot.transform.parent);

                legs.name = "StandJumpLegs";
                legs.transform.localPosition = driver.dioRoot.transform.localPosition;
                legs.transform.localRotation = driver.dioRoot.transform.localRotation;
                legs.transform.localScale = Vector3.one * DioAssets.StandScale * 1.06f;
                foreach (var skin in legs.GetComponentsInChildren<DioCpuSkinning>(true)) skin.legsOnly = true;
                legs.AddComponent<DioStandLegPose>().Bind(driver.dioRoot.transform);
            }
            GetComponent<DioVisualDriver>()?.SummonFeedback(body.footPosition+Vector3.up*.5f);
            legs.SetActive(true);

        }
        private void GroundImpact(float coefficient)
        {
            Vector3 point = body.footPosition;
            if (Physics.Raycast(body.corePosition, Vector3.down, out var hit, 8f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) point = hit.point;
            DioFeedback.Sound(gameObject, "impact-heavy", 1f);
            DioFeedback.Impact(point + Vector3.up * .2f, 2f);
            DioGroundBreak.Spawn(point);
            if (NetworkServer.active) new BlastAttack {
                attacker = gameObject, inflictor = gameObject, teamIndex = body.teamComponent.teamIndex,
                position = point, radius = 5f, baseDamage = body.damage * coefficient,
                baseForce = 250f, bonusForce = Vector3.up * 350f, crit = body.RollCrit(), procCoefficient = 1f,
                falloffModel = BlastAttack.FalloffModel.None, attackerFiltering = AttackerFiltering.NeverHitSelf
            }.Fire();
        }
        private void RestoreGravity() { if (hovering && motor) { var gravity = motor.gravityParameters; gravity.channeledAntiGravityGranterCount = Mathf.Max(0, gravity.channeledAntiGravityGranterCount - 1); motor.gravityParameters = gravity; } hovering = false; }
        public void Cancel() => Clear();
        private void Clear() { RestoreGravity(); active = false; if (legs) legs.SetActive(false); }
        private void OnDisable() => Clear();
        private void OnDestroy() { Clear(); if (legs) Destroy(legs); }
    }

    [DefaultExecutionOrder(50)]
    public sealed class DioStandLegPose : MonoBehaviour
    {
        public Vector3 Offset;
        private Transform sourceSpace, targetSpace;
        private readonly System.Collections.Generic.List<Transform> targets = new System.Collections.Generic.List<Transform>();
        private readonly System.Collections.Generic.List<Transform> sources = new System.Collections.Generic.List<Transform>();
        public void Bind(Transform dio)
        {
            foreach(var skin in dio.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.name.EndsWith(" body")){sourceSpace=skin.transform;break;}
            foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.name.EndsWith(" body")){targetSpace=skin.transform;break;}
            var animation = GetComponentInChildren<Animation>(true);
            if (animation) animation.enabled = false;
            var bones = dio.GetComponentsInChildren<Transform>(true);
            foreach (var target in GetComponentsInChildren<Transform>(true))
            {
                string match = target.name.Substring(target.name.IndexOf(' ')+1);
                if (match == target.name) continue;
                foreach (var source in bones)
                    if (source.name.Substring(source.name.IndexOf(' ')+1) == match) { targets.Add(target); sources.Add(source); break; }
            }
        }
        private void LateUpdate()
        {
            for (int i = 0; i < targets.Count; i++)
                if (targets[i] && sources[i])
                {
                    Vector3 point=DioPoseSolver.VisualPosition(sourceSpace,sources[i])+Offset;
                    if(targetSpace)point=targetSpace.position+(point-targetSpace.position)/Mathf.Max(.0001f,Mathf.Abs(targetSpace.lossyScale.x));
                    targets[i].SetPositionAndRotation(point,sources[i].rotation);
                }
        }
    }

    public sealed class DioGroundBreak : MonoBehaviour
    {
        private Material material;
        private Transform[] chunks;
        private Vector3[] velocities;
        private float age;
        public static void Spawn(Vector3 point, float size = 1f)
        {
            if (!DioFeedback.RingShader) return;
            var obj = new GameObject("DioGroundBreak"); DioTimeWorld.MarkRollerEffect(obj); obj.transform.position = point + Vector3.up * .05f;
            obj.transform.localScale = Vector3.one * size;
            obj.AddComponent<DioGroundBreak>();
        }
        private void Start()
        {
            material = new Material(DioFeedback.RingShader);
            chunks = new Transform[12]; velocities = new Vector3[12];
            for (int i = 0; i < chunks.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / chunks.Length;
                var outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var crack = new GameObject("GroundCrack"); crack.transform.SetParent(transform, false);
                var line = crack.AddComponent<LineRenderer>(); line.sharedMaterial = material;
                line.useWorldSpace = false; line.positionCount = 4; line.startWidth = .12f; line.endWidth = .015f;
                line.startColor = new Color(1f,.8f,.25f); line.endColor = new Color(.25f,.18f,.08f);
                for (int j = 0; j < 4; j++)
                {
                    Vector3 p = transform.position + outward * (j * 1.1f);
                    if (Physics.Raycast(p + Vector3.up * 2, Vector3.down, out var hit, 4, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) p = hit.point + hit.normal * .04f;
                    line.SetPosition(j, p - transform.position);
                }
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.GetComponent<Collider>().enabled=false;
                Destroy(rock.GetComponent<Collider>()); rock.transform.SetParent(transform, false);
                rock.transform.localPosition = outward * UnityEngine.Random.Range(.5f, 2f);
                rock.transform.localScale = Vector3.one * UnityEngine.Random.Range(.15f,.35f);
                rock.GetComponent<Renderer>().sharedMaterial = material;
                chunks[i] = rock.transform; velocities[i] = outward * 2 + Vector3.up * UnityEngine.Random.Range(3f,6f);
            }
        }
        private void Update()
        {
            if (DioTimeWorld.Active && !GetComponent<DioTimeStopExempt>()) return;
            age += Time.deltaTime;
            if (age > 1f) { Destroy(gameObject); return; }
            for (int i = 0; i < chunks.Length; i++)
            {
                velocities[i] += Vector3.down * 16f * Time.deltaTime;
                chunks[i].localPosition += velocities[i] * Time.deltaTime;
                chunks[i].Rotate(Vector3.one * 180f * Time.deltaTime);
                if (age > .65f) chunks[i].localScale *= Mathf.Exp(-8 * Time.deltaTime);
            }
        }
        private void OnDestroy() { if (material) Destroy(material); }
    }
}
