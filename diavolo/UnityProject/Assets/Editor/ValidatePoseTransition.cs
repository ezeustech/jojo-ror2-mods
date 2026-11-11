using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
public static class ValidatePoseTransition
{
 public static void Run(){
  int checks=0;
  foreach(string code in new[]{"5dvl01","5kcr01","5dvl11"}){
   var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab"));
   var anim=model.GetComponentInChildren<Animation>();var states=anim.Cast<AnimationState>().ToArray();
   AnimationClip idle=states.First(s=>s.name.EndsWith(code=="5kcr01" ? "_bas_s_nut" : "_bas_nut")).clip;
   AnimationClip attack=states.First(s=>s.name.EndsWith("_atk_h1")).clip;
   var blend=new Diavolo.PoseTransition(model);var bones=model.GetComponentsInChildren<Transform>(true);
   idle.SampleAnimation(anim.gameObject,.2f);blend.BeforeSample("idle",0,0,.1f);var start=bones.Select(b=>b.localRotation).ToArray();
   blend.BeforeSample("impale",1,1,.06f);attack.SampleAnimation(anim.gameObject,0);blend.AfterSample(1);
   for(int i=1;i<bones.Length;i++)if(Quaternion.Angle(bones[i].localRotation,start[i])>.1f)throw new System.Exception("Entrance still snaps: "+bones[i].name);checks++;
   attack.SampleAnimation(anim.gameObject,attack.length*.4f);var contact=bones.Select(b=>b.localRotation).ToArray();blend.AfterSample(1.18f);
   for(int i=1;i<bones.Length;i++)if(Quaternion.Angle(bones[i].localRotation,contact[i])>.1f)throw new System.Exception("Blend changed contact pose");checks++;
   blend.BeforeSample("idle",0,2,.1f);idle.SampleAnimation(anim.gameObject,.3f);blend.AfterSample(2.1f);
   foreach(var bone in bones)if(float.IsNaN(bone.localRotation.x) || float.IsNaN(bone.localPosition.x))throw new System.Exception("Invalid transform");checks++;
   Object.DestroyImmediate(model);
  }
  File.WriteAllText("../docs/pose-transition-validation.txt","PASS: "+checks+" checks across the three EOH rigs: entrance continuity, exact authored contact pose after blend, finite recovery transforms. Gameplay and visual feel remain unverified.");
 }
}
