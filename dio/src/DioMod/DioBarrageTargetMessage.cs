using RoR2;
using R2API.Networking;
using R2API.Networking.Interfaces;
using UnityEngine;
using UnityEngine.Networking;
namespace EyesOfHeavenDio
{
    public sealed class DioBarrageTargetMessage : INetMessage
    {
        private GameObject owner;
        private HurtBoxReference target;
        private bool relayed;
        public DioBarrageTargetMessage(){}
        internal static void Publish(GameObject owner,HurtBox target){
            new DioBarrageTargetMessage{owner=owner,target=HurtBoxReference.FromHurtBox(target),relayed=NetworkServer.active}
                .Send(NetworkServer.active?NetworkDestination.Clients:NetworkDestination.Server);
        }
        public void Serialize(NetworkWriter writer){writer.Write(owner);writer.Write(target);writer.Write(relayed);}
        public void Deserialize(NetworkReader reader){owner=reader.ReadGameObject();target=reader.ReadHurtBoxReference();relayed=reader.ReadBoolean();}
        public void OnReceived(){
            if(!owner || !owner.GetComponent<DioVisualDriver>())return;
            var body=owner.GetComponent<CharacterBody>();
            var stand=owner.GetComponent<LocalStandCombat.StandPrimaryController>();
            if(!body || !stand || !stand.Continuous)return;
            if(NetworkServer.active){if(relayed)return;relayed=true;this.Send(NetworkDestination.Clients);}
            else if(!relayed)return;
            if(!body.hasEffectiveAuthority)stand.FollowContinuous(target.ResolveHurtBox());
        }
    }
}
