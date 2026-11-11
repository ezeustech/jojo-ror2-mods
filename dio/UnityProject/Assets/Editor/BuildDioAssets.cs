using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Asset-only build. Gameplay components are attached by the BepInEx plugin.
public static class BuildDioAssets
{
    [MenuItem("DIO/Build Asset Bundle")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var modelPaths = new[] { "3dio01", "3wld01", "bdio01", "bwld01" };
        Directory.CreateDirectory("Assets/Generated");
        var report = new List<string>();
        foreach (string code in modelPaths)
        {
            string path = "Assets/Models/" + code + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing converted model: " + path);
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.globalScale = .01f;
            importer.assetBundleName = "eohdio";
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var root = new GameObject(code + "Visual");
            var model = UnityEngine.Object.Instantiate(source, root.transform);
            model.name = code;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.Contains("nofacial") || renderer.name.Contains("damage"))
                {
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                    continue;
                }
                renderer.sharedMaterials = renderer.sharedMaterials.Select(original =>
                {
                    string materialName = original ? original.name : "body";
                    string textureName = "t_" + code + "_body01_d_#0_0";
                    if (materialName.EndsWith("_eye")) textureName = "t_" + code + "_eyelashes_0";
                    if (materialName.Contains("hair")) textureName = "t_" + code + "_hair01_d_#0_0";
                    // Heaven's arm01 texture is solid black. The arm surfaces use
                    // the body atlas for their visible skin in our standard shader.
                    var material = new Material(Shader.Find("Standard"));
                    material.name = code + "_" + materialName;
                    material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/" + textureName + ".png");
                    material.SetFloat("_Glossiness", .15f);
                    if (!material.mainTexture) material.color = new Color(.8f, .8f, .75f);
                    string materialPath = "Assets/Generated/" + material.name + ".mat";
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (existing) { EditorUtility.CopySerialized(material, existing); UnityEngine.Object.DestroyImmediate(material); material = existing; }
                    else AssetDatabase.CreateAsset(material, materialPath);
                    return material;
                }).ToArray();
            }
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (code == "bdio01" || code == "bwld01")
                clips = Retarget(code, code == "bdio01" ? "3dio01" : "3wld01", model);
            var idle = clips.FirstOrDefault(c => c.name.EndsWith("_bas_nut"));
            if (!idle && code == "bwld01") idle = clips.FirstOrDefault(c => c.name.EndsWith("_bas_s_nut"));
            if (idle) idle.SampleAnimation(model, 0);
            var renderers = model.GetComponentsInChildren<Renderer>();
            // Imported animation bounds include unrelated poses and can be orders of
            // magnitude larger than the skinned surface. Measure the actual vertices.
            Bounds bounds = default;
            bool hasPoint = false;
            foreach (var renderer in renderers.OfType<SkinnedMeshRenderer>())
            {
                var posedMesh = new Mesh();
                renderer.BakeMesh(posedMesh);
                foreach (var vertex in posedMesh.vertices)
                {
                    var point = renderer.transform.TransformPoint(vertex);
                    if (!hasPoint) { bounds = new Bounds(point, Vector3.zero); hasPoint = true; }
                    else bounds.Encapsulate(point);
                }
                renderer.localBounds = posedMesh.bounds;
                renderer.updateWhenOffscreen = true;
                UnityEngine.Object.DestroyImmediate(posedMesh);
            }
            if (!hasPoint || bounds.size.y < .00001f) throw new InvalidOperationException("Invalid posed bounds for " + code);
            float scale = 1.9f / bounds.size.y;
            model.transform.localScale *= scale;
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
            report.Add(code + ": height=" + bounds.size.y + ", scale=" + scale + ", renderers=" + renderers.Length + ", clips=" + clips.Length);
            foreach (var clip in clips) report.Add("  " + clip.name + " seconds=" + clip.length + " curves=" + AnimationUtility.GetCurveBindings(clip).Length);
            var animation = model.GetComponent<Animation>();
            if (!animation) animation = model.AddComponent<Animation>();
            foreach (var clip in clips) animation.AddClip(clip, clip.name);
            string prefabPath = "Assets/Generated/" + code + "Visual.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetImporter.GetAtPath(prefabPath).assetBundleName = "eohdio";
            UnityEngine.Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("../docs/unity-model-report.txt", report);
        Directory.CreateDirectory("../dist/Assets");
        var manifest = BuildPipeline.BuildAssetBundles("../dist/Assets", BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64);
        if (manifest == null) throw new InvalidOperationException("Asset bundle build failed; inspect the Editor log.");
        Debug.Log("DIO asset bundle built. Runtime wiring and in-game visual validation are still required.");
    }

    private static AnimationClip[] Retarget(string targetCode, string sourceCode, GameObject model)
    {
        Directory.CreateDirectory("Assets/Generated/HeavenAnimations");
        var result = new List<AnimationClip>();
        foreach (var original in AssetDatabase.LoadAllAssetsAtPath("Assets/Models/" + sourceCode + ".fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
        {
            var clip = new AnimationClip { name = original.name.Replace(sourceCode, targetCode), legacy = true, frameRate = original.frameRate, wrapMode = original.wrapMode };
            var bindings = new List<EditorCurveBinding>();
            var curves = new List<AnimationCurve>();
            foreach (var binding in AnimationUtility.GetCurveBindings(original))
            {
                var target = binding;
                target.path = binding.path.Replace(sourceCode, targetCode);
                // Extra outfit bones retain their imported rest transforms; only
                // matching skeleton paths receive the original moveset curves.
                if (target.path.Length > 0 && !model.transform.Find(target.path)) continue;
                bindings.Add(target); curves.Add(AnimationUtility.GetEditorCurve(original, binding));
            }
            AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());
            if(targetCode=="bdio01")HeavenHairMotion.Bake(model,clip);
            clip.EnsureQuaternionContinuity();
            string path = "Assets/Generated/HeavenAnimations/" + clip.name.Replace('|', '_').Replace(':', '_') + ".anim";
            var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (saved) { EditorUtility.CopySerialized(clip, saved); UnityEngine.Object.DestroyImmediate(clip); clip = saved; }
            else AssetDatabase.CreateAsset(clip, path);
            result.Add(clip);
        }
        if (result.Count == 0) throw new InvalidOperationException("Missing Heaven moveset animations");
        return result.ToArray();
    }
    public static void FixHeavenArms()
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/t_bdio01_body01_d_#0_0.png");
        if(!texture)throw new InvalidOperationException("Heaven body atlas missing");
        foreach(string side in new[]{"l","r"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/bdio01_bdio01bod1_"+side+"_arm.mat");
            if(!material)throw new InvalidOperationException("Heaven arm material missing");
            material.mainTexture=texture;material.color=Color.white;EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
        if(!BuildPipeline.BuildAssetBundles("../dist/Assets",BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Arm material bundle build failed");
        ValidateHeaven.Run();
    }
}
