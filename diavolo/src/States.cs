using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Diavolo
{
    public abstract class CrimsonState : BaseSkillState
    {
        protected void UpdatePrimaryPose(float previous,float age,float duration) {
            DiavoloPresentation visual=GetComponent<DiavoloPresentation>();
            if(visual)visual.PrimaryTime(previous,age,duration);
        }
        protected void EndPrimaryPose() {
            DiavoloPresentation visual=GetComponent<DiavoloPresentation>();
            if(visual)visual.EndPrimary();
        }
        protected void BeginAttack(string animation, float duration, bool aimUser = true)
        {
            TemporalController time = GetComponent<TemporalController>();
            if (time) time.BreakForAttack();
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>();
            if (visual) visual.Attack(animation, duration, !aimUser);
            if (aimUser) StartAimMode(duration + 1f);
        }
        protected void Strike(float coefficient, float reach, float radius, DamageType type = DamageType.Generic)
        {
            StrikeFrom(GetAimRay(), coefficient, reach, radius, type);
        }
        protected void StrikeFrom(Ray aim, float coefficient, float reach, float radius, DamageType type = DamageType.Generic)
        {
            // Fate resolution and damage happen on the same server, in that order.
            if (!NetworkServer.active) return;
            new BulletAttack {
                owner = gameObject, weapon = gameObject, origin = aim.origin, aimVector = aim.direction,
                minSpread = 0f, maxSpread = 0f, bulletCount = 1, damage = damageStat * coefficient,
                force = 180f, radius = radius, maxDistance = reach, isCrit = RollCrit(),
                procCoefficient = 1f, falloffModel = BulletAttack.FalloffModel.None,
                damageType = type,
                smartCollision = true, stopperMask = LayerIndex.world.mask,
                hitEffectPrefab = null
            }.Fire();
        }
        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.PrioritySkill; }
    }

    public sealed class KingCrimson : CrimsonState
    {
        private float duration;
        private bool fired;
        public override void OnEnter() { base.OnEnter(); duration = .48f / attackSpeedStat; BeginAttack("chop", duration); }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!fired && fixedAge >= duration * .25f) { fired = true; Strike(6f, 8f, 1.8f, DamageType.BleedOnHit); DiavoloAudio.Impact(characterBody, "impact_chop"); }
            if (isAuthority && fixedAge >= duration) outer.SetNextStateToMain();
        }
    }
    public sealed class Impale : CrimsonState
    {
        public float ChargeFraction { get { return Mathf.Clamp01(fixedAge / 1.8f); } }

        public override void OnEnter()
        {
            base.OnEnter(); StartAimMode(4f);
            TemporalController time = GetComponent<TemporalController>(); if (time && time.Erasing) time.HoldingImpale = true;
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.Attack("charge", 2f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && ((!inputBank.skill2.down && fixedAge >= .08f) || fixedAge >= 2f)) {
                outer.SetNextState(new ImpaleImpact { Charged = CombatRules.FullyCharged(fixedAge) });
            }
        }
        public override void OnExit() { GetComponent<TemporalController>().HoldingImpale = false; base.OnExit(); }
    }
    public sealed class CrimsonBarrage : CrimsonState
    {
        private float nextPunch=.12f,nextVoice;
        private float nextSync;
        private HurtBox lastSent;
        private LocalStandCombat.StandPrimaryController stand;
        private DiavoloPresentation visual;
        public override void OnEnter()
        {
            base.OnEnter();
            TemporalController time=GetComponent<TemporalController>();if(time)time.BreakForAttack();
            stand=LocalStandCombat.StandPrimaryController.Of(gameObject);
            stand.BeginContinuous(GetAimRay(),NetworkServer.active);
            visual=GetComponent<DiavoloPresentation>();if(visual)visual.SustainBarrage(true);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            bool decoy=GetComponent<TimeEraseDecoy>();
            if(isAuthority && (decoy ? fixedAge>=2f : (!ExtraGamepad.KeyHeld(Plugin.BarrageKey.Value) && !ExtraGamepad.Held(Plugin.GamepadMoves[2].Value)))){outer.SetNextStateToMain();return;}
            stand.TickContinuous(GetAimRay(),Time.fixedDeltaTime,NetworkServer.active);
            if(NetworkServer.active && (stand.Target!=lastSent || fixedAge>=nextSync)){
                lastSent=stand.Target;nextSync=fixedAge+.5f;
                BarrageTargetMessage.Publish(gameObject,lastSent);
            }
            if(visual)visual.SustainBarrage(false);
            Ray ray=GetAimRay();
            bool ready=stand.Active ? stand.TryAttackRay(out ray) : !stand.Visible;
            if(!stand.Active)ray=GetAimRay();
            if(!ready){nextPunch=fixedAge+.06f;return;}
            if(fixedAge>=nextVoice){nextVoice=fixedAge+Mathf.Max(1f,Mathf.Max(DiavoloAudio.Duration("barrage_voice",2f),DiavoloAudio.Duration("barrage_sfx",2f)));DiavoloAudio.Play(characterBody,"barrage");}
            if(fixedAge>=nextPunch){
                nextPunch=fixedAge+Mathf.Max(.06f,.12f/LocalStandCombat.StandPrimaryRules.AnimationSpeed(attackSpeedStat));
                StrikeFrom(ray,1.8f,stand.Active?3.5f:8f,1.8f,DamageType.Stun1s);
            }
        }
        public override void OnExit()
        {
            if(stand)stand.StopContinuous();
            if(visual)visual.StopAttack();
            DiavoloAudio.StopSfx(characterBody);
            base.OnExit();
        }
    }
    public sealed class Epitaph : BaseSkillState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.Support(EpitaphVision.Duration);
            TemporalController time = GetComponent<TemporalController>();
            if (!time || !time.Erasing) DiavoloAudio.Play(characterBody, "epitaph");
            if (time) time.BeginEpitaph();
            if (time && time.Erasing) time.MarkFuture();
            else if (isAuthority && NetworkClient.active) {
                EpitaphVision vision = GetComponent<EpitaphVision>();
                if (!vision) vision = gameObject.AddComponent<EpitaphVision>();
                vision.Reveal();
            }
        }
        public override void FixedUpdate() { base.FixedUpdate(); if (isAuthority && fixedAge >= .1f) outer.SetNextStateToMain(); }
    }
    public sealed class TimeErase : BaseSkillState
    {
        public override void OnEnter() { base.OnEnter(); GetComponent<TemporalController>().Begin(); }
        public override void FixedUpdate() { base.FixedUpdate(); if (isAuthority && fixedAge >= .1f) outer.SetNextStateToMain(); }
    }
    public sealed class TimeSkip : BaseSkillState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            TemporalController time = GetComponent<TemporalController>(); if (!time || !time.Erasing) DiavoloAudio.Play(characterBody, "skip");
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.Support(.4f);
            if (NetworkServer.active) characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, .3f);
            TimeOverlay.Send(characterBody, 3);
            if (!isAuthority) return;
            CrimsonFx.Burst(characterBody.corePosition, "5dvl01_eff_timestep00_0", 4f, .4f);
            Ray aim = GetAimRay();
            Vector3 destination;
            if (CursorSkip.Destination(characterBody, aim, out destination)) {
                TeleportHelper.TeleportGameObject(gameObject, destination);
                if (characterMotor) characterMotor.velocity = Vector3.zero;
                Physics.SyncTransforms();
            }
            CrimsonFx.Burst(characterBody.corePosition, "5dvl01_eff_trail00_0", 4f, .4f);
        }
        public override void FixedUpdate() { base.FixedUpdate(); if (isAuthority && fixedAge >= .12f) outer.SetNextStateToMain(); }
    }
    public sealed class BloodThrow : BaseSkillState
    {
        private bool thrown;
        public override void OnEnter()
        {
            base.OnEnter(); StartAimMode(1f);
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>();
            if (visual) visual.Attack("blood", .6f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!thrown && fixedAge >= .18f) {
                thrown = true;
                DiavoloPresentation visual = GetComponent<DiavoloPresentation>();
                if (visual) visual.Blood(GetAimRay());
                if (NetworkServer.active) {
                    Ray aim = GetAimRay();
                    foreach (CharacterBody enemy in CharacterBody.readOnlyInstancesList) {
                        if (!TemporalController.IsEnemy(characterBody, enemy) || enemy.isPlayerControlled) continue;
                        Vector3 offset = enemy.corePosition - aim.origin;
                        float along = Vector3.Dot(aim.direction, offset);
                        float width = 1.2f + along * .10f + Mathf.Min(3f, enemy.radius);
                        if (along < 0 || along > 48f || (offset-aim.direction*along).sqrMagnitude > width*width) continue;
                        if (Physics.Linecast(aim.origin, enemy.corePosition, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                        BloodBlind.Lock(enemy, 6f, enemy.transform.position, enemy.transform.rotation);
                        BloodFx.Impact(enemy.corePosition, aim.direction, false);
                    }
                }
            }
            if (isAuthority && fixedAge >= Mathf.Max(.2f, .6f / attackSpeedStat)) outer.SetNextStateToMain();
        }
    }
}
