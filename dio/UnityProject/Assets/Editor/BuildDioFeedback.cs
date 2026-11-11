using UnityEditor;
using System.Collections.Generic;
public static class BuildDioFeedback
{
    public static void Build()
    {
        AssetDatabase.Refresh();
        foreach (var path in new[] { "Assets/Feedback/DioStopAtlas.png", "Assets/Feedback/DioResumeAtlas.png" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
            if (!texture || texture.width != 4800 || texture.height != 2160)
                throw new System.Exception("Unexpected overlay atlas size: " + path);
            UnityEngine.Debug.Log("DIO overlay atlas: " + path + " size=" + texture.width + "x" + texture.height + " format=" + texture.format);
        }
        var assets = new List<string> {
            "Assets/Feedback/DioRipple.shader", "Assets/Feedback/DioRing.shader",
            "Assets/Feedback/DioTimeOverlay.shader", "Assets/Feedback/DioStopAtlas.png",
            "Assets/Feedback/DioResumeAtlas.png" };
        foreach (var name in new[] { "DioPortrait", "DioCard", "Combo", "Knives", "TimeSkip", "TimeStop", "Barrage", "BloodDrain", "StandJump", "RoadRoller", "ExKnives", "StandBlock" })
        {
            string path = "Assets/UI/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path)) throw new System.Exception("Missing UI texture: " + path);
            assets.Add(path);
        }
        var manifest = BuildPipeline.BuildAssetBundles("../dist/Assets", new[] {
            new AssetBundleBuild { assetBundleName = "diofeedback", assetNames = assets.ToArray() }
        }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        if (!manifest) throw new System.Exception("Feedback bundle build failed.");
    }
}
