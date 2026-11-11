using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;

namespace Diavolo
{
    public sealed class ImpaleImpact : CrimsonState
    {
        public bool Charged;
        private bool contacted;
        private bool returned;
        private CharacterBody launchTarget;
        private Vector3 launchVelocity;private float holdDuration,releaseAt;
        public bool CanPuttsunCancel { get { return Charged && contacted; } }
        public override void OnEnter()
        {
            base.OnEnter();
            TemporalController time = GetComponent<TemporalController>(); if (time && time.Erasing) time.HoldingImpale = true;
            holdDuration=DiavoloAudio.Duration("impact_impale_sfx",1.15f);
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.Attack("impale", holdDuration + .5f / attackSpeedStat);
            StartAimMode(2f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(contacted && fixedAge>=releaseAt)ReleaseLaunch();else if(launchTarget)MoveHeldTarget();
            if (!returned && fixedAge >= .08f / attackSpeedStat) {
                returned = true;
                TemporalController time = GetComponent<TemporalController>();
                time.HoldingImpale = false; time.BreakForAttack(TemporalAttack.Impale, AttackMoment.PreContact);
                if (NetworkServer.active) Physics.SyncTransforms();
            }
            if (!contacted && fixedAge >= .18f / attackSpeedStat) {
                contacted = true;
                TemporalController time = GetComponent<TemporalController>();
                // Fallback if erasure was re-entered; synchronize moved hurtboxes before the hit query.
                time.HoldingImpale = false; time.BreakForAttack(TemporalAttack.Impale, AttackMoment.Contact);
                if (NetworkServer.active) { Physics.SyncTransforms(); Contact(); }
            }
            if (isAuthority && fixedAge >= holdDuration + (Charged ? 1.5f : .5f) / attackSpeedStat) outer.SetNextStateToMain();
        }
        private void Contact()
        {
            Ray aim = GetAimRay(); float range = Charged ? 20f : 10f;
            RaycastHit wall;
            if (Physics.Raycast(aim, out wall, range, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) range = wall.distance;
            CharacterBody target = null; float nearest = range;
            foreach (RaycastHit hit in Physics.SphereCastAll(aim, 1.5f, range, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide)) {
                HurtBox hurt = hit.collider.GetComponent<HurtBox>();
                CharacterBody enemy = hurt && hurt.healthComponent ? hurt.healthComponent.body : null;
                if (!TemporalController.IsEnemy(characterBody, enemy) || hit.distance > nearest) continue;
                target = enemy; nearest = hit.distance;
            }
            if (!target) return;
            DamageInfo damage = new DamageInfo { attacker = gameObject, inflictor = gameObject, damage = damageStat * (Charged ? 32f : 16f), crit = RollCrit(), procCoefficient = 1f, position = target.corePosition,
                damageType = Charged ? DamageType.BypassArmor | DamageType.BypassBlock : DamageType.Generic };
            target.healthComponent.TakeDamage(damage);
            GlobalEventManager.instance.OnHitEnemy(damage, target.gameObject); GlobalEventManager.instance.OnHitAll(damage, target.gameObject);
            if (!damage.rejected) {
                DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.ImpaleContact(target);
                new TemporalMessage(gameObject, 8, new System.Collections.Generic.List<FateMark> { new FateMark { Body = target } }).Send(R2API.Networking.NetworkDestination.Clients);
            }
            if (damage.rejected || !target.healthComponent.alive || target.isPlayerControlled) return;
            bool airborne = !target.characterMotor || !target.characterMotor.isGrounded;
            launchTarget=target;releaseAt=fixedAge+holdDuration;
            launchVelocity=Charged && airborne ? aim.direction*55f+Vector3.up*6f : Vector3.zero;
            BloodBlind.Lock(target,holdDuration,target.transform.position,target.transform.rotation);
        }
        private void MoveHeldTarget(){
            if(!NetworkServer.active || !launchTarget || !launchTarget.healthComponent.alive)return;
            CharacterBody victim=launchTarget;Ray aim=GetAimRay();float radius=Mathf.Clamp(victim.radius,.3f,3f);
            Vector3 desiredCore=aim.origin+aim.direction*(3f+Mathf.Min(victim.radius,2f));RaycastHit wall;
            Vector3 delta=desiredCore-aim.origin;
            if(Physics.SphereCast(aim.origin,radius,delta.normalized,out wall,delta.magnitude,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))desiredCore=aim.origin+delta.normalized*Mathf.Max(0,wall.distance-.1f);
            Vector3 motion=desiredCore-victim.corePosition;
            if(motion.sqrMagnitude>.001f && Physics.SphereCast(victim.corePosition,radius,motion.normalized,out wall,motion.magnitude,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))desiredCore=victim.corePosition+motion.normalized*Mathf.Max(0,wall.distance-.1f);
            Vector3 desired=desiredCore-(victim.corePosition-victim.transform.position);
            float footOffset=FateGrounding.RootOffset(victim);RaycastHit floor;
            if(Physics.Raycast(desired+Vector3.up*2,Vector3.down,out floor,footOffset+4f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))desired.y=Mathf.Max(desired.y,floor.point.y+footOffset+.04f);
            Vector3 facing=Vector3.ProjectOnPlane(-aim.direction,Vector3.up);Quaternion rotation=facing.sqrMagnitude>.001f?Quaternion.LookRotation(facing):victim.transform.rotation;
            BloodBlind.Lock(victim,Mathf.Max(.02f,releaseAt-fixedAge),desired,rotation);
            TeleportHelper.TeleportGameObject(victim.gameObject,desired);
        }

        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(Charged); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); Charged = reader.ReadBoolean(); }
        private void ReleaseLaunch()
        {
            if (!NetworkServer.active || !launchTarget) return;
            BloodBlind blind = launchTarget.GetComponent<BloodBlind>();
            if (blind) blind.Release(launchVelocity);
            else if (launchTarget.characterMotor) launchTarget.characterMotor.velocity = launchVelocity;
            else { Rigidbody rigid = launchTarget.GetComponent<Rigidbody>(); if (rigid && !rigid.isKinematic) rigid.velocity = launchVelocity; }
            launchTarget = null;
        }
        public override void OnExit() { GetComponent<TemporalController>().HoldingImpale = false; ReleaseLaunch(); DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.StopAttack(); base.OnExit(); }
    }
    public sealed class PuttsunCancel : BaseSkillState
    {
        public override void OnEnter() { base.OnEnter(); DiavoloAudio.Play(characterBody, "cancel"); }
        public override void FixedUpdate() { base.FixedUpdate(); if (isAuthority) outer.SetNextStateToMain(); }
    }
}
