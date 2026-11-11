using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
public static class Validate24 {
 public static void Run(){
  AssetDatabase.Refresh();foreach(string id in new[]{"BLOCK","JUMP"}){var ti=(TextureImporter)AssetImporter.GetAtPath("Assets/Generated/Icons/"+id+".png");ti.textureType=TextureImporterType.Sprite;ti.mipmapEnabled=false;ti.assetBundleName="diavoloassets";ti.SaveAndReimport();}
  BuildDiavolo.Build();
  Check();
 }
 public static void Check(){
  var bundle=AssetBundle.LoadFromFile(Path.GetFullPath("../dist/Assets/diavoloassets"));var clip=bundle.LoadAsset<AnimationClip>("Assets/Generated/DioBarrage.anim");if(!clip || clip.length<.1f)throw new Exception("Bundled barrage missing");
  var root=UnityEngine.Object.Instantiate(bundle.LoadAsset<GameObject>("Assets/Generated/5kcr01Visual.prefab"));var model=root.GetComponentInChildren<Animation>().gameObject;
  var space=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name.Contains(" body")).transform;
  var hand=model.GetComponentsInChildren<Transform>().First(t=>t.name.EndsWith(" r hand"));Vector3 previous=Vector3.zero;float movement=0;
  for(int frame=0;frame<20;frame++){clip.SampleAnimation(model,clip.length*frame/20);Vector3 p=space.position+(hand.position-space.position)*Mathf.Abs(space.lossyScale.x);if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z))throw new Exception("Invalid barrage pose");if(frame>0)movement+=Vector3.Distance(p,previous);previous=p;}
  if(movement<.1f)throw new Exception("Barrage did not animate King Crimson hand: "+movement+" model="+model.name+" clip="+clip.name);
  Preview(root,model,clip);
  File.WriteAllText("../docs/validation-2.4.txt","PASS: bundled retargeted barrage animates across 20 finite poses; cumulative hand travel "+movement+".\n");
  UnityEngine.Object.DestroyImmediate(root);bundle.Unload(true);ValidateErasedScene.Run();
 }

 static void Preview(GameObject root,GameObject model,AnimationClip clip){
  var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();var meshes=new Mesh[skins.Length];
  for(int i=0;i<skins.Length;i++){meshes[i]=new Mesh();var surface=new GameObject("Preview surface");surface.transform.SetParent(skins[i].transform,false);surface.AddComponent<MeshFilter>().sharedMesh=meshes[i];surface.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;}
  var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(3,2.2f,5);camera.transform.LookAt(new Vector3(0,1.1f,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.035f);camera.fieldOfView=38;
  var rt=new RenderTexture(384,384,24);camera.targetTexture=rt;var sheet=new Texture2D(1536,384,TextureFormat.RGB24,false);
  for(int frame=0;frame<4;frame++){clip.SampleAnimation(model,clip.length*frame/4);for(int i=0;i<skins.Length;i++)skins[i].BakeMesh(meshes[i]);camera.Render();RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,384,384),frame*384,0);}
  sheet.Apply();File.WriteAllBytes("../docs/barrage-preview-2.4.png",sheet.EncodeToPNG());RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sheet);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
 }
}
