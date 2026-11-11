using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
using System.Collections.Generic;
using LocalStandCombat;

namespace Diavolo
{
    public sealed class GrabThrow : CrimsonState
    {
        public bool Chain;private float speed=1f;
        private CharacterBody target;
        private bool punched;
        private bool impactSound;
        public bool Targeted;
        public HurtBoxReference LockedTarget;
        public Vector3 PunchDirection;
        private StandPrimaryController standPrimary;
        private readonly StandAttackClock clock=new StandAttackClock();
        private float duration;
        public override void OnEnter()
        {
            base.OnEnter();speed=Chain?StandPrimaryRules.AnimationSpeed(attackSpeedStat)*.85f:1f;duration=(Chain?1.3f:1.2f)/speed;
            if (Chain && Targeted) {
                standPrimary = StandPrimaryController.Of(gameObject);
                HurtBox locked = LockedTarget.ResolveHurtBox();
                if (standPrimary.Begin(locked, PunchDirection, .25f/speed)) {
                    target = locked.healthComponent.body;
                }
                BeginAttack("grab",duration,false);
                DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual && target) visual.SetGrabTarget(target);
                return;
            }
            BeginAttack("grab",duration);
            if (!NetworkServer.active) return;
            Ray aim = GetAimRay(); float best = 11f;
            foreach (CharacterBody enemy in CharacterBody.readOnlyInstancesList) {
                if (!TemporalController.IsEnemy(characterBody, enemy) || enemy.isPlayerControlled) continue;
                Vector3 delta = enemy.corePosition - aim.origin;
                if (delta.magnitude >= best || Vector3.Angle(aim.direction, delta) > 35f || Physics.Linecast(aim.origin, enemy.corePosition, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                best = delta.magnitude; target = enemy;
            }
            if (target) {
                DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.SetGrabTarget(target);
                new TemporalMessage(gameObject, 6, new List<FateMark> { new FateMark { Body = target } }).Send(NetworkDestination.Clients);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (Chain && isAuthority && inputBank && !inputBank.skill1.down) { outer.SetNextStateToMain(); return; }
            if (Chain && Targeted && standPrimary) {
                standPrimary.Tick(Time.fixedDeltaTime);
                if (!standPrimary.Active && target && !punched) {
                    if (NetworkServer.active) { BloodBlind blind=target.GetComponent<BloodBlind>();if(blind)blind.Release(Vector3.zero); }
                    target=null;
                }
            }
            float age=clock.Advance(Time.fixedDeltaTime,.85f/speed,Chain && Targeted && standPrimary.Active,Chain && Targeted && standPrimary.Arrived);
            if(Chain && Targeted)UpdatePrimaryPose(clock.Previous,age,duration);
            if(clock.TimedOut){standPrimary.Release();if(isAuthority)outer.SetNextStateToMain();return;}
            if (!impactSound && age >= .85f/speed && (!(Chain && Targeted) || standPrimary.Arrived)) { impactSound = true; DiavoloAudio.Impact(characterBody, "impact_grab"); }
            if (NetworkServer.active && target && !punched && target.healthComponent.alive && (!(Chain && Targeted) || standPrimary.Arrived)) {
                Vector3 forward = Chain && Targeted ? standPrimary.Direction : characterDirection.forward;
                // Anchor the target's collar just above King Crimson's forward hand.
                Vector3 collar = characterBody.footPosition + Vector3.up * 2.6f + forward * 2.1f;
                Vector3 collarOffset = target.corePosition - target.transform.position + Vector3.up * Mathf.Min(.5f, target.radius);
                // A targeted execution holds the victim where the stand reached it.
                Vector3 held = Chain && Targeted ? target.transform.position : collar - collarOffset;
                RaycastHit hit;
                if (Physics.Linecast(target.corePosition, held + collarOffset, out hit, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) held = hit.point - collarOffset - forward * .2f;
                Quaternion facing = Quaternion.LookRotation(-forward);
                BloodBlind.Lock(target, .2f, held, facing);
                if (!Chain || !Targeted) TeleportHelper.TeleportGameObject(target.gameObject, held);
                if (age >= .85f/speed) {
                    punched = true;
                    BloodBlind blind = target.GetComponent<BloodBlind>();
                    if (blind) blind.Release(Vector3.zero);
                    DamageInfo damage = new DamageInfo { attacker = gameObject, inflictor = gameObject, damage = damageStat * 24f, crit = RollCrit(), procCoefficient = 1f, position = target.corePosition, force = Vector3.zero };
                    target.healthComponent.TakeDamage(damage);
                    GlobalEventManager.instance.OnHitEnemy(damage, target.gameObject); GlobalEventManager.instance.OnHitAll(damage, target.gameObject);
                    target = null;
                }
            }
            if (isAuthority && age >= duration) outer.SetNextStateToMain();
        }
        public override void OnSerialize(NetworkWriter w){base.OnSerialize(w);w.Write(Chain);w.Write(Targeted);w.Write(LockedTarget);w.Write(PunchDirection);}
        public override void OnDeserialize(NetworkReader r){base.OnDeserialize(r);Chain=r.ReadBoolean();Targeted=r.ReadBoolean();LockedTarget=r.ReadHurtBoxReference();PunchDirection=r.ReadVector3();}
        public override void OnExit()
        {
            if (NetworkServer.active && target) { BloodBlind blind = target.GetComponent<BloodBlind>(); if (blind) blind.Release(Vector3.zero); }
            if(Chain && isAuthority){var tracker=GetComponent<CrimsonChainTracker>();if(tracker)tracker.Sequence.Finish(Time.time,clock.Age>=.85f/speed && !clock.TimedOut);}
            if (standPrimary) {standPrimary.Release();EndPrimaryPose();}
            base.OnExit();
        }
    }
}
