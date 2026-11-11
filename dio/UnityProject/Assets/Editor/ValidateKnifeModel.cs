using System;
using System.IO;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateKnifeModel
{
 public static void Run()
 {
  var mesh=DioKnifeMesh.Create();
  if(mesh.vertexCount<100||mesh.bounds.size.z>.98f||mesh.bounds.size.z<.8f)throw new Exception("Invalid knife geometry");
  var obj=new GameObject("Knife");obj.AddComponent<MeshFilter>().sharedMesh=mesh;
  obj.AddComponent<MeshRenderer>().sharedMaterial=new Material(Shader.Find("Sprites/Default"));
  var camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.62f;camera.transform.position=new Vector3(.4f,1.5f,.35f);camera.transform.LookAt(mesh.bounds.center);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.09f,.12f);
  var rt=new RenderTexture(700,700,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var image=new Texture2D(700,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,700,700),0,0);image.Apply();File.WriteAllBytes("../docs/v356-knife.png",image.EncodeToPNG());RenderTexture.active=null;
  Debug.Log("KNIFE VALIDATED vertices="+mesh.vertexCount+" bounds="+mesh.bounds);
 }
}
