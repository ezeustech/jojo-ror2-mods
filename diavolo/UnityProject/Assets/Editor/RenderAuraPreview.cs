using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
public static class RenderAuraPreview
{
 public static void Run(){
  var root=new GameObject("Aura preview");
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/BodyAura.shader");if(ShaderUtil.ShaderHasError(shader))throw new System.Exception("Aura shader error");
  for(int i=0;i<2;i++){
   var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+(i==0?"5dvl01":"5kcr01")+"Visual.prefab"),root.transform);
   model.transform.position=new Vector3(i==0?-.7f:.7f,0,0);model.transform.localScale*=i==0?1.2f:1.05f;
   var animation=model.GetComponentInChildren<Animation>();foreach(AnimationState state in animation)if(state.name.EndsWith(i==0?"_bas_nut":"_bas_s_nut")){state.clip.SampleAnimation(animation.gameObject,.3f);break;}
   if(i==0)new Diavolo.EohHairPose(model).Apply(Vector3.back);
   foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
    var mesh=new Mesh();skin.BakeMesh(mesh);skin.enabled=false;
    var surface=new GameObject("Surface");surface.transform.SetParent(skin.transform,false);surface.AddComponent<MeshFilter>().sharedMesh=mesh;
    surface.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials.Select(m=>{var mat=new Material(Shader.Find("Unlit/Texture"));mat.mainTexture=m.mainTexture;return mat;}).ToArray();
    for(int shell=0;shell<2;shell++){
     var aura=new GameObject("Aura");aura.transform.SetParent(skin.transform,false);aura.AddComponent<MeshFilter>().sharedMesh=mesh;var mat=new Material(shader);mat.SetFloat("_Width",shell==0?.025f:.075f);if(shell==1)mat.SetColor("_Color",new Color(.85f,.005f,.025f,.07f));aura.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
   }
  }
  var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(3,1.6f,5);camera.transform.LookAt(new Vector3(0,1.2f,0));camera.orthographic=true;camera.orthographicSize=1.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.035f);
  var rt=new RenderTexture(1000,800,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1000,800);image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();File.WriteAllBytes("../docs/aura-preview.png",image.EncodeToPNG());
  Object.DestroyImmediate(root);Object.DestroyImmediate(camera.gameObject);RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
  ValidateSmashBundle.Run();
 }
}
