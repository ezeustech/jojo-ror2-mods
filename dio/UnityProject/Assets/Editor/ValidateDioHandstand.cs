using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateDioHandstand {
 public static void Run() {
  var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/3dio01Visual.prefab"));
  root.transform.localScale=Vector3.one*1.15f;
  var animation=root.GetComponentInChildren<Animation>();
  foreach(AnimationState state in animation)if(state.name.EndsWith("_bas_nut")){state.clip.SampleAnimation(animation.gameObject,.1f);break;}
  var upper=new Transform[2];var lower=new Transform[2];var hands=new Transform[2];
  foreach(var bone in root.GetComponentsInChildren<Transform>(true))for(int i=0;i<2;i++){
   string side=i==0?" l ":" r ";
   if(bone.name.EndsWith(side+"upperarm"))upper[i]=bone;
   if(bone.name.EndsWith(side+"forearm"))lower[i]=bone;
   if(bone.name.EndsWith(side+"hand"))hands[i]=bone;
  }
  foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())if(r.name.EndsWith(" body"))Debug.Log("SCALE renderer="+r.transform.lossyScale+" pos="+r.transform.position+" bone="+upper[0].lossyScale+" upperpos="+upper[0].position+" root="+r.rootBone.position);
  var space=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.EndsWith(" body")).transform;
  DioPoseSolver.Handstand(root.transform,Quaternion.identity,upper,lower,hands,Vector3.zero,Vector3.right,Vector3.forward,space);
  for(int i=0;i<2;i++){
   Debug.Log("HANDSTAND hand="+hands[i].position+" upper="+upper[i].position+" lower="+lower[i].position);
   if(Mathf.Abs(DioPoseSolver.VisualPosition(space,hands[i]).y-.08f)>.06f)throw new Exception("Hand is not planted on contact plane");
  }
  bool have=false;Bounds bounds=new Bounds();
  foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
   var mesh=new Mesh();skin.BakeMesh(mesh);
   var surface=new GameObject("PreviewSurface");surface.transform.SetParent(skin.transform,false);
   surface.AddComponent<MeshFilter>().sharedMesh=mesh;surface.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
   skin.enabled=false;
   foreach(var v in mesh.vertices){var point=skin.transform.TransformPoint(v);if(!have){bounds=new Bounds(point,Vector3.zero);have=true;}else bounds.Encapsulate(point);}
  }
  var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.12f,0);floor.transform.localScale=new Vector3(4,.2f,3);
  floor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard")){color=new Color(.5f,.4f,.1f)};
  var camera=new GameObject("PreviewCamera").AddComponent<Camera>();camera.transform.position=bounds.center+new Vector3(3,1,-5);camera.transform.LookAt(bounds.center);
  camera.orthographic=true;camera.orthographicSize=2;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.15f);
  foreach(var offset in new[]{new Vector3(2,4,-3),new Vector3(-3,2,1)}){
   var light=new GameObject("PreviewLight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.position=offset;light.transform.LookAt(bounds.center);
  }
  RenderSettings.ambientLight=Color.gray;
  var rt=new RenderTexture(900,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();
  File.WriteAllBytes("../docs/handstand-refined.png",image.EncodeToPNG());
  if(bounds.min.y < -.2f)throw new Exception("Rendered model penetrates support plane");
  Debug.Log("HANDSTAND VALIDATED bounds="+bounds);
 }
}
