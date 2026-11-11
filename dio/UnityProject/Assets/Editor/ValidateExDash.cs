using System;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateExDash
{
 public static void Run()
 {
  GameObject Box(Vector3 p,Vector3 s){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.transform.position=p;o.transform.localScale=s;return o;}
  var floor=Box(new Vector3(0,-.5f,0),new Vector3(100,1,100));Physics.SyncTransforms();
  Vector3 Move(Vector3 start,Vector3 end)=>DioDashCollision.Move(start,end,Vector3.up,.5f,1,0,1);
  var flat=Move(new Vector3(0,.08f,0),new Vector3(0,-3,10));
  if(flat.y<.079f||flat.z<9.5f)throw new Exception("Down-aimed dash should travel above floor: "+flat);
  var wall=Box(new Vector3(0,2,5),new Vector3(4,4,.3f));Physics.SyncTransforms();
  var stopped=Move(new Vector3(0,.08f,0),new Vector3(0,.08f,10));
  if(stopped.z>4.4f||stopped.z<3.5f)throw new Exception("Dash must stop before wall: "+stopped);
  UnityEngine.Object.DestroyImmediate(wall);
  var slope=Box(new Vector3(0,.8f,6),new Vector3(5,.2f,8));slope.transform.rotation=Quaternion.Euler(-12,0,0);Physics.SyncTransforms();
  var safe=Move(new Vector3(0,.08f,0),new Vector3(0,.08f,10));
  if(Physics.CheckCapsule(safe+Vector3.up*1.5f,safe+Vector3.up*.5f,.47f,1,QueryTriggerInteraction.Ignore))throw new Exception("Slope dash ends embedded: "+safe);
  if(safe.z<1)throw new Exception("Dash cannot advance toward slope");
  Debug.Log("EX DASH VALIDATED flat="+flat+" wall="+stopped+" slope="+safe);
  UnityEngine.Object.DestroyImmediate(slope);UnityEngine.Object.DestroyImmediate(floor);
  ValidateRollerPunch.Run();
 }
}
