using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
public static class RenderSelection
{
 public static void Run(){
  var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/5dvl01Visual.prefab"));
  var animation=model.GetComponentInChildren<Animation>();
  foreach(AnimationState s in animation)if(s.name.EndsWith("_bas_nut")){s.clip.SampleAnimation(animation.gameObject,.4f);break;}
  new Diavolo.EohHairPose(model).Apply(Vector3.back);
  foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
   var go=new GameObject("Portrait surface");go.transform.SetParent(skin.transform,false);var mesh=new Mesh();skin.BakeMesh(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
   go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials.Select(m=>{var mat=new Material(Shader.Find("Unlit/Texture"));mat.mainTexture=m.mainTexture;return mat;}).ToArray();skin.enabled=false;
  }
  var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.transform.position=new Vector3(2.5f,1.6f,1.8f);camera.transform.LookAt(new Vector3(0,1.45f,0));camera.orthographic=true;camera.orthographicSize=.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.015f,.045f);
  var rt=new RenderTexture(256,256,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(256,256);image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();
  File.WriteAllBytes("Assets/Generated/DiavoloPortrait.png",image.EncodeToPNG());AssetDatabase.Refresh();
  var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Generated/DiavoloPortrait.png");importer.textureType=TextureImporterType.Default;importer.assetBundleName="diavoloassets";importer.SaveAndReimport();
  Object.DestroyImmediate(model);Object.DestroyImmediate(camera.gameObject);RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
 }
}
