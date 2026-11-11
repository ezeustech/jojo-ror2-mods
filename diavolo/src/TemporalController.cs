using System.Collections.Generic;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;

namespace Diavolo
{
    public struct FateMark
    {
        public CharacterBody Body;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public sealed class TemporalController : MonoBehaviour
    {
        public const float EraseDuration = 22.894875f;
        public const float MarkRadius = 60f;
        public bool Erasing { get; private set; }
        public bool HoldingImpale;
        private readonly TemporalCombatWindow combat = new TemporalCombatWindow();
        private float lastDodgeVisual;
        public bool Dodging { get { return body && body.healthComponent && combat.Dodging(Time.time, body.healthComponent.alive); } }
        public void BeginEpitaph()
        {
            if (!NetworkServer.active) return;
            combat.BeginEpitaph(Time.time, EpitaphVision.Duration);
            body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, EpitaphVision.Duration);
        }
        public void Dodge()
        {
            if (Time.time < lastDodgeVisual + .12f) return;
            lastDodgeVisual = Time.time;
            ShowDodge();
            new TemporalMessage(gameObject, 7, new List<FateMark>()).Send(NetworkDestination.Clients);
        }
        internal void ShowDodge() { DiavoloPresentation visual = GetComponent<DiavoloPresentation>(); if (visual) visual.Dodge(); }
        public bool TakeAmbush()
        {
            return combat.TakeAmbush(Erasing, body.healthComponent && body.healthComponent.alive);
        }
        public void RestoreAmbush() { combat.RestoreAmbush(); }
        private CharacterBody body;
        private float expires, nextTargetClear;
        private readonly List<FateMark> marks = new List<FateMark>();
        private CharacterModel model;
        private CharacterMaster decoy;

        private void Awake() { body = GetComponent<CharacterBody>(); }

        public static bool IsEnemy(CharacterBody owner, CharacterBody target)
        {
            return owner && owner.teamComponent && target && target != owner && target.teamComponent
                && target.healthComponent && target.healthComponent.alive
                && TeamMask.GetEnemyTeams(owner.teamComponent.teamIndex).HasTeam(target.teamComponent.teamIndex);
        }

        public void Begin()
        {
            if (!NetworkServer.active || Erasing || GetComponent<TimeEraseDecoy>()) return;
            ApplyBegin();
            decoy = TimeEraseDecoy.Spawn(body);
            new TemporalMessage(gameObject, 0, marks).Send(NetworkDestination.Clients);
            TimeOverlay.Send(body, 0);
        }

        internal void ApplyBegin()
        {
            if (Erasing) return;
            DiavoloAudio.EraseBass(body, true);
            DiavoloAudio.Layered(body, "erase");
            EpitaphVision activeVision=GetComponent<EpitaphVision>();if(activeVision)activeVision.enabled=false;
            Erasing = true; expires = Time.time + EraseDuration; marks.Clear();
            combat.BeginErase();
            if (NetworkServer.active) {
                body.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
                body.AddBuff(RoR2Content.Buffs.Cloak);
            }
            if (body.hurtBoxGroup) body.hurtBoxGroup.hurtBoxesDeactivatorCounter++;
            if (body.modelLocator && body.modelLocator.modelTransform) {
                model = body.modelLocator.modelTransform.GetComponent<CharacterModel>();
                if (model) model.invisibilityCount++;
            }
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>();
            if (visual) visual.SetErased(true);
        }

        private void FixedUpdate()
        {
            if (!Erasing) return;
            if (!body.healthComponent || !body.healthComponent.alive) { End(false); return; }
            if (NetworkServer.active) {
                if (Time.time >= expires) { End(false); return; }
                if (Time.time < nextTargetClear) return; nextTargetClear = Time.time + .2f;
                // Cloak + disabled hurtboxes prevents acquisition; discard already-held targets.
                foreach (CharacterBody enemy in CharacterBody.readOnlyInstancesList) {
                    if (!IsEnemy(body, enemy) || !enemy.master) continue;
                    foreach (BaseAI ai in enemy.master.GetComponents<BaseAI>()) {
                        if (ai.currentEnemy.gameObject == gameObject) { ai.currentEnemy.Reset(); ai.enemyAttention = 0f; }
                        if (ai.customTarget.gameObject == gameObject) ai.customTarget.Reset();
                    }
                }
                if (Time.time >= expires) End(false);
            }
        }

        public void MarkFuture()
        {
            if (!NetworkServer.active || !Erasing) return;
            marks.Clear();
            MapZone[] zones = Object.FindObjectsOfType<MapZone>();
            foreach (CharacterBody enemy in CharacterBody.readOnlyInstancesList) {
                if (!IsEnemy(body, enemy) || enemy.isPlayerControlled || Vector3.Distance(body.corePosition, enemy.corePosition) > MarkRadius) continue;
                Quaternion rotation = enemy.characterDirection ? Quaternion.Euler(0, enemy.characterDirection.yaw, 0) : enemy.transform.rotation;
                Vector3 position;
                if (!AccessibleFate(enemy, zones, out position)) continue;
                marks.Add(new FateMark { Body = enemy, Position = position, Rotation = rotation });
            }
            ShowMarks(marks);
            new TemporalMessage(gameObject, 1, marks).Send(NetworkDestination.Clients);
        }

        internal void ShowMarks(List<FateMark> received)
        {
            if (!body.hasEffectiveAuthority || !NetworkClient.active) return;
            EpitaphVision vision = GetComponent<EpitaphVision>();
            if (!vision) vision = gameObject.AddComponent<EpitaphVision>();
            vision.RevealFrozen(received, Mathf.Max(.1f, expires - Time.time));
        }

        private bool AccessibleFate(CharacterBody enemy, MapZone[] zones, out Vector3 result)
        {
            result = enemy.transform.position;
            float radius, height; FateGrounding.Dimensions(enemy, out radius, out height);
            bool flyer = FateGrounding.IsFlyer(enemy);
            Vector3 originFoot = result - Vector3.up * FateGrounding.RootOffset(enemy);
            if (!flyer && !FatePlacementGeometry.Ground(originFoot, 1.5f, Mathf.Max(24, height * 2), LayerIndex.world.mask, out originFoot)) return false;
            // Independent random samples around THIS enemy, never a ring around Diavolo.
            for (int attempt = 0; attempt < 32; attempt++) {
                Vector2 offset = Random.insideUnitCircle * Mathf.Clamp(radius * 2 + 3, 4, 8);
                if (offset.sqrMagnitude < 1f) continue;
                Vector3 foot = originFoot + new Vector3(offset.x, 0, offset.y);
                if (flyer) {
                    result = enemy.transform.position + new Vector3(offset.x, Random.Range(-1f, 1f), offset.y);
                    Vector3 motion = result - enemy.transform.position;
                    RaycastHit obstruction;
                    if (Physics.CheckSphere(result, radius, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) || Physics.SphereCast(enemy.corePosition, radius, motion.normalized, out obstruction, motion.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                } else {
                    if (!FatePlacementGeometry.Ground(foot, 1.5f, 2.5f, LayerIndex.world.mask, out foot) || !FatePlacementGeometry.Fits(foot, radius, height, LayerIndex.world.mask) || !FatePlacementGeometry.Connected(originFoot, foot, radius, height, LayerIndex.world.mask)) continue;
                    result = foot + Vector3.up * FateGrounding.RootOffset(enemy);
                }
                if (!InsideMap(zones, enemy.corePosition, result, radius, height)) continue;
                bool occupied = false;
                foreach (FateMark mark in marks) {
                    float otherRadius, otherHeight; FateGrounding.Dimensions(mark.Body, out otherRadius, out otherHeight);
                    if (Vector3.ProjectOnPlane(mark.Position-result,Vector3.up).magnitude < radius + otherRadius + 2f) { occupied = true; break; }
                }
                if(!occupied)foreach(CharacterBody other in CharacterBody.readOnlyInstancesList){
                    if(!other || other==enemy || !other.healthComponent || !other.healthComponent.alive)continue;
                    bool relocating=false;foreach(FateMark existing in marks)if(existing.Body==other){relocating=true;break;}
                    if(relocating)continue;
                    if(Mathf.Abs(other.footPosition.y-foot.y)<height && Vector3.ProjectOnPlane(other.footPosition-foot,Vector3.up).magnitude<radius+other.radius+.5f){occupied=true;break;}
                }
                if (!occupied) return true;
            }
            // Omitting a mark is safer than teleporting to an unchecked fallback.
            return false;
        }

        private static bool InsideMap(MapZone[] zones, Vector3 original, Vector3 root, float radius, float height)
        {
            foreach (MapZone zone in zones) {
                if (!zone || !zone.isActiveAndEnabled || (zone.zoneType != MapZone.ZoneType.OutOfBounds && zone.zoneType != MapZone.ZoneType.KillEverything)) continue;
                for (int i = 0; i < 5; i++) {
                    Vector3 point = root + (i == 0 ? Vector3.zero : Quaternion.Euler(0, (i-1)*90, 0)*Vector3.forward*radius);
                    bool inside = zone.IsPointInsideMapZone(point);
                    if (zone.triggerType == MapZone.TriggerType.TriggerEnter && inside) return false;
                    if (zone.triggerType == MapZone.TriggerType.TriggerExit && zone.IsPointInsideMapZone(original) && !inside) return false;
                }
            }
            return true;
        }

        private static bool GroundFate(CharacterBody enemy, Vector3 desired, out Vector3 result)
        {
            result = desired;
            float radius, height; FateGrounding.Dimensions(enemy, out radius, out height);
            Vector3 foot;
            if (!FatePlacementGeometry.Ground(desired - Vector3.up * FateGrounding.RootOffset(enemy), .5f, 1f, LayerIndex.world.mask, out foot) || !FatePlacementGeometry.Fits(foot, radius, height, LayerIndex.world.mask)) return false;
            result = foot + Vector3.up * FateGrounding.RootOffset(enemy);
            return true;
        }

        // Called at attack entry, before windup/damage, never for BloodThrow.
        public void BreakForAttack(TemporalAttack attack = TemporalAttack.Ordinary, AttackMoment moment = AttackMoment.Start) { if (NetworkServer.active && Erasing && CombatRules.EndsErasure(attack, moment)) End(true); }

        private void End(bool resolve)
        {
            if (!Erasing) return;
            if (NetworkServer.active) combat.EndErase(body && body.healthComponent && body.healthComponent.alive);
            if (NetworkServer.active && resolve) {
                foreach (FateMark mark in marks) {
                    if (!IsEnemy(body, mark.Body) || mark.Body.isPlayerControlled) continue;
                    Vector3 resolved=mark.Position;
                    if(!FateGrounding.IsFlyer(mark.Body) && !GroundFate(mark.Body,resolved,out resolved))continue;
                    TeleportHelper.TeleportGameObject(mark.Body.gameObject, resolved, mark.Rotation, true);
                    if(mark.Body.characterMotor && mark.Body.characterMotor.Motor)mark.Body.characterMotor.Motor.SetPositionAndRotation(resolved,mark.Rotation);
                    mark.Body.transform.rotation = mark.Rotation;
                    if (mark.Body.characterDirection) { mark.Body.characterDirection.yaw = mark.Rotation.eulerAngles.y; }
                    if (mark.Body.characterMotor) mark.Body.characterMotor.velocity = Vector3.zero;
                    // A short enforced hold makes the enemy actually occupy the recorded pose.
                    BloodBlind.Lock(mark.Body, .4f, resolved, mark.Rotation);
                }
            }
            ApplyEnd();
            if (body && body.healthComponent && body.healthComponent.alive) TimeOverlay.Send(body, 1);
            if (NetworkServer.active) new TemporalMessage(gameObject, 2, marks).Send(NetworkDestination.Clients);
            marks.Clear();
        }

        internal void ApplyEnd()
        {
            if (!Erasing) return;
            Erasing = false;
            DiavoloAudio.EraseBass(body, false);
            if (body && body.healthComponent && body.healthComponent.alive) DiavoloAudio.Layered(body, "resume");
            HoldingImpale = false;
            if (NetworkServer.active && body) { body.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility); body.RemoveBuff(RoR2Content.Buffs.Cloak); }
            if (body && body.hurtBoxGroup) body.hurtBoxGroup.hurtBoxesDeactivatorCounter--;
            if (model) model.invisibilityCount--;
            EpitaphVision vision = GetComponent<EpitaphVision>();
            if (vision) vision.enabled = false;
            DiavoloPresentation visual = GetComponent<DiavoloPresentation>();
            if (visual) visual.SetErased(false);
            // Return VFX is started above, then the diversion is removed without
            // a kill event, rewards, or on-death item triggers.
            if (NetworkServer.active) { TimeEraseDecoy.Remove(decoy); decoy = null; }
        }
        private void OnDisable() { End(false); if (NetworkServer.active) { TimeEraseDecoy.Remove(decoy); decoy = null; } }
    }

    // Only the server sends temporal outcomes. Clients never provide teleport destinations.
    public sealed class TemporalMessage : INetMessage
    {
        private GameObject owner;
        private byte operation;
        private List<FateMark> marks = new List<FateMark>();
        public TemporalMessage() { }
        public TemporalMessage(GameObject owner, byte operation, List<FateMark> marks) { this.owner = owner; this.operation = operation; this.marks = new List<FateMark>(marks); }
        public void Serialize(NetworkWriter writer)
        {
            writer.Write(owner); writer.Write(operation); writer.Write(marks.Count);
            foreach (FateMark mark in marks) { writer.Write(mark.Body ? mark.Body.gameObject : null); writer.Write(mark.Position); writer.Write(mark.Rotation); }
        }
        public void Deserialize(NetworkReader reader)
        {
            owner = reader.ReadGameObject(); operation = reader.ReadByte(); int count = reader.ReadInt32();
            if (count < 0 || count > 4096) throw new System.InvalidOperationException("Invalid fate mark count");
            marks.Clear();
            for (int i = 0; i < count; i++) { GameObject target = reader.ReadGameObject(); Vector3 position = reader.ReadVector3(); Quaternion rotation = reader.ReadQuaternion(); if (target) marks.Add(new FateMark { Body = target.GetComponent<CharacterBody>(), Position = position, Rotation = rotation }); }
        }
        public void OnReceived()
        {
            if (NetworkServer.active || !owner) return;
            TemporalController controller = owner.GetComponent<TemporalController>();
            if (!controller) return;
            if (operation == 0) controller.ApplyBegin();
            else if (operation == 1) controller.ShowMarks(marks);
            else if (operation == 2) controller.ApplyEnd();
            else if (operation == 3 || operation == 4) { CounterController counter = owner.GetComponent<CounterController>(); if (counter) counter.ShowCounter(operation == 4); }
            else if (operation == 6 && marks.Count > 0) { DiavoloPresentation visual = owner.GetComponent<DiavoloPresentation>(); if (visual) visual.SetGrabTarget(marks[0].Body); }
            else if (operation == 7) controller.ShowDodge();
            else if (operation == 8 && marks.Count > 0) { DiavoloPresentation visual = owner.GetComponent<DiavoloPresentation>(); if (visual) visual.ImpaleContact(marks[0].Body); }
        }
    }
}
