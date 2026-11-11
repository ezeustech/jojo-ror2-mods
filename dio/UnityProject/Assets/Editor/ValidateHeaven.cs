using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateHeaven
{
 public static void Run()=>RunModels(false);
 public static void RunGuard()=>RunModels(true);
 private static void RunModels(bool guard)
 {
  var camera=new GameObject("Camera").AddComponent<Camera>();
  camera.orthographic=true;camera.orthographicSize=2.2f;camera.backgroundColor=new Color(.09f,.1f,.15f);camera.clearFlags=CameraClearFlags.SolidColor;
  RenderSettings.ambientLight=Color.gray;
  foreach(var p in new[]{new Vector3(2,4,-4),new Vector3(-3,2,1)}) {
   var light=new GameObject("Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.position=p;light.transform.LookAt(Vector3.up);
  }
  foreach(string code in guard ? new[]{"3dio01","3wld01","bdio01","bwld01"} : new[]{"bdio01","bwld01"})
  {
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab");
   if(!prefab)throw new Exception("Missing skin prefab");
   var root=UnityEngine.Object.Instantiate(prefab);root.transform.localScale=Vector3.one*1.15f;
   var animation=root.GetComponentInChildren<Animation>();
   if(!animation||animation.GetClipCount()<10)throw new Exception("Missing skin animation set");
   var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();
   foreach(var skin in skins)foreach(var mat in skin.sharedMaterials)if(!mat.mainTexture)throw new Exception("Missing skin texture: "+mat.name);
   var meshes=skins.Select(s=>new Mesh()).ToArray();
   for(int i=0;i<skins.Length;i++){
    var surface=new GameObject("Surface");surface.transform.SetParent(skins[i].transform,false);
    surface.AddComponent<MeshFilter>().sharedMesh=meshes[i];surface.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;
   }
   foreach(string suffix in guard ? new[]{code.Contains("dio")?"_grd_nut_lp":"_grd_s_nut_lp"} : code=="bdio01" ? new[]{"_bas_nut","_atk_l1","_spa_01_lp","_bas_jump_lp"} : new[]{"_bas_s_nut","_atk_l3","_spa_01_lp","_bas_s_dash_lp"})
   {
    var state=animation.Cast<AnimationState>().FirstOrDefault(s=>s.name.EndsWith(suffix));
    if(state==null)throw new Exception("Missing required animation: "+code+suffix);
    state.clip.SampleAnimation(animation.gameObject,state.length*.35f);
    var bounds=new Bounds();bool first=true;
    for(int i=0;i<skins.Length;i++){
     skins[i].BakeMesh(meshes[i]);
     foreach(var v in meshes[i].vertices){var point=skins[i].transform.TransformPoint(v);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
    }
    if(float.IsNaN(bounds.size.y)||bounds.size.y<.5f||bounds.size.magnitude>12)throw new Exception("Invalid posed geometry "+code+suffix+" "+bounds);
    camera.transform.position=bounds.center+new Vector3(-1.6f,.3f,5);camera.transform.LookAt(bounds.center);
    var rt=new RenderTexture(700,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
    var image=new Texture2D(700,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,700,900),0,0);image.Apply();
    File.WriteAllBytes("../docs/"+code+suffix+"-heaven.png",image.EncodeToPNG());
    camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    Debug.Log("HEAVEN VALIDATED "+code+suffix+" bounds="+bounds);
   }
   if(code=="bdio01"&&!guard) {
    var upper=new Transform[2];var lower=new Transform[2];var hands=new Transform[2];
    foreach(var bone in root.GetComponentsInChildren<Transform>())for(int i=0;i<2;i++) {
     string side=i==0?" l ":" r ";if(bone.name.EndsWith(side+"upperarm"))upper[i]=bone;
     if(bone.name.EndsWith(side+"forearm"))lower[i]=bone;if(bone.name.EndsWith(side+"hand"))hands[i]=bone;
    }
    var space=skins.First(s=>s.name.EndsWith(" body")).transform;
    DioPoseSolver.Handstand(root.transform,Quaternion.identity,upper,lower,hands,Vector3.zero,Vector3.right,Vector3.forward,space);
    foreach(var hand in hands)if(Mathf.Abs(DioPoseSolver.VisualPosition(space,hand).y-.08f)>.06f)throw new Exception("Heaven handstand hand alignment");
   }
   UnityEngine.Object.DestroyImmediate(root);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
  }
  Debug.Log("HEAVEN SKIN VALIDATION PASSED");
 }
}
