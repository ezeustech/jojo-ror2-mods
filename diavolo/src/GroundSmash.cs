using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using LocalStandCombat;
namespace Diavolo
{
    public sealed class GroundSmash : CrimsonState
    {
        public bool Chain;
        private float duration;
        private bool fired, ground;
        private Vector3 point;
        public bool Targeted;
        public HurtBoxReference LockedTarget;
        public Vector3 PunchDirection;
        private StandPrimaryController standPrimary;
        private readonly StandAttackClock clock=new StandAttackClock();
        public override void OnEnter()
        {
            base.OnEnter(); duration = Chain ? 1f/(StandPrimaryRules.AnimationSpeed(attackSpeedStat)*.85f) : .9f/attackSpeedStat;
            if ((isAuthority || NetworkServer.active) && (!Chain || !Targeted)) {
                Vector3 forward = Vector3.ProjectOnPlane(GetAimRay().direction, Vector3.up).normalized;
                if (forward.sqrMagnitude < .1f) forward = characterDirection.forward;
                Vector3 wanted = characterBody.corePosition + forward * 4.5f;
                RaycastHit hit;
                if (Physics.Linecast(characterBody.corePosition, wanted, out hit, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) wanted = hit.point - forward * .5f;
                ground = Physics.Raycast(wanted + Vector3.up * 2f, Vector3.down, out hit, 12f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
                point = ground ? hit.point : wanted - Vector3.up;
            }
            if (Chain && Targeted) {
                standPrimary=StandPrimaryController.Of(gameObject);
                standPrimary.Begin(LockedTarget.ResolveHurtBox(),PunchDirection,duration*.6f);
                UpdateTargetPoint();
            }
            BeginAttack("groundsmash", duration, !(Chain && Targeted));
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.SetSmashTarget(point);
        }
        private void UpdateTargetPoint()
        {
            if (!standPrimary || !standPrimary.Active || !standPrimary.ValidTarget(standPrimary.Target)) { ground=false; return; }
            RaycastHit hit;
            Vector3 wanted=standPrimary.Target.transform.position;
            ground=Physics.Raycast(wanted+Vector3.up*2f,Vector3.down,out hit,12f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore);
            point=ground?hit.point:wanted-Vector3.up;
            standPrimary.SetPoint(point+Vector3.up*1.3f);
            DiavoloPresentation visual=GetComponent<DiavoloPresentation>();if(visual)visual.SetSmashTarget(point);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (Chain && isAuthority && inputBank && !inputBank.skill1.down) { outer.SetNextStateToMain(); return; }
            if (Chain && Targeted) { if(!fired)UpdateTargetPoint();standPrimary.Tick(Time.fixedDeltaTime); }
            float age=clock.Advance(Time.fixedDeltaTime,duration*.6f,Chain && Targeted && standPrimary.Active,Chain && Targeted && standPrimary.Arrived);
            if(Chain && Targeted)UpdatePrimaryPose(clock.Previous,age,duration);
            if(clock.TimedOut){standPrimary.Release();if(isAuthority)outer.SetNextStateToMain();return;}
            if (!fired && age >= duration * .6f) {
                fired = true;
                if (ground && (!Targeted || (standPrimary && standPrimary.Arrived))) {
                    DiavoloAudio.Impact(characterBody, "impact_ground");
                    GroundBreakFx.Send(point,6f);
                    if (NetworkServer.active) {
                        new BlastAttack { attacker = gameObject, inflictor = gameObject, teamIndex = characterBody.teamComponent.teamIndex,
                            position = point, radius = 6f, baseDamage = damageStat * 12f, baseForce = 1200f, bonusForce = Vector3.up * 1600f,
                            crit = RollCrit(), procCoefficient = 1f, damageType = DamageType.Stun1s, falloffModel = BlastAttack.FalloffModel.None,
                            attackerFiltering = AttackerFiltering.NeverHitSelf }.Fire();
                    }
                }
            }
            if (isAuthority && age >= duration) outer.SetNextStateToMain();
        }
        public override void OnExit(){if(Chain && isAuthority){var tracker=GetComponent<CrimsonChainTracker>();if(tracker)tracker.Sequence.Finish(Time.time,fired);}if(standPrimary){standPrimary.Release();EndPrimaryPose();}base.OnExit();}
        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(Chain);writer.Write(point); writer.Write(ground); writer.Write(Targeted);writer.Write(LockedTarget);writer.Write(PunchDirection); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); Chain=reader.ReadBoolean();point = reader.ReadVector3(); ground = reader.ReadBoolean();Targeted=reader.ReadBoolean();LockedTarget=reader.ReadHurtBoxReference();PunchDirection=reader.ReadVector3(); }
    }
}
