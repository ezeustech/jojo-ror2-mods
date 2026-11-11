using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateV351
{
 public static void Run()
 {
  int cases=0;
  foreach(var code in new[]{"3wld01","bwld01"})
  {
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab"));root.transform.localScale=Vector3.one*1.15f;
   var animation=root.GetComponentInChildren<Animation>();var bones=root.GetComponentsInChildren<Transform>();
   Transform Find(string suffix)=>bones.First(t=>t.name.EndsWith(suffix));
   var space=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name.EndsWith(" body")).transform;
   foreach(int step in new[]{3,4,5})foreach(float phase in new[]{.13f,.23f,.37f,.47f})foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.up,Vector3.down})
   {
    var clip=animation.Cast<AnimationState>().First(s=>s.name.EndsWith("_atk_l"+step)).clip;clip.SampleAnimation(animation.gameObject,clip.length*phase);
    var origin=new Vector3(3,2,-4);
    DioPoseSolver.AimStand(root.transform,space,Find(" l upperarm"),Find(" r upperarm"),origin,dir,2);
    DioPoseSolver.AlignPunch(root.transform,space,Find(" l hand"),Find(" r hand"),origin,dir,1);
    float distance=Mathf.Min(Vector3.Distance(DioPoseSolver.VisualPosition(space,Find(" l hand")),origin+dir*3.5f),Vector3.Distance(DioPoseSolver.VisualPosition(space,Find(" r hand")),origin+dir*3.5f));
    if(distance>.001f)throw new Exception("M1 fist alignment failed "+code+" "+step);cases++;
   }
   UnityEngine.Object.DestroyImmediate(root);
  }
  foreach(var code in new[]{"3dio01","bdio01"})
  {
   var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab");
   var animation=root.GetComponentInChildren<Animation>();
   foreach(var suffix in new[]{"_bas_nut","_spa_01_lp"})if(!animation.Cast<AnimationState>().Any(s=>s.name.EndsWith(suffix)))throw new Exception("EX pose missing "+code+suffix);
  }
  Debug.Log("V351 VALIDATED "+cases+" M1 contacts; both DIO EX neutral and barrage poses exist");
 }
}
