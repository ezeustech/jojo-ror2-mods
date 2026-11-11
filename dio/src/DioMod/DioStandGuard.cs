using System;
using System.Reflection;
using EntityStates;
using MonoMod.RuntimeDetour;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EyesOfHeavenDio
{
    internal static class DioGuardHooks
    {
        private static Hook hook;
        private delegate void TakeDamageOriginal(HealthComponent health, DamageInfo info);
        private static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static void Install()
        {
            hook = new Hook(typeof(HealthComponent).GetMethod("TakeDamage", new[] { typeof(DamageInfo) }),
                new Action<TakeDamageOriginal, HealthComponent, DamageInfo>(TakeDamage));
        }
        internal static void Dispose() { hook?.Dispose(); hook = null; }
        private static void TakeDamage(TakeDamageOriginal original, HealthComponent health, DamageInfo info)
        {
            var guard = NetworkServer.active && health ? health.GetComponent<DioStandGuard>() : null;
            if (guard && guard.CanBlock(info))
            {
                // Never mutate the attacker's shared DamageInfo (e.g. an area hit).
                info = (DamageInfo)Clone.Invoke(info, null);
                info.damage *= DioGuardMath.DamageMultiplier;
                info.force *= .1f;
                guard.Hit();
            }
            original(health, info);
        }
    }

    public sealed class DioStandGuard : MonoBehaviour
    {
        public bool Blocking { get; private set; }
        private CharacterBody body;
        private DioVisualDriver visual;
        private float nextImpact;
        private void Awake() { body=GetComponent<CharacterBody>(); visual=GetComponent<DioVisualDriver>(); }
        public void Begin() { if(!visual)visual=GetComponent<DioVisualDriver>(); Blocking=true; visual?.BeginGuard(); }
        public void End() { Blocking=false; if(visual)visual.EndGuard(); }
        public Vector3 Facing
        {
            get { var direction=body&&body.inputBank ? body.inputBank.aimDirection : transform.forward; direction.y=0; return direction.sqrMagnitude>.001f ? direction.normalized : transform.forward; }
        }
        internal bool CanBlock(DamageInfo info)
        {
            if (!Blocking || !body || !body.healthComponent.alive || info == null || info.rejected || info.damage<=0 || !info.attacker || info.attacker==gameObject || info.dotIndex!=DotController.DotIndex.None) return false;
            if ((info.damageType & (DamageType.BypassBlock | DamageType.VoidDeath | DamageType.FallDamage)) != DamageType.Generic) return false;
            Vector3 incoming = info.force.sqrMagnitude>.01f ? -info.force :
                (info.inflictor ? info.inflictor.transform.position : info.attacker.transform.position)-body.corePosition;
            return DioGuardMath.InFront(Facing.x,Facing.z,incoming.x,incoming.z);
        }
        internal void Hit()
        {
            if(Time.time<nextImpact)return;
            nextImpact=Time.time+.09f;
            var point=body.corePosition+Facing*1.6f;
            DioFeedback.ServerSound(gameObject,"stand-block",.6f);
            DioFeedback.Impact(point,.65f);
            DioStandParticles.Emit(point,true);
        }
        private void OnDisable()=>End();
        private void OnDestroy()=>End();
    }

    public sealed class DioStandBlock : BaseSkillState
    {
        private DioStandGuard guard;
        public override void OnEnter()
        {
            base.OnEnter();guard=GetComponent<DioStandGuard>();guard?.Begin();
            characterBody.isSprinting=false;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && (!DioInput.Held(DioPlugin.StandBlockKey.Value,5) || !characterBody.healthComponent.alive ||
                !DioInput.Allowed(characterBody)))
            { outer.SetNextStateToMain(); return; }
            characterBody.isSprinting=false;
            StartAimMode(.2f);
        }
        public override void OnExit() { if(guard)guard.End();base.OnExit(); }
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Skill;
    }
}
