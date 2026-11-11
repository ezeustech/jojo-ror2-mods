using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EyesOfHeavenDio;
public static class ValidateBarrageAim
{
 public static void Run()
 {
  int count=0;
  foreach(var code in new[]{"3wld01","bwld01"})
  {
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab"));root.transform.localScale=Vector3.one*1.15f;
   var animation=root.GetComponentInChildren<Animation>();var clip=animation.Cast<AnimationState>().First(s=>s.name.EndsWith("_spa_01_lp")).clip;
   var bones=root.GetComponentsInChildren<Transform>();var left=bones.First(t=>t.name.EndsWith(" l upperarm"));var right=bones.First(t=>t.name.EndsWith(" r upperarm"));
   var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name.EndsWith(" body"));
   foreach(var aim in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left,Vector3.up,Vector3.down,new Vector3(1,1,1).normalized,new Vector3(-1,-1,1).normalized})
   foreach(float reach in new[]{2f,5f})
   foreach(float phase in new[]{.05f,.25f,.5f})
   {
    clip.SampleAnimation(animation.gameObject,phase);
    Vector3 origin=new Vector3(4,2,-3);
    DioPoseSolver.AimStand(root.transform,skin.transform,left,right,origin,aim,reach);
    if(Vector3.Dot(root.transform.forward,aim)<.999f)throw new Exception("Stand body does not face crosshair");
    var shoulders=(DioPoseSolver.VisualPosition(skin.transform,left)+DioPoseSolver.VisualPosition(skin.transform,right))*.5f;
    if(Vector3.Distance(shoulders,origin+aim*reach)>.001f)throw new Exception("Stand shoulder anchor drift");
    var mesh=new Mesh();skin.BakeMesh(mesh);
    foreach(var vertex in mesh.vertices)if(Vector3.Distance(skin.transform.TransformPoint(vertex),shoulders)>5f)throw new Exception("Animated mesh detached from aim anchor");
    UnityEngine.Object.DestroyImmediate(mesh);count++;
   }
   UnityEngine.Object.DestroyImmediate(root);
  }
  Debug.Log("BARRAGE AIM VALIDATED "+count+" poses, both skins and full vertical aim");
 }
}
