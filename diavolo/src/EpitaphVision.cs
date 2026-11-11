using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace Diavolo
{
    // Cosmetic only: never instantiate enemy prefabs, AI, hurtboxes or network objects.
    public sealed class EpitaphVision : MonoBehaviour
    {
        public const float LookAhead = 4f;
        public const float Duration = 6f;
        private CharacterBody owner;
        private Material material;
        private float expires;
        private bool frozen;
        private readonly Dictionary<CharacterBody, Forecast> forecasts = new Dictionary<CharacterBody, Forecast>();
        private readonly List<CharacterBody> removed = new List<CharacterBody>();

        private bool IsLocalOwner(){if(!owner || owner.GetComponent<TimeEraseDecoy>())return false;foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList)if(user.cachedBody==owner)return true;return false;}
        public void Reveal()
        {
            OnDisable();
            frozen = false;
            owner = GetComponent<CharacterBody>();
            if(!IsLocalOwner()){enabled=false;return;}
            expires = Time.time + Duration;
            enabled = true;
            if (material) return;
            Shader shader = DiavoloPresentation.Asset<Shader>("Assets/CrimsonFX.shader");
            if (!shader) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (!shader) shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("UI/Default");
            if (!shader) {
                Debug.LogError("Diavolo: no supported transparent Epitaph shader found.");
                enabled = false;
                return;
            }
            material = new Material(shader) { name = "Epitaph red hologram", renderQueue = 3000 };
            Color red = new Color(1f, 0.03f, 0.03f, 0.22f);
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", red);
            if (material.HasProperty("_Color")) material.SetColor("_Color", red);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)CullMode.Off);
        }

        public void RevealFrozen(List<FateMark> marks, float duration)
        {
            Reveal();if(!enabled || !material)return; frozen = true; expires = Time.time + duration;
            foreach (FateMark mark in marks) {
                if (!mark.Body || !mark.Body.modelLocator || !mark.Body.modelLocator.modelTransform) continue;
                Forecast forecast = new Forecast(mark.Body, mark.Body.modelLocator.modelTransform);
                forecast.Freeze(mark);
                forecasts[mark.Body] = forecast;
            }
        }

        private bool IsEnemy(CharacterBody body)
        {
            return body && body != owner && body.healthComponent && body.healthComponent.alive
                && body.teamComponent && TeamMask.GetEnemyTeams(owner.teamComponent.teamIndex).HasTeam(body.teamComponent.teamIndex);
        }

        private void LateUpdate()
        {
            TemporalController time = owner ? owner.GetComponent<TemporalController>() : null;
            bool holdingContact = frozen && time && time.Erasing && time.HoldingImpale;
            if (!IsLocalOwner() || (!frozen && time && time.Erasing) || !owner || !owner.healthComponent || !owner.healthComponent.alive || !owner.teamComponent || (Time.time >= expires && !holdingContact)) {
                enabled = false;
                return;
            }
            // No range or enemy-count cap: include every living hostile body in this stage.
            if (!frozen) foreach (CharacterBody body in CharacterBody.readOnlyInstancesList) {
                if (!IsEnemy(body) || forecasts.ContainsKey(body)) continue;
                ModelLocator locator = body.modelLocator;
                if (!locator || !locator.modelTransform) continue;
                forecasts.Add(body, new Forecast(body, locator.modelTransform));
            }
            removed.Clear();
            foreach (KeyValuePair<CharacterBody, Forecast> pair in forecasts) {
                if (!IsEnemy(pair.Key)) { pair.Value.Dispose(); removed.Add(pair.Key); }
                else pair.Value.Draw(material);
            }
            foreach (CharacterBody body in removed) forecasts.Remove(body);
        }

        private void OnDisable()
        {
            foreach (Forecast forecast in forecasts.Values) forecast.Dispose();
            forecasts.Clear();
            removed.Clear();
            if (material) Destroy(material);
            material = null;
        }

        private sealed class Forecast
        {
            private readonly CharacterBody body;
            private readonly List<GhostMesh> meshes = new List<GhostMesh>();
            private Vector3 lastPosition;
            private Vector3 velocity;
            private float lastSample;
            private bool frozen;
            private Matrix4x4 fixedOffset;

            public void Freeze(FateMark mark)
            {
                frozen = true;
                fixedOffset = Matrix4x4.TRS(mark.Position, mark.Rotation, body.transform.lossyScale) * body.transform.worldToLocalMatrix;
                foreach (GhostMesh mesh in meshes) mesh.Freeze();
            }

            public Forecast(CharacterBody body, Transform model)
            {
                this.body = body;
                lastPosition = body.transform.position;
                lastSample = Time.time;
                // CharacterModel's base renderers avoid duplicating particles and item displays.
                CharacterModel characterModel = model.GetComponent<CharacterModel>();
                if (characterModel && characterModel.baseRendererInfos != null && characterModel.baseRendererInfos.Length > 0) {
                    foreach (CharacterModel.RendererInfo info in characterModel.baseRendererInfos) AddRenderer(info.renderer);
                } else {
                    foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) AddRenderer(renderer);
                }
            }

            private void AddRenderer(Renderer renderer)
            {
                if (!renderer) return;
                foreach (GhostMesh existing in meshes) if (existing.Source == renderer) return;
                SkinnedMeshRenderer skin = renderer as SkinnedMeshRenderer;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (skin && skin.sharedMesh) meshes.Add(new GhostMesh(skin));
                else if (renderer is MeshRenderer && filter && filter.sharedMesh) meshes.Add(new GhostMesh(renderer, filter.sharedMesh));
            }

            public void Draw(Material material)
            {
                if (frozen) { foreach (GhostMesh mesh in meshes) mesh.Draw(material, fixedOffset); return; }
                Vector3 position = body.transform.position;
                float elapsed = Time.time - lastSample;
                // Sampling the replicated transform also works for remote enemies without local motors.
                // Hold the estimate between physics/network samples, avoiding alternating
                // zero/speed spikes when several rendered frames share the same physics pose.
                if (elapsed >= 0.05f) {
                    Vector3 displacement = position - lastPosition;
                    // Teleports are a discontinuity, not a velocity to extrapolate across the map.
                    velocity = displacement.magnitude > Mathf.Max(10f, body.moveSpeed * elapsed * 4f)
                        ? Vector3.zero : displacement / elapsed;
                    lastPosition = position;
                    lastSample = Time.time;
                }
                Vector3 offset = velocity * LookAhead;
                RaycastHit hit;
                // Stop the forecast at static geometry rather than placing it inside a wall.
                if (offset.sqrMagnitude > 0.001f && Physics.Raycast(body.corePosition, offset.normalized, out hit, offset.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    offset = offset.normalized * Mathf.Max(0f, hit.distance - body.radius);
                Matrix4x4 translation = Matrix4x4.Translate(offset);
                foreach (GhostMesh mesh in meshes) mesh.Draw(material, translation);
            }

            public void Dispose() { foreach (GhostMesh mesh in meshes) mesh.Dispose(); }
        }

        private sealed class GhostMesh
        {
            public readonly Renderer Source;
            private readonly SkinnedMeshRenderer skin;
            private readonly Mesh mesh;
            private readonly bool ownsMesh;
            private bool frozen;
            private Matrix4x4 fixedPose;

            public void Freeze()
            {
                if (!Source) return;
                if (skin) { skin.BakeMesh(mesh); fixedPose = Source.localToWorldMatrix; }
                else fixedPose = Source.localToWorldMatrix;
                frozen = true;
            }

            public GhostMesh(SkinnedMeshRenderer source)
            {
                Source = source; skin = source;
                ownsMesh = true;
                mesh = new Mesh { name = "Epitaph pose" };
                mesh.MarkDynamic();
            }
            public GhostMesh(Renderer source, Mesh sharedMesh) { Source = source; mesh = sharedMesh; }

            public void Draw(Material material, Matrix4x4 translation)
            {
                if (!Source || !Source.gameObject.activeInHierarchy || !mesh) return;
                Matrix4x4 pose;
                if (frozen) pose = fixedPose;
                else if (skin) {
                    if (!skin.sharedMesh) return;
                    skin.BakeMesh(mesh);
                    // BakeMesh's default useScale=false leaves the renderer transform to us.
                    pose = Source.localToWorldMatrix;
                } else pose = Source.localToWorldMatrix;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    Graphics.DrawMesh(mesh, translation * pose, material, 0, null, submesh, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            }
            public void Dispose() { if (ownsMesh && mesh) Object.Destroy(mesh); }
        }
    }
}
