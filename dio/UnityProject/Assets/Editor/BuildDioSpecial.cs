using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class BuildDioSpecial {
 public static void Build() {
  AssetDatabase.Refresh();
  string folder="Assets/RoadRoller/";
  var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"rr.fbx");
  importer.isReadable=true; importer.importAnimation=false; importer.importCameras=false; importer.importLights=false; importer.SaveAndReimport();
  var normal=(TextureImporter)AssetImporter.GetAtPath(folder+"DefaultMaterial_normal.png"); normal.textureType=TextureImporterType.NormalMap; normal.SaveAndReimport();
  var mat=new Material(Shader.Find("Standard")); mat.name="DioRoadRoller";
  mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"DefaultMaterial_baseColor.png");
  mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"DefaultMaterial_normal.png")); mat.EnableKeyword("_NORMALMAP");
  mat.SetFloat("_Metallic",.35f); mat.SetFloat("_Glossiness",.25f);
  string materialPath=folder+"RoadRoller.mat";
  var old=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
  if(old) { EditorUtility.CopySerialized(mat,old); UnityEngine.Object.DestroyImmediate(mat); mat=old; } else AssetDatabase.CreateAsset(mat,materialPath);
  var root=new GameObject("DioRoadRoller"); var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"rr.fbx"),root.transform);
  var renderers=model.GetComponentsInChildren<Renderer>(); if(renderers.Length==0)throw new Exception("No roller geometry");
  var bounds=renderers[0].bounds;
  foreach(var r in renderers) { bounds.Encapsulate(r.bounds); r.sharedMaterial=mat; }
  float scale=6f/Mathf.Max(bounds.size.x,bounds.size.z);
  model.transform.localScale*=scale; model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
  PrefabUtility.SaveAsPrefabAsset(root,folder+"RoadRoller.prefab");
  Debug.Log("ROADROLLER VALIDATED original="+bounds.size+" normalized="+(bounds.size*scale)+" meshes="+renderers.Length);
  UnityEngine.Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
  var manifest=BuildPipeline.BuildAssetBundles("../dist/Assets",new[]{new AssetBundleBuild {assetBundleName="diospecial",assetNames=new[]{folder+"RoadRoller.prefab",folder+"DioGhost.shader"}}},BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64);
  if(!manifest)throw new Exception("Special bundle failed");
 }
}
