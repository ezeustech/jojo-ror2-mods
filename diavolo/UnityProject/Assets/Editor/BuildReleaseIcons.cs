using UnityEditor;
using UnityEngine;
public static class BuildReleaseIcons {
 public static void Run() {
  AssetDatabase.Refresh();
  var result=BuildPipeline.BuildAssetBundles("../dist/Assets",BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64);
  if(!result)throw new System.Exception("Icon bundle build failed");
  VerifyReleaseBundle.Run();
 }
}
