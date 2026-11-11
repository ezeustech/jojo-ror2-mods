using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateRollerPunch
{
 public static void Run()
 {
  foreach(var code in new[]{"3dio01","bdio01"})
  {
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab"));root.transform.localScale=Vector3.one*1.15f;
   var animation=root.GetComponentInChildren<Animation>();var idle=animation.Cast<AnimationState>().First(s=>s.name.EndsWith("_bas_nut")).clip;
   var bones=root.GetComponentsInChildren<Transform>();
   var punch=animation.Cast<AnimationState>().First(s=>s.name.EndsWith("_spa_01_lp")).clip;punch.SampleAnimation(animation.gameObject,.2f);
   var fists=bones.Where(t=>t.name.Contains(" finger")).ToDictionary(t=>t,t=>t.localRotation);
   Transform Find(string suffix)=>bones.First(t=>t.name.EndsWith(suffix));
   Transform[] Pair(string suffix)=>new[]{Find(" l "+suffix),Find(" r "+suffix)};
   var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();var space=skins.First(s=>s.name.EndsWith(" body")).transform;
   var feet=Pair("foot");var hands=Pair("hand");
   foreach(float phase in new[]{0f,.15f,.3f,.45f,.6f,1.2f,2.9f,6.29f,6.5f})
   {
    root.transform.position=Vector3.zero;idle.SampleAnimation(animation.gameObject,.1f);
    foreach(var finger in fists)finger.Key.localRotation=finger.Value;
    DioPoseSolver.RollerPunch(root.transform,Find(" pelvis"),Find(" spine"),Pair("thigh"),Pair("calf"),feet,Pair("upperarm"),Pair("forearm"),hands,Vector3.zero,Vector3.right,Vector3.forward,phase,space,phase>=6.29f);
    foreach(var foot in feet)if(Mathf.Abs(DioPoseSolver.VisualPosition(space,foot).y-.24f)>.08f)throw new Exception("Foot not planted "+code+" phase="+phase+" pos="+DioPoseSolver.VisualPosition(space,foot));
    foreach(var hand in hands)if(DioPoseSolver.VisualPosition(space,hand).y<-.02f)throw new Exception("Hand penetrates roller");
   }
   Bounds bounds=default;bool first=true;
   foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);var obj=new GameObject("Surface");obj.transform.SetParent(skin.transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
    foreach(var v in mesh.vertices){var p=skin.transform.TransformPoint(v);if(first){first=false;bounds=new Bounds(p,Vector3.zero);}else bounds.Encapsulate(p);}}
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.15f,0);floor.transform.localScale=new Vector3(3,.3f,3);floor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard")){color=new Color(.6f,.45f,.1f)};
   var camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.6f;camera.transform.position=bounds.center+new Vector3(3,1,4);camera.transform.LookAt(bounds.center);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.1f,.15f);
   RenderSettings.ambientLight=Color.gray;var light=new GameObject("Light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-35,0);
   var rt=new RenderTexture(900,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();File.WriteAllBytes("../docs/"+code+"-roller-crouch.png",image.EncodeToPNG());
   RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);
   Debug.Log("ROLLER CROUCH VALIDATED "+code+" bounds="+bounds);
  }
 }
}
