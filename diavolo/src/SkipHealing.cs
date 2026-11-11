using RoR2;
using UnityEngine.Networking;
namespace Diavolo {
 public static class SkipHealing {
  public static void Apply(CharacterBody body) {
   if(!NetworkServer.active || !body || body.GetComponent<TimeEraseDecoy>() || !body.healthComponent || !body.healthComponent.alive)return;
   body.healthComponent.Heal(body.healthComponent.fullHealth*.15f,default(ProcChainMask),true);
  }
 }
}
