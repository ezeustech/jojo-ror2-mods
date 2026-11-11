using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace EyesOfHeavenDio
{
    // Explicit serialization works without a generated UNet weaver/RPC layer.
    // Only the server writes the stop lifecycle; every client's own camera cleans up.
    public sealed class DioNetworkPresentation : NetworkBehaviour
    {
        private CharacterBody body;
        private bool stopped, presented;
        private float ends;
        private void Awake(){body=GetComponent<CharacterBody>();}
        public void ServerTimeStop(bool active,float seconds=0)
        {
            if(!NetworkServer.active)return;
            stopped=active;ends=Time.time+Mathf.Clamp(seconds,0,15);SetDirtyBit(1u);
        }
        public override bool OnSerialize(NetworkWriter writer,bool initialState)
        { writer.Write(stopped);writer.Write(Mathf.Max(0,ends-Time.time));return true; }
        public override void OnDeserialize(NetworkReader reader,bool initialState)
        {
            bool active=reader.ReadBoolean();float remaining=reader.ReadSingle();
            if(NetworkServer.active)return;
            stopped=active;ends=Time.time+Mathf.Clamp(remaining,0,15);
            if(!body||!DioInput.IsLocal(body))return;
            if(active){presented=true;DioFeedback.MainTimeTransition(false,true,remaining,false,body);}
            else if(presented){DioFeedback.MainTimeTransition(true,true);presented=false;}
        }
        private void Update()
        {
            if(NetworkServer.active||!presented)return;
            if(!body||!body.healthComponent||!body.healthComponent.alive||!Run.instance){Finish();return;}
            // Resume audio is a single server-relayed cue, shared with teammates.
            // Bounded fallback for disconnects, lost state, or a despawn during stop.
            if(Time.time>=ends+.5f)Finish();
        }
        private void Finish(){if(presented)DioFeedback.MainTimeTransition(true,true);presented=false;stopped=false;}
        private void OnDisable(){Finish();}
    }
}
