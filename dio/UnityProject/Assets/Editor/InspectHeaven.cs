using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectHeaven {
 public static void Run() {
  foreach(string code in new[]{"3dio01","bdio01","3wld01","bwld01"}) {
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/"+code+"Visual.prefab"));
   foreach(var t in root.GetComponentsInChildren<Transform>())
    if(t.name.EndsWith(" pelvis")||t.name.EndsWith(" l upperarm")||t.name.EndsWith(" body")||t.name==code)
     Debug.Log(code+" BONE "+t.name+" path="+AnimationUtility.CalculateTransformPath(t,root.transform.GetChild(0))+" pos="+t.localPosition.ToString("F5")+" rot="+t.localRotation.ToString("F5")+" scale="+t.lossyScale);
   var anim=root.GetComponentInChildren<Animation>();
   if(anim)foreach(AnimationState s in anim)if(s.name.EndsWith("_bas_nut")) {
    foreach(var b in AnimationUtility.GetCurveBindings(s.clip).Take(8))Debug.Log("CURVE "+b.path+" "+b.propertyName+" "+AnimationUtility.GetEditorCurve(s.clip,b).Evaluate(0));
   }
   Object.DestroyImmediate(root);
  }
 }
}
