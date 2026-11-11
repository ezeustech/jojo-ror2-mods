using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using R2API.Networking;
using R2API.Networking.Interfaces;
namespace Diavolo {
 public static class GroundBreakFx {
  static Material stone;
  public static void Send(Vector3 point,float radius){if(!NetworkServer.active)return;if(NetworkClient.active)Show(point,radius);new GroundBreakMessage(point,radius).Send(NetworkDestination.Clients);}
  public static void Show(Vector3 point,float radius){
   if(!stone){var shader=DiavoloPresentation.Asset<Shader>("Assets/CrimsonModel.shader");if(!shader)return;stone=new Material(shader);stone.color=new Color(.16f,.13f,.13f);}
   for(int i=0;i<18;i++){float angle=i*137.508f;float distance=radius*Mathf.Sqrt((i+.5f)/18f);Vector3 offset=Quaternion.Euler(0,angle,0)*Vector3.forward*distance;RaycastHit floor;
    if(!Physics.Raycast(point+offset+Vector3.up*2,Vector3.down,out floor,4,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))continue;
    var rock=GameObject.CreatePrimitive(PrimitiveType.Cube);rock.name="Crimson ground fracture";var collider=rock.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);rock.AddComponent<ErasureVisible>();rock.GetComponent<Renderer>().sharedMaterial=stone;
    rock.transform.position=floor.point;rock.transform.rotation=Quaternion.Euler(20,angle,15);rock.transform.localScale=new Vector3(.25f+radius*.055f,.18f,.45f+radius*.08f);var motion=rock.AddComponent<GroundFragment>();motion.Velocity=offset.normalized*2+Vector3.up*(3f+distance*.25f);
   }
  }
 }
 public sealed class GroundFragment:MonoBehaviour {public Vector3 Velocity;float age;void Update(){age+=Time.deltaTime;Velocity+=Vector3.down*12*Time.deltaTime;transform.position+=Velocity*Time.deltaTime;transform.Rotate(30*Time.deltaTime,50*Time.deltaTime,0);if(age>.65f)transform.localScale*=Mathf.Exp(-7*Time.deltaTime);if(age>1.1f)Destroy(gameObject);}}
 public sealed class GroundBreakMessage:INetMessage {Vector3 position;float radius;public GroundBreakMessage(){}public GroundBreakMessage(Vector3 p,float r){position=p;radius=r;}public void Serialize(NetworkWriter w){w.Write(position);w.Write(radius);}public void Deserialize(NetworkReader r){position=r.ReadVector3();radius=r.ReadSingle();}public void OnReceived(){if(!NetworkServer.active)GroundBreakFx.Show(position,Mathf.Clamp(radius,1,10));}}
 public sealed class BarrageFinish:CrimsonState {bool fired;float duration;public override void OnEnter(){base.OnEnter();duration=.5f/attackSpeedStat;BeginAttack("chain3",duration);}public override void FixedUpdate(){base.FixedUpdate();if(!fired && fixedAge>=duration*.35f){fired=true;Strike(6,9,2);DiavoloAudio.Impact(characterBody,"impact_heavy");}if(isAuthority && fixedAge>=duration)outer.SetNextStateToMain();}}
}
