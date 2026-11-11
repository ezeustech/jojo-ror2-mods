using System;
using UnityEditor;
using UnityEngine;
public static class ValidateStandJump
{
    public static void Run()
    {
        var bundle = AssetBundle.LoadFromFile("../dist/Assets/eohdio");
        var prefab = bundle.LoadAsset<GameObject>("Assets/Generated/3wld01Visual.prefab");
        int kept = 0, total = 0;
        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = renderer.sharedMesh; var weights = mesh.boneWeights;
            bool IsLeg(int index)
            {
                for (var bone = renderer.bones[index]; bone && bone != renderer.rootBone; bone = bone.parent)
                    if (bone.name.IndexOf("thigh", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                return false;
            }
            bool Vertex(int index)
            {
                var w = weights[index];
                return (IsLeg(w.boneIndex0) ? w.weight0 : 0) + (IsLeg(w.boneIndex1) ? w.weight1 : 0)
                    + (IsLeg(w.boneIndex2) ? w.weight2 : 0) + (IsLeg(w.boneIndex3) ? w.weight3 : 0) >= .5f;
            }
            var triangles = mesh.triangles;
            total += triangles.Length / 3;
            for (int i = 0; i < triangles.Length; i += 3)
                if (Vertex(triangles[i]) && Vertex(triangles[i+1]) && Vertex(triangles[i+2])) kept++;
        }
        if (kept == 0 || kept >= total) throw new Exception("Invalid legs-only geometry");
        var dio = bundle.LoadAsset<GameObject>("Assets/Generated/3dio01Visual.prefab");
        int mapped = 0;
        foreach (var target in prefab.GetComponentsInChildren<Transform>(true))
            foreach (var source in dio.GetComponentsInChildren<Transform>(true))
                if (target.name.Contains("3wld01t0") && target.name.Replace("3wld01t0", "3dio01t0") == source.name) { mapped++; break; }
        if (mapped < 10) throw new Exception("Insufficient shared pose bones");
        var ui = AssetBundle.LoadFromFile("../dist/Assets/diofeedback");
        var icon = ui.LoadAsset<Texture2D>("Assets/UI/StandJump.png");
        if (!icon || icon.width != 256 || icon.height != 256) throw new Exception("Invalid Stand Jump icon");
        Debug.Log("STAND JUMP VALIDATED: legs triangles=" + kept + "/" + total + "; shared pose bones=" + mapped + "; bundled icon=256x256");
    }
}
