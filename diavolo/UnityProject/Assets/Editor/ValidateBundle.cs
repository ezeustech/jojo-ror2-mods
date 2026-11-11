using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ValidateBundle
{
    public static void Run()
    {
        AssetBundle bundle = AssetBundle.LoadFromFile("../dist/Assets/diavoloassets");
        if (!bundle) throw new Exception("Bundle cannot be loaded");
        foreach (string code in new[] { "5dvl01", "5kcr01" }) {
            GameObject prefab = bundle.LoadAsset<GameObject>("Assets/Generated/"+code+"Visual.prefab");
            if (!prefab || prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0) throw new Exception("Missing model " + code);
            Animation animation = prefab.GetComponentInChildren<Animation>();
            if (!animation || animation.GetClipCount() < 50) throw new Exception("Missing clips " + code);
        }
        if (!bundle.LoadAsset<Shader>("Assets/CrimsonFX.shader")) throw new Exception("Missing FX shader");
        foreach (string name in new[] { "5dlv01_eff_crack00_0", "5dvl01_eff_timestep00_0", "5dvl01_eff_trail00_0", "5kcr01_eff_hitsimble01_0" })
            if (!bundle.LoadAsset<Texture2D>("Assets/Textures/"+name+".png")) throw new Exception("Missing FX texture " + name);
        File.WriteAllText("../docs/bundle-validation.txt", "PASS: loadable bundle; both models; animation clip counts >= 50; CrimsonFX shader; four runtime effect textures.\n");
        bundle.Unload(true);
    }
}
