using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using Diavolo;
public static class PreviewBarrage25 {
 public static void Run(){
  var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/5kcr01Visual.prefab"));root.transform.localScale=Vector3.one*1.05f;
  var animation=root.GetComponentInChildren<Animation>();animation.enabled=false;
  var bones=root.GetComponentsInChildren<Transform>();var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();
  var space=skins.First(s=>s.name.Contains(" body")).transform;
  var left=bones.First(t=>t.name.EndsWith(" l hand"));var right=bones.First(t=>t.name.EndsWith(" r hand"));
  var ls=bones.First(t=>t.name.EndsWith(" l upperarm"));var rs=bones.First(t=>t.name.EndsWith(" r upperarm"));var pelvis=bones.First(t=>t.name.EndsWith(" pelvis"));
  var idle=animation.Cast<AnimationState>().First(s=>s.name.EndsWith("_bas_s_nut")).clip;
  idle.SampleAnimation(animation.gameObject,0);Quaternion neutral=pelvis.localRotation;var torso=new BarrageTorsoPose(root);torso.Capture();
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Generated/DioBarrage.anim");
  var meshes=new Mesh[skins.Length];for(int i=0;i<skins.Length;i++){meshes[i]=new Mesh();var g=new GameObject("Baked preview");g.transform.SetParent(skins[i].transform,false);g.AddComponent<MeshFilter>().sharedMesh=meshes[i];g.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;}
  var camera=new GameObject("Barrage camera").AddComponent<Camera>();camera.transform.position=new Vector3(4,2,6);camera.transform.LookAt(new Vector3(0,1,1.5f));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.04f,.06f);camera.orthographic=true;camera.orthographicSize=2.3f;
  var target=new RenderTexture(384,384,24);camera.targetTexture=target;var sheet=new Texture2D(1536,384,TextureFormat.RGB24,false);
  float maxSide=0;
  for(int f=0;f<20;f++){
   float age=f*.035f;root.transform.SetPositionAndRotation(new Vector3(.35f,.15f,1.3f),Quaternion.identity);clip.SampleAnimation(animation.gameObject,Mathf.Repeat(age/.7f,1)*clip.length);pelvis.localRotation=neutral;torso.Apply();
   Vector3 core=Vector3.up,aim=Vector3.forward,side=Vector3.right;
   Vector3 center=(CrimsonPose.VisualPosition(space,ls)+CrimsonPose.VisualPosition(space,rs))*.5f;root.transform.position+=core+aim*1.5f-center;
   float beat=age/.12f;
   CrimsonPose.Arm(right,space,core+aim*Mathf.Lerp(1.7f,3.2f,(Mathf.Sin(beat*Mathf.PI)+1)*.5f)+side*.32f,side);
   CrimsonPose.Arm(left,space,core+aim*Mathf.Lerp(3.2f,1.7f,(Mathf.Sin(beat*Mathf.PI)+1)*.5f)-side*.32f,-side);
   foreach(var hand in new[]{left,right}){Vector3 p=CrimsonPose.VisualPosition(space,hand);if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z))throw new Exception("Barrage pose invalid");maxSide=Mathf.Max(maxSide,Mathf.Abs(p.x));}
   if(f%5==0){for(int i=0;i<skins.Length;i++){skins[i].BakeMesh(meshes[i]);meshes[i].RecalculateBounds();}camera.Render();camera.Render();RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,384,384),f/5*384,0);}
  }
  if(maxSide>1.5f)throw new Exception("Barrage hands still veer sideways: "+maxSide);
  sheet.Apply();File.WriteAllBytes("../docs/barrage-preview-2.5.1.png",sheet.EncodeToPNG());File.WriteAllText("../docs/barrage-validation-2.5.1.txt","PASS: 20 finite aim-aligned barrage poses, maximum lateral hand offset "+maxSide+" m.\n");
  RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(sheet);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
 }
}
