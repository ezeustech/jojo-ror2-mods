using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using LocalStandCombat;

namespace Diavolo
{
    public sealed class CrimsonChainTracker : MonoBehaviour { public readonly ComboSequence Sequence = new ComboSequence(); }
    public sealed class CrimsonChain : CrimsonState
    {
        public int Step;
        private float duration;
        private bool fired;
        private CrimsonChainTracker tracker;
        private StandPrimaryController standPrimary;
        private HurtBoxReference lockedTarget;
        private bool targeted;
        private Vector3 punchDirection;
        private readonly StandAttackClock clock = new StandAttackClock();
        public override void OnEnter()
        {
            base.OnEnter();
            tracker = GetComponent<CrimsonChainTracker>();
            if (!tracker && isAuthority) tracker = gameObject.AddComponent<CrimsonChainTracker>();
            if (isAuthority && tracker) Step = tracker.Sequence.Take(Time.time);
            Step = Mathf.Clamp(Step, 0, 4);
            standPrimary = StandPrimaryController.Of(gameObject);
            if (isAuthority) {
                punchDirection = GetAimRay().direction;
                HurtBox target = standPrimary.Acquire(GetAimRay());
                targeted = target;
                lockedTarget = HurtBoxReference.FromHurtBox(target);
            }
            if(Step>=3)return;
            duration = (Step == 2 ? .62f : .44f) / (.85f * StandPrimaryRules.AnimationSpeed(attackSpeedStat));
            if (targeted) standPrimary.Begin(lockedTarget.ResolveHurtBox(), punchDirection, duration * .35f);
            BeginAttack(Step == 3 ? "chop" : "chain" + (Step + 1), duration, !targeted);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && inputBank && !inputBank.skill1.down) { outer.SetNextStateToMain(); return; }
            if(Step>=3){if(isAuthority)outer.SetNextState(Step==3?(EntityStates.EntityState)new GrabThrow{Chain=true,Targeted=targeted,LockedTarget=lockedTarget,PunchDirection=punchDirection}:new GroundSmash{Chain=true,Targeted=targeted,LockedTarget=lockedTarget,PunchDirection=punchDirection});return;}
            if (targeted) standPrimary.Tick(Time.fixedDeltaTime);
            float age=clock.Advance(Time.fixedDeltaTime,duration*.35f,targeted && standPrimary.Active,targeted && standPrimary.Arrived);
            if(targeted)UpdatePrimaryPose(clock.Previous,age,duration);
            if(clock.TimedOut){standPrimary.Release();if(isAuthority)outer.SetNextStateToMain();return;}
            if (!fired && age >= duration * .35f) {
                fired = true;
                float coefficient = Step == 0 ? 3f : Step == 1 ? 3.5f : 6f;
                if (targeted) { Ray attackRay; if (standPrimary.TryAttackRay(out attackRay)) StrikeFrom(attackRay, coefficient, 3.2f, 1.6f); }
                else Strike(coefficient, 8f, 1.6f);
                DiavoloAudio.Impact(characterBody, Step == 3 ? "impact_chop" : Step == 2 ? "impact_heavy" : "impact_light");
            }
            if (isAuthority && age >= duration) outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            if (isAuthority && tracker && Step<3) tracker.Sequence.Finish(Time.time, fired);
            if (Step < 3 && standPrimary && targeted) {standPrimary.Release();EndPrimaryPose();}
            base.OnExit();
        }
        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(Step); writer.Write(targeted); writer.Write(lockedTarget); writer.Write(punchDirection); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); Step = reader.ReadInt32(); targeted=reader.ReadBoolean(); lockedTarget=reader.ReadHurtBoxReference(); punchDirection=reader.ReadVector3(); }
    }
}
