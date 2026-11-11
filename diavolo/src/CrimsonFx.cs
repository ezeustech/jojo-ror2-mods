using RoR2;
using UnityEngine;

namespace Diavolo
{
    public sealed class CrimsonFx : MonoBehaviour
    {
        private Material material;
        private float born, duration, size;
        private static Mesh quad;
        public static void Burst(Vector3 position, string texture, float size, float duration)
        {
            Shader shader = DiavoloPresentation.Asset<Shader>("Assets/CrimsonFX.shader");
            Texture image = DiavoloPresentation.Asset<Texture2D>("Assets/Textures/" + texture + ".png");
            if (!shader || !image) return;
            if (!quad) {
                quad = new Mesh { name = "Crimson effect quad" };
                quad.vertices = new Vector3[] { new Vector3(-.5f,-.5f), new Vector3(.5f,-.5f), new Vector3(.5f,.5f), new Vector3(-.5f,.5f) };
                quad.uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                quad.triangles = new int[] { 0,1,2,0,2,3 }; quad.RecalculateBounds();
            }
            GameObject effect = new GameObject("EOH Crimson effect"); effect.transform.position = position;
            effect.AddComponent<MeshFilter>().sharedMesh = quad;
            CrimsonFx driver = effect.AddComponent<CrimsonFx>();
            driver.material = new Material(shader); driver.material.mainTexture = image;
            effect.AddComponent<MeshRenderer>().sharedMaterial = driver.material;
            driver.born = Time.time; driver.duration = duration; driver.size = size;
        }
        private void LateUpdate()
        {
            float progress = (Time.time - born) / duration;
            if (progress >= 1f) { Destroy(gameObject); return; }
            if (Camera.main) transform.rotation = Camera.main.transform.rotation;
            transform.localScale = Vector3.one * size * Mathf.Lerp(.6f, 1.3f, progress);
            material.SetColor("_Color", new Color(1f, .08f, .18f, (1f-progress)*.8f));
        }
        private void OnDestroy() { if (material) Destroy(material); }
    }
}
