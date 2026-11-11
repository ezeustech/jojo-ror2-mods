using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PreviewDiavolo
{
    public static void Render()
    {
        string[] codes = { "5dvl01", "5kcr01" };
        foreach (string code in codes) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/" + code + "Visual.prefab");
            if (!prefab) throw new Exception("Missing " + code);
            GameObject model = UnityEngine.Object.Instantiate(prefab);
            Animation animation = model.GetComponentInChildren<Animation>();
            if (!animation) throw new Exception("Missing animation");
            foreach (AnimationState state in animation) if (state.name.EndsWith("_bas_nut")) { state.clip.SampleAnimation(animation.gameObject, 0); break; }
            foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                Mesh baked = new Mesh(); skin.BakeMesh(baked); baked.RecalculateBounds();
                GameObject surface = new GameObject("CPU surface"); surface.transform.SetParent(skin.transform, false);
                surface.AddComponent<MeshFilter>().sharedMesh = baked;
                surface.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                skin.enabled = false;
            }
            Bounds bounds = new Bounds(); bool first = true;
            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>()) {
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => { var unlit = new Material(Shader.Find("Unlit/Texture")); unlit.mainTexture = m.mainTexture; return unlit; }).ToArray();
            }
            GameObject camObject = new GameObject("Preview camera"); Camera camera = camObject.AddComponent<Camera>();
            camera.transform.position = bounds.center + new Vector3(.3f, .08f, 1f).normalized * bounds.size.y * 2.1f; camera.transform.LookAt(bounds.center);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .07f, .09f);
            camera.fieldOfView = 32f;
            GameObject lightObject = new GameObject("Preview light"); Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(35, -30, 0);
            RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
            RenderTexture target = new RenderTexture(640, 800, 24); camera.targetTexture = target; camera.Render();
            RenderTexture.active = target; Texture2D image = new Texture2D(640, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 640, 800), 0, 0); image.Apply();
            File.WriteAllBytes("../docs/" + code + "-preview.png", image.EncodeToPNG());
            if (code == "5kcr01") foreach (string suffix in new[] { "_atk_h1", "_spa_01_hit", "_spa_02_lp" }) {
                AnimationState selected = animation.Cast<AnimationState>().FirstOrDefault(s => s.name.EndsWith(suffix));
                if (selected == null) throw new Exception("Missing action " + suffix);
                selected.clip.SampleAnimation(animation.gameObject, selected.length * .45f);
                foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    MeshFilter surface = skin.GetComponentInChildren<MeshFilter>();
                    skin.BakeMesh(surface.sharedMesh); surface.sharedMesh.RecalculateBounds();
                }
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,640,800),0,0); image.Apply();
                File.WriteAllBytes("../docs/" + code + suffix + "-preview.png",image.EncodeToPNG());
            }
            RenderTexture.active = null; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(model); UnityEngine.Object.DestroyImmediate(camObject); UnityEngine.Object.DestroyImmediate(lightObject);
        }
    }
}
