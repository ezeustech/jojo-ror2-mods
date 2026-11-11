using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class RetargetDioBarrage {
 static void FileDiagnostic(AnimationClip c){System.IO.File.WriteAllLines("../docs/barrage-source-curves.txt",new[]{c.name}.Concat(AnimationUtility.GetCurveBindings(c).Take(30).Select(b=>b.path+" :: "+b.propertyName)));}
 public static void Run(){
  AssetDatabase.Refresh();string path="Assets/Models/DioBarrageSource.fbx";
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.globalScale=.01f;importer.SaveAndReimport();
  var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__") && c.name.EndsWith("_spa_01_lp"));
  FileDiagnostic(clip);
  var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
  var target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/5kcr01.fbx"));
  System.IO.File.WriteAllLines("../docs/barrage-bones.txt",source.GetComponentsInChildren<Transform>().Select(t=>"SOURCE "+t.name).Concat(target.GetComponentsInChildren<Transform>().Select(t=>"TARGET "+t.name)));
  var bones=new Dictionary<string,Transform>();foreach(var b in source.GetComponentsInChildren<Transform>())if(b.name.StartsWith("3wld01t0 "))bones[b.name.Substring(9)]=b;
  var rests=bones.ToDictionary(pair=>pair.Key,pair=>pair.Value.localRotation);var positions=bones.ToDictionary(pair=>pair.Key,pair=>pair.Value.localPosition);
  var output=new AnimationClip{legacy=true,frameRate=60,name="King Crimson - The World barrage"};int count=0;
  foreach(var dest in target.GetComponentsInChildren<Transform>()){
   if(!dest.name.StartsWith("5kcr01t0 "))continue;string key=dest.name.Substring(9);Transform from;if(!bones.TryGetValue(key,out from))continue;
   // Preserve King Crimson's rest proportions; transfer rotation and scaled translation deltas.
   Quaternion sourceRest=rests[key],targetRest=dest.localRotation;
   var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};Quaternion previous=Quaternion.identity;
   Vector3 basePosition=dest.localPosition;float ratio=positions[key].magnitude>.001f?Mathf.Clamp(basePosition.magnitude/positions[key].magnitude,.25f,4f):1f;
   int frames=Mathf.CeilToInt(clip.length*60);
   for(int i=0;i<=frames;i++){float time=Mathf.Min(clip.length,i/60f);clip.SampleAnimation(source,time);Quaternion q=targetRest*Quaternion.Inverse(sourceRest)*from.localRotation;if(i>0 && Quaternion.Dot(previous,q)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);previous=q;curves[0].AddKey(time,q.x);curves[1].AddKey(time,q.y);curves[2].AddKey(time,q.z);curves[3].AddKey(time,q.w);Vector3 pos=basePosition+(from.localPosition-positions[key])*ratio;curves[4].AddKey(time,pos.x);curves[5].AddKey(time,pos.y);curves[6].AddKey(time,pos.z);}
   string binding=AnimationUtility.CalculateTransformPath(dest,target.transform);for(int axis=0;axis<4;axis++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(binding,typeof(Transform),"m_LocalRotation."+"xyzw"[axis]),curves[axis]);for(int axis=0;axis<3;axis++)AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(binding,typeof(Transform),"m_LocalPosition."+"xyz"[axis]),curves[axis+4]);count++;
  }
  if(count<20)throw new Exception("Insufficient barrage retarget bones: "+count);
  output.EnsureQuaternionContinuity();const string result="Assets/Generated/DioBarrage.anim";
  var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(result);if(existing){EditorUtility.CopySerialized(output,existing);UnityEngine.Object.DestroyImmediate(output);}else AssetDatabase.CreateAsset(output,result);
  AssetImporter.GetAtPath(result).assetBundleName="diavoloassets";AssetDatabase.SaveAssets();
  System.IO.File.WriteAllText("../docs/barrage-retarget.txt","Retargeted The World _spa_01_lp: "+count+" matched bones; duration "+clip.length);
  UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);BuildDiavolo.Build();Validate24.Check();
 }
}
