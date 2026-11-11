using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;
namespace Diavolo {
 public sealed class AudioCueMessage:INetMessage {
  private GameObject owner;private string cue;private byte operation;private uint sequence;
  public AudioCueMessage(){}
  public AudioCueMessage(GameObject owner,string cue,byte operation,uint sequence){this.owner=owner;this.cue=cue;this.operation=operation;this.sequence=sequence;}
  public void Serialize(NetworkWriter w){w.Write(owner);w.Write(cue);w.Write(operation);w.Write(sequence);}
  public void Deserialize(NetworkReader r){owner=r.ReadGameObject();cue=r.ReadString();operation=r.ReadByte();sequence=r.ReadUInt32();}
  public void OnReceived(){if(!NetworkServer.active && owner && owner.GetComponent<DiavoloPresentation>() && operation<=5 && cue!=null && cue.Length<80 && cue.IndexOfAny(new[]{'/','\\',':'})<0)DiavoloAudio.ReceiveNetwork(owner.GetComponent<RoR2.CharacterBody>(),cue,operation,sequence);}
 }
}
