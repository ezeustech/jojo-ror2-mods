using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectHeavenHair
{
 public static void Run()
 {
  var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/bdio01Visual.prefab"));root.transform.localScale=Vector3.one*1.15f;
  var anim=root.GetComponentInChildren<Animation>();var clip=anim.Cast<AnimationState>().First(s=>s.name.EndsWith("_bas_run_front")).clip;clip.SampleAnimation(anim.gameObject,.2f);
  foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.Contains("hair"))Debug.Log("HAIR "+t.name+" parent="+t.parent.name+" rotation="+t.localEulerAngles);
  bool first=true;Bounds bounds=default;
  foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
   var mesh=new Mesh();skin.BakeMesh(mesh);var surface=new GameObject("Preview");surface.transform.SetParent(skin.transform,false);surface.AddComponent<MeshFilter>().sharedMesh=mesh;surface.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
   foreach(var v in mesh.vertices){var p=skin.transform.TransformPoint(v);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
  }
  var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.position=bounds.center+new Vector3(5,.2f,1);camera.transform.LookAt(bounds.center);camera.orthographic=true;camera.orthographicSize=1.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.1f,.15f);
  RenderSettings.ambientLight=Color.gray;var light=new GameObject("Light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-65,0);
  var rt=new RenderTexture(900,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();File.WriteAllBytes("../docs/heaven-hair-run.png",image.EncodeToPNG());
 }
}
