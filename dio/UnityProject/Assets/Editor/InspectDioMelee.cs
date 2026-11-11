using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class InspectDioMelee
{
    public static void Inspect()
    {
        var rows = new List<string>();
        foreach (var code in new[] { "3dio01", "3wld01" })
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/" + code + "Visual.prefab"));
            var animation = root.GetComponentInChildren<Animation>();
            var bones = root.GetComponentsInChildren<Transform>();
            rows.Add(code + " BONES " + string.Join(",", bones.Select(b => b.name)));
            for (int step = 1; step <= 5; step++)
            {
                var clip = animation.Cast<AnimationState>().FirstOrDefault(s => s.name.EndsWith("_atk_l" + step))?.clip;
                if (!clip) continue;
                for (int sample = 0; sample <= 20; sample++)
                {
                    float t = sample / 20f;
                    clip.SampleAnimation(animation.gameObject, t * clip.length);
                    foreach (var bone in bones.Where(b => b.name.ToLowerInvariant().Contains("hand")))
                    {
                        var p = root.transform.InverseTransformPoint(bone.position);
                        rows.Add($"{code} step={step} t={t:F2} bone={bone.name} p={p.ToString("F3")}");
                    }
                }
            }
            Object.DestroyImmediate(root);
        }
        File.WriteAllLines("../docs/melee-animation-samples.txt", rows);
    }
}
