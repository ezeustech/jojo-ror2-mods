using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace Diavolo {
 public sealed class StandDefense:MonoBehaviour { public bool Blocking; }
 public sealed class StandBlock:CrimsonState {
  public override void OnEnter(){base.OnEnter();BeginAttack("block",.25f);GetComponent<StandDefense>().Blocking=true;}
  public override void FixedUpdate(){base.FixedUpdate();var visual=GetComponent<DiavoloPresentation>();if(visual)visual.HoldBlock();
   if(isAuthority && (fixedAge>.15f && (!ExtraGamepad.KeyHeld(Plugin.BlockKey.Value) && !ExtraGamepad.Held(Plugin.BlockPad.Value))))outer.SetNextStateToMain();
  }
  public override void OnExit(){GetComponent<StandDefense>().Blocking=false;var visual=GetComponent<DiavoloPresentation>();if(visual)visual.StopAttack();base.OnExit();}
 }
 public sealed class StandJump:CrimsonState {
  public override void OnEnter(){base.OnEnter();BeginAttack("jump",.8f);
   if(NetworkServer.active){RaycastHit floor;if(Physics.Raycast(characterBody.footPosition+Vector3.up*2,Vector3.down,out floor,5,LayerIndex.world.mask,QueryTriggerInteraction.Ignore)){GroundBreakFx.Send(floor.point,7);DiavoloAudio.Impact(characterBody,"impact_ground");new BlastAttack{attacker=gameObject,inflictor=gameObject,teamIndex=characterBody.teamComponent.teamIndex,position=floor.point,radius=7,baseDamage=damageStat*8,baseForce=900,bonusForce=Vector3.up*700,crit=RollCrit(),procCoefficient=1,damageType=DamageType.Stun1s,falloffModel=BlastAttack.FalloffModel.None,attackerFiltering=AttackerFiltering.NeverHitSelf}.Fire();}}
   if(!isAuthority || !characterMotor)return;
   Vector3 heading=inputBank.moveVector;heading.y=0;if(heading.sqrMagnitude<.01f){heading=GetAimRay().direction;heading.y=0;}
   characterMotor.Motor.ForceUnground();characterMotor.velocity=heading.normalized*Mathf.Max(38f,moveSpeedStat*3f)+Vector3.up*28f;
  }
  public override void FixedUpdate(){base.FixedUpdate();if(isAuthority && fixedAge>=.25f)outer.SetNextStateToMain();}
 }
 public sealed class LifeAudio:MonoBehaviour {
  System.Collections.IEnumerator Start(){yield return new WaitForSeconds(.5f);var body=GetComponent<CharacterBody>();if(NetworkServer.active && body && body.healthComponent && body.healthComponent.alive && !GetComponent<TimeEraseDecoy>())DiavoloAudio.Layered(body,"entrance");}
 }
}
