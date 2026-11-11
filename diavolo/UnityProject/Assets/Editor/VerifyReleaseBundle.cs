using UnityEngine;
using System.IO;
public static class VerifyReleaseBundle
{
 public static void Run(){
  var bundle=AssetBundle.LoadFromFile(Path.GetFullPath("../dist/Assets/diavoloassets"));if(!bundle)throw new System.Exception("Bundle missing");int checks=0;
  foreach(string name in new[]{"CHAIN","CHOP","SMASH","IMPALE","SKIP","ERASE","EPITAPH","BLOOD","BARRAGE","COUNTER","GRAB","CANCEL","ULTIMATE","BLOCK","JUMP"}){
   if(!bundle.LoadAsset<Sprite>("Assets/Generated/Icons/"+name+".png"))throw new System.Exception("Sprite missing "+name);checks++;
  }
  foreach(string shader in new[]{"CosmicVoid","BodyAura","CrimsonModel","GroundHologram"}){
   var value=bundle.LoadAsset<Shader>("Assets/"+shader+".shader");if(!value || !value.isSupported)throw new System.Exception("Unsupported shader "+shader);checks++;
  }
  if(!bundle.LoadAsset<Texture2D>("Assets/Generated/DiavoloPortrait.png"))throw new System.Exception("Portrait missing");checks++;
  foreach(string code in new[]{"5dvl01","5kcr01","5dvl11"}){if(!bundle.LoadAsset<GameObject>("Assets/Generated/"+code+"Visual.prefab"))throw new System.Exception("Model missing");checks++;}
  File.WriteAllText("../docs/release-bundle-validation.txt","PASS: "+checks+" bundled asset checks: 15 Sprite assets, 4 supported shaders, portrait texture and all three models.");bundle.Unload(true);
  ValidatePoseTransition.Run();
 }
}
