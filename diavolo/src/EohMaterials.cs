using System.Collections.Generic;
using UnityEngine;

namespace Diavolo
{
    public sealed class EohMaterials
    {
        private readonly List<Material> owned = new List<Material>();
        public void Apply(GameObject visual, Shader shader)
        {
            if (!shader || !shader.isSupported) throw new System.InvalidOperationException("Diavolo model shader is missing or unsupported.");
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true)) {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) {
                    Material source = materials[i];
                    Material instance = new Material(shader) { name = "Diavolo owned surface", mainTexture = source ? source.mainTexture : null };
                    instance.SetColor("_Color", Color.white);
                    owned.Add(instance); materials[i] = instance;
                }
                renderer.sharedMaterials = materials;
            }
        }
        public void LegsOnly(bool active,float height){foreach(var material in owned)if(material){material.SetFloat("_LegsOnly",active?1:0);material.SetFloat("_LegHeight",height);}}
        private bool contrast;
        public void SetErasureContrast(bool enabled) { if(contrast==enabled)return;contrast=enabled;foreach(Material material in owned)if(material)material.SetFloat("_ErasureContrast",enabled ? 1f : 0f); }
        public void Dispose(bool immediate = false)
        {
            // Never discover ownership by traversing renderers: some references
            // belong to the shared prefab, and CPU surfaces share our instances.
            foreach (Material material in owned) if (material) {
                if (immediate) Object.DestroyImmediate(material); else Object.Destroy(material);
            }
            owned.Clear();
        }
    }
}
