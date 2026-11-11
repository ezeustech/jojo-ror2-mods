using UnityEngine;
using System.IO;
using System.Linq;
using System;
public static class ValidateFinal251 {
 public static void Run(){
  VerifyReleaseBundle.Run();
  var bundle=AssetBundle.LoadFromFile(Path.GetFullPath("../dist/Assets/diavoloassets"));
  foreach(string code in new[]{"5dvl01","5kcr01"}){
   var animation=bundle.LoadAsset<GameObject>("Assets/Generated/"+code+"Visual.prefab").GetComponentInChildren<Animation>();
   foreach(string suffix in new[]{"_spa_02_lp","_spa_02_ed",code=="5kcr01"?"_gmc_01_jump":"_bas_jump"})
    if(!animation.Cast<AnimationState>().Any(s=>s.name.EndsWith(suffix)))throw new Exception("Required move clip missing: "+code+suffix);
  }
  var shader=bundle.LoadAsset<Shader>("Assets/CrimsonModel.shader");var material=new Material(shader);
  if(material.FindPass("ShadowCaster")<0)throw new Exception("Exported model depth pass missing");
  UnityEngine.Object.DestroyImmediate(material);bundle.Unload(true);
  PreviewBarrage25.Run();
  File.WriteAllText("../docs/final-bundle-2.5.1.txt","PASS: required impale/stand jump clips and model depth pass in final bundle; required assets and pose transition checks passed.\n");
 }
}
