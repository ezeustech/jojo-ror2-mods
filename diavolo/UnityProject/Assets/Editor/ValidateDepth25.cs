using UnityEngine;
using UnityEditor;
using System;
using System.IO;
public static class ValidateDepth25 {
 public static void Run(){
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/CrimsonModel.shader");if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Model shader compile failed");
  var material=new Material(shader);material.color=new Color(.5f,.1f,.2f);
  var shape=GameObject.CreatePrimitive(PrimitiveType.Cube);shape.GetComponent<Renderer>().sharedMaterial=material;
  var camera=new GameObject("Depth test").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-5);camera.transform.LookAt(Vector3.zero);camera.clearFlags=CameraClearFlags.SolidColor;camera.farClipPlane=100;camera.depthTextureMode=DepthTextureMode.Depth;camera.backgroundColor=Color.black;
  var target=new RenderTexture(128,128,24);camera.targetTexture=target;
  var image=new Texture2D(128,128,TextureFormat.RGB24,false);
  camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();Color black=image.GetPixel(64,64);
  camera.backgroundColor=Color.white;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();Color white=image.GetPixel(64,64);
  if(Mathf.Abs(black.r-white.r)+Mathf.Abs(black.g-white.g)+Mathf.Abs(black.b-white.b)>.01f)throw new Exception("Model blends with background");
  var depthMaterial=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/DepthAudit.shader"));var depthTarget=new RenderTexture(128,128,0);
  var command=new UnityEngine.Rendering.CommandBuffer();command.Blit(UnityEngine.Rendering.BuiltinRenderTextureType.CurrentActive,depthTarget,depthMaterial);camera.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardOpaque,command);camera.Render();camera.RemoveCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardOpaque,command);command.Release();
  RenderTexture.active=depthTarget;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();
  float center=image.GetPixel(64,64).r,corner=image.GetPixel(2,2).r;
  if(center>.2f || corner<.9f)throw new Exception("Model absent from depth texture: center="+center+" corner="+corner);
  File.WriteAllBytes("../docs/model-depth-2.5.1.png",image.EncodeToPNG());
  File.WriteAllText("../docs/depth-validation-2.5.1.txt","PASS: opaque model color unchanged against black/white backgrounds; model in camera depth texture (center="+center+", background="+corner+"). RoR2 postprocessing still needs live testing.\n");
  RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(shape);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(depthMaterial);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(depthTarget);UnityEngine.Object.DestroyImmediate(image);
  ValidateFate25.Run();
 }
}
