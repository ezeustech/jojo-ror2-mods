using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateV358
{
 public static void Run()
 {
  int count=0;
  foreach(var code in new[]{"3wld01","bwld01"})
  {
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab"));root.transform.localScale=Vector3.one*1.25f;
   var anim=root.GetComponentInChildren<Animation>();var bones=root.GetComponentsInChildren<Transform>();
   Transform Bone(string suffix)=>bones.First(t=>t.name.EndsWith(suffix));
   var pelvis=Bone(" pelvis");var left=Bone(" l upperarm");var right=Bone(" r upperarm");var space=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name.EndsWith(" body")).transform;
   anim.Cast<AnimationState>().First(s=>s.name.EndsWith("_bas_s_nut")).clip.SampleAnimation(anim.gameObject,0);
   var rest=Quaternion.Inverse(root.transform.rotation)*pelvis.rotation;
   foreach(int step in new[]{3,4,5})foreach(int frame in Enumerable.Range(0,61))foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.up,Vector3.down})
   {
    var clip=anim.Cast<AnimationState>().First(s=>s.name.EndsWith("_atk_l"+step)).clip;clip.SampleAnimation(anim.gameObject,clip.length*frame/60f);
    var origin=new Vector3(1,2.45f,-3);
    DioPoseSolver.AimPunchBody(root.transform,space,left,right,origin,dir);
    DioPoseSolver.LimitStandTurn(root.transform,pelvis,rest);
    DioPoseSolver.AimPunchBody(root.transform,space,left,right,origin,dir);
    if(Quaternion.Angle(root.transform.rotation*rest,pelvis.rotation)>30.1f)throw new Exception("Torso rotation escaped limit");
    var anchor=(DioPoseSolver.VisualPosition(space,left)+DioPoseSolver.VisualPosition(space,right))*.5f;
    if(Vector3.Distance(anchor,origin+dir*2f)>.002f)throw new Exception("Stand anchor drift");
    count++;
   }
   UnityEngine.Object.DestroyImmediate(root);
  }
  Debug.Log("V358 VALIDATED "+count+" full-body combo frames: torso limit and forward raised anchor");
 }
}
