using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuildDiavolo
{
    public static void Build()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Assets/Generated");
        var report = new List<string>();
        foreach (string code in new[] { "5dvl01", "5kcr01", "5dvl11" }) {
            string path = "Assets/Models/" + code + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true; importer.importCameras = false; importer.importLights = false;
            importer.isReadable = true; importer.globalScale = .01f; importer.SaveAndReimport();
            var root = new GameObject(code + "Visual");
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var idle = clips.FirstOrDefault(c => c.name.EndsWith("_bas_nut")) ?? clips.FirstOrDefault(c => c.name.EndsWith("_bas_s_nut"));
            if (idle) idle.SampleAnimation(model, 0);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(original => {
                    string name = original ? original.name : "body";
                    string texture = "t_" + code + (name.Contains("hair") ? "_hair01_d_#0_0" : name.Contains("iris") ? "_iris_0" : name.Contains("eyelash") ? "_eyelashes_0" : "_body01_d_#0_0");
                    if(code=="5dvl11" && texture.Contains("body01_d"))texture=texture.Replace("#0","#1");
                    var material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/CrimsonModel.shader"));
                    material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/" + texture + ".png");
                    string output = "Assets/Generated/" + code + "_" + name + ".mat";
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(output);
                    if (existing) { EditorUtility.CopySerialized(material, existing); UnityEngine.Object.DestroyImmediate(material); return existing; }
                    AssetDatabase.CreateAsset(material, output); return material;
                }).ToArray();
            }
            Bounds bounds = default(Bounds); bool hasPoint = false;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                var mesh = new Mesh(); skin.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices) { Vector3 point = skin.transform.TransformPoint(vertex); if (!hasPoint) { bounds = new Bounds(point, Vector3.zero); hasPoint = true; } else bounds.Encapsulate(point); }
                skin.localBounds = mesh.bounds; skin.updateWhenOffscreen = true;
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            if (!hasPoint || bounds.size.y < .0001f) throw new Exception("Invalid model bounds " + code);
            float scale = (code == "5kcr01" ? 2.25f : 1.9f) / bounds.size.y;
            model.transform.localScale *= scale;
            model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
            var animation = model.GetComponent<Animation>() ?? model.AddComponent<Animation>();
            foreach (var clip in clips) animation.AddClip(clip, clip.name);
            if (code == "5kcr01") {
                string asbrPath = "Assets/Models/5kcr01ASBR.fbx";
                var asbrImporter = (ModelImporter)AssetImporter.GetAtPath(asbrPath);
                asbrImporter.animationType = ModelImporterAnimationType.Legacy;
                asbrImporter.importAnimation = true; asbrImporter.globalScale = .01f; asbrImporter.SaveAndReimport();
                AnimationClip sourceClip = AssetDatabase.LoadAllAssetsAtPath(asbrPath).OfType<AnimationClip>().First(c => c.name.EndsWith("_atk_s_6h"));
                AnimationClip smash = new AnimationClip { legacy = true, frameRate = sourceClip.frameRate, name = "5kcr01_asbr_groundsmash" };
                // An armature-only FBX omits the outer mesh container. Restore
                // that path prefix so these curves address the existing EOH rig.
                foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip)) {
                    var mapped = binding;
                    mapped.path = "5kcr01bod1" + (binding.path.Length > 0 ? "/" + binding.path : "");
                    AnimationUtility.SetEditorCurve(smash, mapped, AnimationUtility.GetEditorCurve(sourceClip, binding));
                }
                string output = "Assets/Generated/ASBRGroundSmash.anim";
                // Legacy conversion had saved Unity's editor-only native clip class
                // (1108). Normalize its serialized class and main object id to the
                // runtime AnimationClip class (74) before loading or rebuilding it.
                if (File.Exists(output) && File.ReadAllText(output).Contains("PreviewAnimationClip:")) {
                    File.WriteAllText(output, File.ReadAllText(output).Replace("!u!1108 &110800000", "!u!74 &7400000").Replace("PreviewAnimationClip:", "AnimationClip:"));
                    string meta = output + ".meta";
                    if (File.Exists(meta)) File.WriteAllText(meta, File.ReadAllText(meta).Replace("mainObjectFileID: 110800000", "mainObjectFileID: 7400000"));
                    AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceUpdate);
                }
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
                if (existing && existing.GetType() != typeof(AnimationClip)) { AssetDatabase.DeleteAsset(output); existing = null; }
                if (existing) {
                    existing.ClearCurves(); existing.legacy = true; existing.frameRate = smash.frameRate; existing.name = smash.name;
                    foreach (var binding in AnimationUtility.GetCurveBindings(smash)) AnimationUtility.SetEditorCurve(existing, binding, AnimationUtility.GetEditorCurve(smash, binding));
                    EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(smash); smash = existing;
                }
                else AssetDatabase.CreateAsset(smash, output);
                animation.AddClip(smash, smash.name);
                int matched = AnimationUtility.GetCurveBindings(smash).Count(binding => binding.path == "" || model.transform.Find(binding.path));
                if (matched < 100) throw new Exception("ASBR 6H does not bind to EOH skeleton: " + matched);
                report.Add("ASBR 6H: " + smash.length + " seconds, " + matched + " matched curve bindings");
            }
            string prefab = "Assets/Generated/" + code + "Visual.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefab); AssetImporter.GetAtPath(prefab).assetBundleName = "diavoloassets";
            report.Add(code + " clips=" + clips.Length + " height=" + bounds.size.y + " scale=" + scale);
            foreach (var clip in clips) report.Add(clip.name + " " + clip.length);
            UnityEngine.Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        AssetImporter.GetAtPath("Assets/CrimsonFX.shader").assetBundleName = "diavoloassets";
        AssetImporter.GetAtPath("Assets/CrimsonModel.shader").assetBundleName = "diavoloassets";
        AssetImporter.GetAtPath("Assets/BodyAura.shader").assetBundleName = "diavoloassets";
        AssetImporter.GetAtPath("Assets/CosmicVoid.shader").assetBundleName = "diavoloassets";
        AssetImporter.GetAtPath("Assets/GroundHologram.shader").assetBundleName = "diavoloassets";
        AssetImporter.GetAtPath("Assets/Generated/ASBRGroundSmash.anim").assetBundleName = "diavoloassets";
        foreach (string texture in Directory.GetFiles("Assets/Textures", "*eff*.png")) AssetImporter.GetAtPath(texture.Replace('\\', '/')).assetBundleName = "diavoloassets";
        Directory.CreateDirectory("../dist/Assets");
        if (!BuildPipeline.BuildAssetBundles("../dist/Assets", BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64)) throw new Exception("Bundle build failed");
        File.WriteAllLines("../docs/unity-assets.txt", report);
    }
}
