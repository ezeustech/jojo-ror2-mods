using UnityEngine;
using System.IO;
using System.Text;
public static class AuditGolem {
 public static void Run(){
  string folder=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("ROR2_GAME_DIR") ?? throw new System.InvalidOperationException("Set ROR2_GAME_DIR to your game installation."), "Risk of Rain 2_Data/StreamingAssets/aa/StandaloneWindows64");
  var output=new StringBuilder();
  foreach(string file in Directory.GetFiles(folder,"ror2-base-golem*_assets_all*.bundle")){
   if(file.Contains("golemplains"))continue;
   var bundle=AssetBundle.LoadFromFile(file);if(!bundle)continue;
   foreach(string name in bundle.GetAllAssetNames()){
    output.AppendLine("ASSET "+name);
    if(!name.ToLowerInvariant().Contains("golembody"))continue;
    var prefab=bundle.LoadAsset<GameObject>(name);if(!prefab)continue;
    output.AppendLine(name);
    foreach(var t in prefab.GetComponentsInChildren<Transform>(true))
     if(t.name.ToLowerInvariant().Contains("foot") || t.name.ToLowerInvariant().Contains("model"))output.AppendLine(t.name+" pos="+t.position+" local="+t.localPosition+" scale="+t.lossyScale);
    foreach(var c in prefab.GetComponentsInChildren<CapsuleCollider>(true))output.AppendLine("CAPSULE "+c.name+" height="+c.height+" radius="+c.radius+" center="+c.center+" trigger="+c.isTrigger+" scale="+c.transform.lossyScale+" position="+c.transform.position);
   }
  }
  File.WriteAllText("../docs/golem-collider-audit.txt",output.ToString());
 }
}
