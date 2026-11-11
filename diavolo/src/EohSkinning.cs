using UnityEngine;

namespace Diavolo
{
    // XFBIN rigs use the same CPU-baked rendering path as the local DIO project.
    [DefaultExecutionOrder(100)]
    public sealed class EohSkinning : MonoBehaviour
    {
        private SkinnedMeshRenderer source;
        private Mesh baked;
        private MeshFilter filter;
        private GameObject aura;
        private Material auraMaterial;
        private Material outerAuraMaterial;
        public void SetAura(bool visible)
        {
            if (visible && !aura) {
                Shader shader = DiavoloPresentation.Asset<Shader>("Assets/BodyAura.shader"); if (!shader) return;
                aura = new GameObject("Faint Crimson aura"); aura.transform.SetParent(transform, false);
                aura.AddComponent<MeshFilter>().sharedMesh = baked;
                auraMaterial = new Material(shader);
                auraMaterial.SetFloat("_Width", .025f);
                aura.AddComponent<MeshRenderer>().sharedMaterial = auraMaterial;
                GameObject glow = new GameObject("Soft radiating edge"); glow.transform.SetParent(aura.transform,false);
                glow.AddComponent<MeshFilter>().sharedMesh = baked;
                outerAuraMaterial = new Material(shader); outerAuraMaterial.SetFloat("_Width", .075f);
                outerAuraMaterial.SetColor("_Color",new Color(.85f,.005f,.025f,.07f));
                glow.AddComponent<MeshRenderer>().sharedMaterial = outerAuraMaterial;
            }
            if (aura && aura.activeSelf != visible) aura.SetActive(visible);
        }
        private void Awake()
        {
            source = GetComponent<SkinnedMeshRenderer>();
            source.updateWhenOffscreen = true;
            baked = new Mesh { name = "Diavolo EOH animated surface" }; baked.MarkDynamic();
            GameObject surface = new GameObject("EOH surface"); surface.transform.SetParent(transform, false);
            filter = surface.AddComponent<MeshFilter>(); filter.sharedMesh = baked;
            surface.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
            source.enabled = false;
        }
        private void LateUpdate() { if (source && baked) { source.enabled = false; source.BakeMesh(baked); } }
        private void OnDestroy() { if (baked) Destroy(baked); if (auraMaterial) Destroy(auraMaterial); if (outerAuraMaterial) Destroy(outerAuraMaterial); }
    }
}
