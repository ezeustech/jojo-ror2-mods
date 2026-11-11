using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class InspectDioPose
{
    public static void Inspect()
    {
        var report = new List<string>();
        foreach(var path in new[] { "Assets/Models/3dio01.fbx", "Assets/Generated/3dio01Visual.prefab" }) {
        report.Add(path);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var root = Object.Instantiate(prefab);
        var animation = root.GetComponentInChildren<Animation>();
        foreach (string suffix in new[] { "REST", "_bas_nut", "_atk_l1" })
        {
            if (suffix != "REST")
                foreach (AnimationState state in animation)
                    if (state.name.EndsWith(suffix)) state.clip.SampleAnimation(animation.gameObject, .1f);
            report.Add(suffix);
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                renderer.BakeMesh(mesh);
                var points = mesh.vertices.Select(v => renderer.transform.TransformPoint(v)).ToArray();
                var actual = new Bounds(points[0], Vector3.zero);
                foreach(var p in points) actual.Encapsulate(p);
                report.Add(renderer.name + " enabled=" + renderer.enabled + " scale=" + renderer.transform.lossyScale + " bounds=" + renderer.bounds + " baked=" + mesh.bounds + " actual=" + actual);
                Object.DestroyImmediate(mesh);
            }
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t => t.parent == root.transform || t.name.EndsWith("bod1")))
                report.Add(t.name + " local=" + t.localPosition + " scale=" + t.localScale);
        }
        Object.DestroyImmediate(root);
        }
        File.WriteAllLines("../docs/pose-inspection.txt", report);
    }
}
