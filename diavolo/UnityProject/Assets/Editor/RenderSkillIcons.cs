using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class RenderSkillIcons
{
 public static void Run(){
  string[] names={"CHAIN","CHOP","SMASH","IMPALE","SKIP","ERASE","EPITAPH","BLOOD","BARRAGE","COUNTER","GRAB","CANCEL","ULTIMATE"};
  string[] poses={"_atk_l2","_atk_h2","_asbr_groundsmash","_atk_h1","_bas_run_front","_bas_jojo_02_lp","_bas_nut","_spa_01_hit","_atk_l3","_grd_s_nut_lp","_atk_h1","_bas_jump_lp","_atk_h1"};
  float[] phases={.4f,.4f,.6f,.4f,.4f,.2f,.2f,.35f,.45f,.3f,.1f,.3f,.7f};
  Directory.CreateDirectory("Assets/Generated/Icons");
  for(int i=0;i<names.Length;i++){
   bool actor=i==4 || i==5 || i==6 || i==7 || i==11;
   var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+(actor?"5dvl01":"5kcr01")+"Visual.prefab"));
   var anim=model.GetComponentInChildren<Animation>();
   foreach(AnimationState state in anim)if(state.name.EndsWith(poses[i])){state.clip.SampleAnimation(anim.gameObject,state.length*phases[i]);break;}
   if(actor)new Diavolo.EohHairPose(model).Apply(Vector3.back);
   Bounds bounds=new Bounds();bool first=true;
   foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
    var go=new GameObject("Icon surface");go.transform.SetParent(skin.transform,false);var mesh=new Mesh();skin.BakeMesh(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
    var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=skin.sharedMaterials.Select(m=>{var mat=new Material(Shader.Find("Unlit/Texture"));mat.mainTexture=m.mainTexture;return mat;}).ToArray();skin.enabled=false;
    if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);
   }
   var camera=new GameObject("Icon camera").AddComponent<Camera>();Vector3 focus=bounds.center+Vector3.up*bounds.size.y*.1f;
   camera.transform.position=focus+new Vector3(.55f,.15f,4);camera.transform.LookAt(focus);camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.extents.y*.85f,bounds.extents.x)*1.15f;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.HSVToRGB((.94f+i*.018f)%1f,.75f,.13f);
   var rt=new RenderTexture(128,128,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(128,128);image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();
   string path="Assets/Generated/Icons/"+names[i]+".png";File.WriteAllBytes(path,image.EncodeToPNG());
   Object.DestroyImmediate(model);Object.DestroyImmediate(camera.gameObject);RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
  }
  AssetDatabase.Refresh();foreach(string path in Directory.GetFiles("Assets/Generated/Icons","*.png")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.assetBundleName="diavoloassets";importer.SaveAndReimport();
  }
 }
}
