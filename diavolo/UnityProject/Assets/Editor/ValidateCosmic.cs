using UnityEngine;
using UnityEditor;
using System.IO;
public static class ValidateCosmic
{
 public static void Run(){
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/CosmicVoid.shader");
  if(!shader || ShaderUtil.ShaderHasError(shader))throw new System.Exception("Cosmic shader compilation failed");
  var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.transform.localScale=Vector3.one*100;
  sphere.GetComponent<Renderer>().sharedMaterial=new Material(shader);
  var camera=new GameObject("Cosmic preview").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.fieldOfView=75;
  var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("../docs/cosmic-preview.png",image.EncodeToPNG());
  Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(sphere);RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
  RenderSelection.Run();ValidateSmashBundle.Run();
 }
}
