using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class HeavenHairMotion
{
 public static void Bake(GameObject model,AnimationClip clip)
 {
  if(!clip.name.Contains("_bas_run_front"))return;
  var roots=model.GetComponentsInChildren<Transform>(true).Where(t=>(t.name.StartsWith("bdio01t0 hair",StringComparison.Ordinal)||t.name.EndsWith(" l hair 01")||t.name.EndsWith(" r hair 01"))&&t.parent&&t.parent.name.EndsWith(" face01",StringComparison.Ordinal)).ToArray();
  var neutral=roots.Select(t=>t.localRotation).ToArray();
  var curves=roots.Select(t=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()}).ToArray();
  // Rebuilding is idempotent: remove previously generated long-hair curves.
  foreach(var bone in roots)foreach(var axis in new[]{"x","y","z","w"})
   AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(bone,model.transform),typeof(Transform),"m_LocalRotation."+axis),null);
  int frames=Mathf.CeilToInt(clip.length*30);
  for(int frame=0;frame<=frames;frame++)
  {
   float time=Mathf.Min(clip.length,frame/30f);
   for(int i=0;i<roots.Length;i++)roots[i].localRotation=neutral[i];
   clip.SampleAnimation(model,time);
   for(int i=0;i<roots.Length;i++)
   {
    // Lift the trailing locks away from the forward-leaning back. The scalp
    // and front fringe keep their original pose and attachment points.
    bool trailing=roots[i].name.EndsWith(" l hair 01")||roots[i].name.EndsWith(" r hair 01");
    roots[i].rotation=Quaternion.AngleAxis(trailing?45f:0f,model.transform.right)*roots[i].rotation;
    Quaternion q=roots[i].localRotation;
    curves[i][0].AddKey(time,q.x);curves[i][1].AddKey(time,q.y);curves[i][2].AddKey(time,q.z);curves[i][3].AddKey(time,q.w);
   }
  }
  for(int i=0;i<roots.Length;i++)
  {
   string path=AnimationUtility.CalculateTransformPath(roots[i],model.transform);
   for(int axis=0;axis<4;axis++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[axis]),curves[i][axis]);
   roots[i].localRotation=neutral[i];
  }
  clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);
  Debug.Log("HEAVEN HAIR baked "+roots.Length+" roots / "+(frames+1)+" frames: "+clip.name);
 }
 public static void Build()
 {
  // Use the FBX rest rig rather than an already animated instance.
  foreach(var guid in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Generated/HeavenAnimations"}))
  {
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
   if(!clip.name.Contains("bdio01_bas_run_front"))continue;
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/bdio01.fbx"));
   Bake(model,clip);UnityEngine.Object.DestroyImmediate(model);
  }
  AssetDatabase.SaveAssets();
  if(!BuildPipeline.BuildAssetBundles("../dist/Assets",BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64))throw new Exception("Hair bundle build failed");
  InspectHeavenHair.Run();
 }
}
