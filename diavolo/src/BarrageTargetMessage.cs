using RoR2;
using R2API.Networking;
using R2API.Networking.Interfaces;
using UnityEngine;
using UnityEngine.Networking;
namespace Diavolo
{
    public sealed class BarrageTargetMessage : INetMessage
    {
        private GameObject owner;
        private HurtBoxReference target;
        public BarrageTargetMessage(){}
        internal static void Publish(GameObject owner,HurtBox target){
            new BarrageTargetMessage{owner=owner,target=HurtBoxReference.FromHurtBox(target)}.Send(NetworkDestination.Clients);
        }
        public void Serialize(NetworkWriter writer){writer.Write(owner);writer.Write(target);}
        public void Deserialize(NetworkReader reader){owner=reader.ReadGameObject();target=reader.ReadHurtBoxReference();}
        public void OnReceived(){
            if(NetworkServer.active || !owner || !owner.GetComponent<DiavoloPresentation>())return;
            var stand=owner.GetComponent<LocalStandCombat.StandPrimaryController>();
            if(stand)stand.FollowContinuous(target.ResolveHurtBox());
        }
    }
}
