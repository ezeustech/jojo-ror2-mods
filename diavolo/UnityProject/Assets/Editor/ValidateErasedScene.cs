using UnityEngine;
using UnityEditor;
using System.IO;
public static class ValidateErasedScene
{
 public static void Run(){
  var root=new GameObject("Erased scene preview");
  Shader skyShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/CosmicVoid.shader"),groundShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/GroundHologram.shader"),modelShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/CrimsonModel.shader");
  foreach(var shader in new[]{skyShader,groundShader,modelShader})if(!shader || ShaderUtil.ShaderHasError(shader))throw new System.Exception("Scene shader compilation error");
  var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.transform.SetParent(root.transform);sphere.transform.localScale=Vector3.one*150;sphere.GetComponent<Renderer>().sharedMaterial=new Material(skyShader);
  var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.SetParent(root.transform);floor.transform.localScale=Vector3.one*4;floor.GetComponent<Renderer>().sharedMaterial=new Material(groundShader);
  var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/5dvl01Visual.prefab"),root.transform);model.transform.localScale*=1.2f;model.transform.rotation=Quaternion.Euler(0,-30,0);
  var anim=model.GetComponentInChildren<Animation>();foreach(AnimationState state in anim)if(state.name.EndsWith("_bas_nut")){state.clip.SampleAnimation(anim.gameObject,.3f);break;}
  new Diavolo.EohHairPose(model).Apply(-model.transform.forward);
  foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
   var mesh=new Mesh();skin.BakeMesh(mesh);var go=new GameObject("Body surface");go.transform.SetParent(skin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
   var materials=new Material[skin.sharedMaterials.Length];for(int j=0;j<materials.Length;j++){materials[j]=new Material(modelShader);materials[j].mainTexture=skin.sharedMaterials[j].mainTexture;materials[j].SetFloat("_ErasureContrast",0);}
   go.AddComponent<MeshRenderer>().sharedMaterials=materials;skin.enabled=false;
  }
  var camera=new GameObject("Scene camera").AddComponent<Camera>();camera.transform.position=new Vector3(3.5f,2.5f,6);camera.transform.LookAt(new Vector3(0,1,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.fieldOfView=55;
  var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;
  var ground=new Diavolo.GroundHologram(groundShader);var floorRenderer=floor.GetComponent<Renderer>();floorRenderer.forceRenderingOff=true;
  camera.Render();RenderTexture.active=rt;var baseline=new Texture2D(1280,720);baseline.ReadPixels(new Rect(0,0,1280,720),0,0);baseline.Apply();File.WriteAllBytes("../docs/erased-ground-baseline.png",baseline.EncodeToPNG());Object.DestroyImmediate(baseline);
  foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())Debug.Log("Preview surface "+renderer.name+" bounds="+renderer.bounds+" enabled="+renderer.enabled);
  ground.Draw(camera,new[]{new System.Collections.Generic.KeyValuePair<Renderer,bool>(floorRenderer,false)},new Terrain[0],Vector3.zero,1);
  camera.Render();ground.Detach();if(camera.GetCommandBuffers(UnityEngine.Rendering.CameraEvent.BeforeForwardAlpha).Length!=0)throw new System.Exception("Ground draw did not detach");ground.Dispose();RenderTexture.active=rt;var image=new Texture2D(1280,720);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("../docs/erased-ground-preview.png",image.EncodeToPNG());
  Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
  VerifyReleaseBundle.Run();
 }
}
