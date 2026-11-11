using EntityStates;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
using System.Collections.Generic;

namespace Diavolo
{
    public sealed class CounterController : MonoBehaviour
    {
        public bool Posing { get; private set; }
        private float expires;
        private CharacterBody body;
        private CharacterBody attacker;
        private float strikeAt;
        private bool chop;
        internal static void Install()
        {
            Harmony harmony = new Harmony("local.diavolo.damage");
            foreach (string method in new string[] { "TakeDamageProcess" }) harmony.Patch(AccessTools.Method(typeof(HealthComponent), method, new System.Type[] { typeof(DamageInfo) }), prefix: new HarmonyMethod(typeof(CounterController), "Intercept"));
            harmony.Patch(AccessTools.Method(typeof(HealthComponent), "TakeDamageProcess", new System.Type[] { typeof(DamageInfo) }),
                prefix: new HarmonyMethod(typeof(CounterController), "Ambush") { priority = Priority.Last },
                postfix: new HarmonyMethod(typeof(CounterController), "FinishAmbush"));
            // Compatibility patchers can add legacy Suicide overloads at startup.
            foreach (System.Reflection.MethodInfo method in typeof(HealthComponent).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) {
                System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "Suicide" && parameters.Length > 0 && parameters[0].ParameterType == typeof(GameObject))
                    harmony.Patch(method, prefix: new HarmonyMethod(typeof(CounterController), "InterceptDeath"));
            }
        }
        private static bool InterceptDeath(HealthComponent __instance, GameObject __0)
        {
            if (!NetworkServer.active || !__instance || !__instance.body) return true;
            if (__instance.body.GetComponent<TimeEraseDecoy>()) return false;
            TemporalController temporal = __instance.body.GetComponent<TemporalController>();
            if (temporal && temporal.Dodging) { temporal.Dodge(); return false; }
            // Combat executions can call Suicide directly; keep run teardown/despawn intact.
            if (!__0) return true;
            return Intercept(__instance, new DamageInfo { attacker = __0 });
        }

        private static bool Intercept(HealthComponent __instance, DamageInfo damageInfo)
        {
            if (!NetworkServer.active || !__instance || !__instance.body || damageInfo == null) return true;
            if (__instance.body.GetComponent<TimeEraseDecoy>() || (damageInfo.attacker && damageInfo.attacker.GetComponent<TimeEraseDecoy>())) {
                CounterController decoyCounter = __instance.body.GetComponent<CounterController>();
                if (__instance.body.GetComponent<TimeEraseDecoy>() && decoyCounter && decoyCounter.Posing) decoyCounter.Trigger(damageInfo.attacker);
                damageInfo.damage = 0f; damageInfo.procCoefficient = 0f; damageInfo.rejected = true; return false;
            }
            CharacterBody victim = __instance.body;
            TemporalController time = victim.GetComponent<TemporalController>();
            if (time && time.Dodging) { damageInfo.rejected = true; time.Dodge(); return false; }
            CounterController counter = victim.GetComponent<CounterController>();
            if (counter && counter.Posing && Time.time <= counter.expires) {
                damageInfo.rejected = true;
                counter.Trigger(damageInfo.attacker);
                return false;
            }
            if (time && time.Erasing) { damageInfo.rejected = true; return false; }
            var defense=victim.GetComponent<StandDefense>();
            if(defense && defense.Blocking && damageInfo.dotIndex==DotController.DotIndex.None && (damageInfo.damageType & DamageType.BypassBlock)==DamageType.Generic){
                Vector3 incoming=damageInfo.attacker?damageInfo.attacker.transform.position-victim.corePosition:damageInfo.position-victim.corePosition;
                if(incoming.sqrMagnitude>.01f && Vector3.Dot(victim.inputBank.aimDirection,incoming.normalized)>=.25f)damageInfo.damage*=.15f;
            }
            return true;
        }
        private static void Ambush(HealthComponent __instance, DamageInfo damageInfo, out TemporalController __state)
        {
            __state = null;
            if (!NetworkServer.active || damageInfo == null || damageInfo.rejected || damageInfo.damage <= 0f || damageInfo.dotIndex != DotController.DotIndex.None || damageInfo.procCoefficient <= 0f || !damageInfo.attacker) return;
            TemporalController owner = damageInfo.attacker.GetComponent<TemporalController>();
            if (!owner || !TemporalController.IsEnemy(damageInfo.attacker.GetComponent<CharacterBody>(), __instance.body) || !owner.TakeAmbush()) return;
            damageInfo.damage *= 2f; __state = owner;
        }
        private static void FinishAmbush(DamageInfo damageInfo, TemporalController __state)
        {
            if (__state && damageInfo.rejected) { damageInfo.damage *= .5f; __state.RestoreAmbush(); }
            if (!NetworkServer.active || damageInfo.rejected || damageInfo.damage <= 0 || damageInfo.dotIndex != DotController.DotIndex.None || damageInfo.procCoefficient <= 0 || !damageInfo.attacker) return;
            DiavoloPresentation visual = damageInfo.attacker.GetComponent<DiavoloPresentation>();
            if (!visual || damageInfo.attacker.GetComponent<TimeEraseDecoy>()) return;
            string move = visual.CurrentMove;
            if (string.IsNullOrEmpty(move)) return;
            if (move == "barrage") DiavoloAudio.Impact(visual.GetComponent<CharacterBody>(), "barrage_hit");
            BloodFx.Impact(damageInfo.position, damageInfo.position - damageInfo.attacker.transform.position, move == "impale" || move == "chop");
        }
        private void Awake() { body = GetComponent<CharacterBody>(); }
        public void Begin() { Posing = true; expires = Time.time + 2.5f; }
        public void StopPose() { Posing = false; }
        private void Trigger(GameObject source)
        {
            Posing = false;
            CharacterBody enemy = source ? source.GetComponent<CharacterBody>() : null;
            if (!enemy && source) { CharacterMaster master = source.GetComponent<CharacterMaster>(); if (master) enemy = master.GetBody(); }
            if (!TemporalController.IsEnemy(body, enemy)) {
                enemy = null;
                // Environmental damage still counters; select a nearby hostile if possible.
                float nearest = 60f;
                foreach (CharacterBody candidate in CharacterBody.readOnlyInstancesList) if (TemporalController.IsEnemy(body, candidate)) {
                    float distance = Vector3.Distance(body.corePosition, candidate.corePosition);
                    if (distance < nearest) { nearest = distance; enemy = candidate; }
                }
            }
            GetComponent<TemporalController>().BreakForAttack();
            TimeOverlay.Send(body, 2);
            if (enemy) {
                Vector3 forward = enemy.characterDirection ? enemy.characterDirection.forward : enemy.transform.forward;
                forward.y = 0; forward.Normalize();
                Vector3 wanted = enemy.transform.position - forward * (enemy.radius + body.radius + 1f);
                Vector3 delta = wanted - enemy.corePosition;
                RaycastHit hit;
                if (Physics.SphereCast(enemy.corePosition, Mathf.Max(.25f, body.radius), delta.normalized, out hit, delta.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) wanted = enemy.corePosition + delta.normalized * Mathf.Max(0, hit.distance - .2f);
                TeleportHelper.TeleportGameObject(gameObject, wanted);
                Vector3 facing = (enemy.corePosition - body.corePosition).normalized;
                body.inputBank.aimDirection = facing;
                if (body.characterDirection) { body.characterDirection.forward = facing; }
                float ourHeight = Height(body), theirHeight = Height(enemy);
                chop = CombatRules.CounterUsesChop(ourHeight, theirHeight);
                attacker = enemy; strikeAt = Time.time + .16f;
                if (!enemy.isPlayerControlled) BloodBlind.Lock(enemy, .35f, enemy.transform.position, enemy.transform.rotation);
                if (NetworkServer.active) body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, .45f);
            }
            ShowCounter(chop);
            new TemporalMessage(gameObject, (byte)(chop ? 4 : 3), new List<FateMark>()).Send(NetworkDestination.Clients);
        }
        internal void ShowCounter(bool useChop)
        {
            Posing = false;
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>();
            if (visual) visual.Attack(useChop ? "chop" : "impale", .6f);
        }
        private static float Height(CharacterBody target)
        {
            CapsuleCollider capsule = target.GetComponent<CapsuleCollider>();
            if (capsule) return capsule.height * Mathf.Abs(capsule.transform.lossyScale.y);
            if (target.mainHurtBox) { Collider collider = target.mainHurtBox.GetComponent<Collider>(); if (collider) return collider.bounds.size.y; }
            return Mathf.Max(1f, target.radius * 2f);
        }
        private void FixedUpdate()
        {
            if (Time.time > expires) Posing = false;
            if (!NetworkServer.active || !attacker || Time.time < strikeAt) return;
            CharacterBody target = attacker; attacker = null;
            if (!body.healthComponent.alive || !target.healthComponent || !target.healthComponent.alive) return;
            DamageInfo damage = new DamageInfo { attacker = gameObject, inflictor = gameObject, damage = body.damage * (chop ? 12f : 18f), crit = body.RollCrit(), procCoefficient = 1f, position = target.corePosition, damageType = chop ? DamageType.BleedOnHit : DamageType.Generic };
            target.healthComponent.TakeDamage(damage);
            GlobalEventManager.instance.OnHitEnemy(damage, target.gameObject);
            GlobalEventManager.instance.OnHitAll(damage, target.gameObject);
        }
        private void OnDisable() { Posing = false; attacker = null; }
    }

    public sealed class CounterPose : BaseSkillState
    {
        public override void OnEnter()
        {
            base.OnEnter(); GetComponent<CounterController>().Begin();
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.Attack("counter", 2.5f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && characterMotor) { Vector3 velocity = characterMotor.velocity; velocity.x = 0; velocity.z = 0; characterMotor.velocity = velocity; }
            if (isAuthority && (fixedAge >= 2.5f || !GetComponent<CounterController>().Posing)) outer.SetNextStateToMain();
        }
        public override void OnExit() { GetComponent<CounterController>().StopPose(); base.OnExit(); }
        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Frozen; }
    }
}
