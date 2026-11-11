using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
namespace Diavolo
{
    public sealed class CrimsonUltimate : CrimsonState
    {
        private CharacterBody target;
        private bool grabbed, resolved;
        private Vector3 forward;
        public override void OnEnter()
        {
            base.OnEnter(); BeginAttack("ultimate",1.5f);
            if(!NetworkServer.active)return;
            Ray aim=GetAimRay(); double best=double.PositiveInfinity;
            forward=Vector3.ProjectOnPlane(aim.direction,Vector3.up).normalized;
            if(forward.sqrMagnitude<.01f)forward=characterDirection.forward;
            // A direct crosshair hit wins over nearby enemies in the assist cone.
            float directDistance=UltimateAimRules.Range;
            RaycastHit wall;
            if(Physics.Raycast(aim,out wall,directDistance,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))directDistance=wall.distance;
            foreach(RaycastHit hit in Physics.RaycastAll(aim,directDistance,LayerIndex.entityPrecise.mask,QueryTriggerInteraction.Collide)) {
                HurtBox hurt=hit.collider.GetComponent<HurtBox>();
                CharacterBody enemy=hurt && hurt.healthComponent ? hurt.healthComponent.body : null;
                if(!TemporalController.IsEnemy(characterBody,enemy) || enemy.isPlayerControlled || hit.distance>directDistance)continue;
                directDistance=hit.distance;target=enemy;
            }
            if(!target) foreach(CharacterBody enemy in CharacterBody.readOnlyInstancesList) {
                if(!TemporalController.IsEnemy(characterBody,enemy) || enemy.isPlayerControlled)continue;
                Vector3 delta=enemy.corePosition-aim.origin;
                double score=UltimateAimRules.Score(Vector3.Angle(aim.direction,delta),delta.magnitude,enemy.radius);
                if(score>=best || Physics.Linecast(aim.origin,enemy.corePosition,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))continue;
                best=score;target=enemy;
            }
            if(target) UltimateMessage.Broadcast(gameObject,target.gameObject,0,null,0,0);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(NetworkServer.active && !resolved && target && target.healthComponent.alive && characterBody.healthComponent.alive) {
                if(!grabbed && fixedAge>=.35f) {
                    // Keep the initial target despite aim changes and ordinary dodges.
                    if(Vector3.Distance(characterBody.corePosition,target.corePosition)>60f+target.radius || Physics.Linecast(characterBody.corePosition,target.corePosition,LayerIndex.world.mask,QueryTriggerInteraction.Ignore)) {target=null;return;}
                    Vector3 a,b;Vector3[] points;
                    if(!UltimatePlacement.Find(characterBody,target,forward,out a,out b,out points)) {target=null;return;}
                    grabbed=true;
                    characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility,.75f);
                }
                if(grabbed) BloodBlind.Lock(target,.25f,target.transform.position,target.transform.rotation);
                if(grabbed && fixedAge>=.8f) {
                    resolved=true;
                    Vector3 ownerRoot,victimRoot;Vector3[] points;
                    if(!UltimatePlacement.Find(characterBody,target,forward,out ownerRoot,out victimRoot,out points))return;
                    TeleportHelper.TeleportGameObject(gameObject,ownerRoot);
                    TeleportHelper.TeleportGameObject(target.gameObject,victimRoot);
                    if(characterMotor)characterMotor.velocity=Vector3.zero;
                    if(characterDirection)characterDirection.forward=forward;
                    Quaternion facing=Quaternion.LookRotation(forward);
                    target.transform.rotation=facing;
                    if(target.characterDirection)target.characterDirection.forward=forward;
                    BloodBlind.Lock(target,2.5f,victimRoot,facing);
                    Physics.SyncTransforms();
                    TimeOverlay.Send(characterBody,2);
                    UltimateMessage.Broadcast(gameObject,target.gameObject,1,points,UltimatePlacement.Height(target),Mathf.Max(.8f,target.radius*2f));
                    DamageInfo damage=new DamageInfo {attacker=gameObject,inflictor=gameObject,damage=damageStat*60f,crit=RollCrit(),procCoefficient=1f,position=target.corePosition,damageType=DamageType.BypassArmor|DamageType.BleedOnHit};
                    target.healthComponent.TakeDamage(damage);
                    GlobalEventManager.instance.OnHitEnemy(damage,target.gameObject);GlobalEventManager.instance.OnHitAll(damage,target.gameObject);
                    if(!damage.rejected) {
                        BloodFx.Impact(target.corePosition,Vector3.up,true);
                        UltimateMessage.Broadcast(gameObject,target.gameObject,2,null,0,0);
                    }
                }
            }
            if(isAuthority && fixedAge>=1.5f)outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            DiavoloPresentation visual=GetComponent<DiavoloPresentation>();if(visual)visual.StopAttack();
            base.OnExit();
        }
    }
    public sealed class UltimateMessage : INetMessage
    {
        private GameObject owner,target; private byte phase; private Vector3[] points; private float height,width;
        public UltimateMessage(){}
        public static void Broadcast(GameObject owner,GameObject target,byte phase,Vector3[] points,float height,float width)
        {
            UltimateMessage message=new UltimateMessage {owner=owner,target=target,phase=phase,points=points,height=height,width=width};
            if(phase==2){DiavoloAudio.Impact(owner.GetComponent<CharacterBody>(),"erase");DiavoloAudio.Impact(owner.GetComponent<CharacterBody>(),"ultimate_slice");}
            if(phase==1)DiavoloAudio.Impact(owner.GetComponent<CharacterBody>(),"impact_impale");
            if(NetworkClient.active)message.Apply();message.Send(NetworkDestination.Clients);
        }
        public void Serialize(NetworkWriter w) {w.Write(owner);w.Write(target);w.Write(phase);w.Write(height);w.Write(width);if(phase==1)for(int i=0;i<3;i++)w.Write(points[i]);}
        public void Deserialize(NetworkReader r) {owner=r.ReadGameObject();target=r.ReadGameObject();phase=r.ReadByte();height=r.ReadSingle();width=r.ReadSingle();if(phase==1){points=new Vector3[3];for(int i=0;i<3;i++)points[i]=r.ReadVector3();}}
        public void OnReceived(){if(!NetworkServer.active)Apply();}
        private void Apply()
        {
            if(!owner)return;
            DiavoloPresentation visual=owner.GetComponent<DiavoloPresentation>();
            if(phase==0 && visual && target)visual.UltimateLunge(target.GetComponent<CharacterBody>());
            if(phase==1) {
                if(visual)visual.UltimateFinish();
                CrimsonSpikes.Create(points,height,width);

            }
        }
    }
}
