using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class ValidateBoss
{
 public static void Run(){
  var bundle=AssetBundle.LoadFromFile(Path.GetFullPath("../dist/Assets/diavoloassets"));var prefab=bundle.LoadAsset<GameObject>("Assets/Generated/5dvl11Visual.prefab");if(!prefab)throw new System.Exception("Boss prefab missing");
  var model=Object.Instantiate(prefab);var anim=model.GetComponentInChildren<Animation>();var states=anim.Cast<AnimationState>().ToArray();if(states.Length<100)throw new System.Exception("Boss clips missing");
  var idle=states.First(s=>s.name.EndsWith("_bas_nut"));var attack=states.First(s=>s.name.EndsWith("_atk_h1"));var bones=model.GetComponentsInChildren<Transform>();
  idle.clip.SampleAnimation(anim.gameObject,.3f);var before=bones.Select(b=>b.position).ToArray();attack.clip.SampleAnimation(anim.gameObject,attack.length*.4f);float movement=0;for(int i=0;i<bones.Length;i++)movement+=Vector3.Distance(before[i],bones[i].position);if(movement<.1f)throw new System.Exception("Boss attack not bound");
  idle.clip.SampleAnimation(anim.gameObject,.3f);
  foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
   var mesh=new Mesh();skin.BakeMesh(mesh);var go=new GameObject("Boss surface");go.transform.SetParent(skin.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
  }
  var camera=new GameObject("Boss camera").AddComponent<Camera>();camera.transform.position=new Vector3(2.5f,1.5f,3);camera.transform.LookAt(new Vector3(0,1,0));camera.orthographic=true;camera.orthographicSize=1.2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.16f,.18f);
  var rt=new RenderTexture(600,800,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;var image=new Texture2D(600,800);image.ReadPixels(new Rect(0,0,600,800),0,0);image.Apply();File.WriteAllBytes("../docs/the-boss-preview.png",image.EncodeToPNG());
  bool red=false;foreach(var r in model.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m && m.mainTexture && m.mainTexture.name.Contains("body01_d_#1"))red=true;if(!red)throw new System.Exception("Boss is missing the red variant texture");
  File.WriteAllText("../docs/boss-validation.txt","PASS: bundled shadow suit with "+states.Length+" clips; impale sampled bone movement="+movement+". Source 5dvl11bod1 + red col1.");
  Object.DestroyImmediate(model);Object.DestroyImmediate(camera.gameObject);RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);bundle.Unload(true);
  ValidateErasedScene.Run();
 }
}
