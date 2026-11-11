using UnityEngine;
using System;
using System.IO;
using Diavolo;
public static class ValidateFate25 {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static GameObject Box(Vector3 p,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.position=p;g.transform.localScale=size;return g;}
 public static void Run(){
  var floor=Box(new Vector3(0,-.5f,0),new Vector3(20,1,20));
  var wall=Box(new Vector3(3,4,0),new Vector3(.1f,8,8));Physics.SyncTransforms();
  Vector3 p;
  Check(FatePlacementGeometry.Ground(new Vector3(0,8,0),1.5f,24,1,out p)&&Mathf.Abs(p.y-.04f)<.01f,"Airborne ground target did not reach floor");
  Check(FatePlacementGeometry.Fits(p,1.63f,7.5f,1),"Stone Golem capsule does not fit open floor");
  Check(FatePlacementGeometry.Connected(p,new Vector3(-4,.04f,0),1.63f,7.5f,1),"Golem cannot move across open floor");
  Check(!FatePlacementGeometry.Connected(p,new Vector3(5,.04f,0),1.63f,7.5f,1),"Golem crossed wall");
  Check(!FatePlacementGeometry.Fits(new Vector3(9,.04f,0),1.63f,7.5f,1),"Golem footprint extends over cliff");
  Check(!FatePlacementGeometry.Ground(new Vector3(12,0,0),1.5f,2.5f,1,out p),"Off-map point accepted");
  UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(wall);
  var left=Box(new Vector3(-4,-.5f,0),new Vector3(5,1,8));var right=Box(new Vector3(4,-.5f,0),new Vector3(5,1,8));Physics.SyncTransforms();
  Check(!FatePlacementGeometry.Connected(new Vector3(-4,.04f,0),new Vector3(4,.04f,0),.5f,2,1),"Path crossed unsupported gap");
  UnityEngine.Object.DestroyImmediate(left);UnityEngine.Object.DestroyImmediate(right);
  File.WriteAllText("../docs/fate-validation-2.5.1.txt","PASS: "+checks+" floor, Stone Golem size, wall, cliff, off-map and unsupported-path checks. Live RoR2 placement still needs testing.\n");
 }
}
