using System;
using System.IO;
using System.Linq;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace EyesOfHeavenDio
{
    internal static class DioAssets
    {
        internal static AssetBundle Bundle;
        internal static GameObject Dio, World, HeavenDio, HeavenWorld, ShadowDio, ShadowWorld;
        internal const float StandScale=1.25f;
        internal static void Load(Material gameMaterial)
        {
            if (Bundle) return;
            string directory = Path.GetDirectoryName(typeof(DioPlugin).Assembly.Location);
            Bundle = AssetBundle.LoadFromFile(Path.Combine(directory, "eohdio"));
            if (!Bundle) throw new InvalidOperationException("Missing eohdio asset bundle alongside EyesOfHeavenDio.dll.");
            Dio = Bundle.LoadAsset<GameObject>("Assets/Generated/3dio01Visual.prefab");
            World = Bundle.LoadAsset<GameObject>("Assets/Generated/3wld01Visual.prefab");
            HeavenDio = Bundle.LoadAsset<GameObject>("Assets/Generated/bdio01Visual.prefab");
            HeavenWorld = Bundle.LoadAsset<GameObject>("Assets/Generated/bwld01Visual.prefab");
            ShadowDio = Bundle.LoadAsset<GameObject>("Assets/Generated/3dio41Visual.prefab");
            ShadowWorld = Bundle.LoadAsset<GameObject>("Assets/Generated/shadowworldVisual.prefab");
            if (!Dio || !World || !HeavenDio || !HeavenWorld || !ShadowDio || !ShadowWorld) throw new InvalidOperationException("A DIO or The World skin prefab is missing from the bundle.");
            PrepareVisual(Dio, gameMaterial);
            PrepareVisual(World, gameMaterial);
            PrepareVisual(HeavenDio, gameMaterial);
            PrepareVisual(HeavenWorld, gameMaterial);
            PrepareVisual(ShadowDio, gameMaterial);PrepareVisual(ShadowWorld, gameMaterial);
            // Shared prefabs keep selection and in-run scales consistent.
            Dio.transform.localScale = Vector3.one * 1.15f;
            World.transform.localScale = Vector3.one * StandScale;
            HeavenDio.transform.localScale = Vector3.one * 1.15f;
            HeavenWorld.transform.localScale = Vector3.one * StandScale;
            ShadowDio.transform.localScale=Vector3.one*1.15f;ShadowWorld.transform.localScale=Vector3.one*StandScale;
            DioFeedback.LoadShaders();
            DioSpecialAssets.Load(gameMaterial);
        }
        private static void PrepareVisual(GameObject visual, Material gameMaterial)
        {
            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var skinning = renderer.gameObject.AddComponent<DioCpuSkinning>();
                skinning.source = renderer;
                skinning.surfaces = new MeshFilter[renderer.sharedMesh.subMeshCount];
                for (int section = 0; section < renderer.sharedMesh.subMeshCount; section++)
                {
                // SkinDef resolves renderers by hierarchy path; siblings must have
                // unique names or skin application aliases every section to #0.
                var surface = new GameObject("DioSurface" + section);
                surface.transform.SetParent(renderer.transform, false);
                skinning.surfaces[section] = surface.AddComponent<MeshFilter>();
                var target = surface.AddComponent<MeshRenderer>();
                var source = renderer.sharedMaterials[Math.Min(section, renderer.sharedMaterials.Length - 1)];
                    // Shadow World's original atlas contains the graduated leg fade.
                    // Keep its alpha-blended shader when creating CPU-skinned surfaces.
                    var material = new Material(visual == ShadowWorld ? source : gameMaterial);
                    material.name = source.name + "_RoR2";
                    material.mainTexture = source.mainTexture;
                    if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                    if (material.HasProperty("_EmColor")) material.SetColor("_EmColor", Color.black);
                    if (material.HasProperty("_NormalTex")) material.SetTexture("_NormalTex", null);
                    if (material.HasProperty("_EmTex")) material.SetTexture("_EmTex", null);
                    target.sharedMaterial = material;
                }
                renderer.enabled = false;
            }
        }
        internal static GameObject Attach(GameObject body)
        {
            var locator = body.GetComponent<ModelLocator>();
            Transform model = locator.modelTransform;
            var oldRenderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in oldRenderers) renderer.enabled = false;
            var characterModel = model.GetComponent<CharacterModel>();
            // DIO owns an empty display set: inventory still works, but no item
            // or equipment model is attached to the inherited Mercenary slots.
            var itemDisplays = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
            itemDisplays.name = "EOHDioHiddenItemDisplays";
            itemDisplays.keyAssetRuleGroups = Array.Empty<ItemDisplayRuleSet.KeyAssetRuleGroup>();
            characterModel.itemDisplayRuleSet = itemDisplays;
            var variants = CreateSkins(model.gameObject, false);
            var driver = body.AddComponent<DioVisualDriver>();
            driver.variants = variants;
            driver.dioRoot = variants.dios[0];
            driver.worldRoot = variants.worlds[0];
            driver.hiddenRenderers = oldRenderers;
            return driver.dioRoot;
        }

        internal static DioSkinVisuals CreateSkins(GameObject model, bool display)
        {
            var variants = model.AddComponent<DioSkinVisuals>();
            variants.groups = new GameObject[3]; variants.dios = new GameObject[3]; variants.worlds = new GameObject[3];
            var character = model.GetComponent<CharacterModel>() ?? model.AddComponent<CharacterModel>();
            var controller = model.GetComponent<ModelSkinController>() ?? model.AddComponent<ModelSkinController>();
            var infos = new System.Collections.Generic.List<CharacterModel.RendererInfo>();
            for (int i = 0; i < 3; i++)
            {
                var group = variants.groups[i] = new GameObject(i == 0 ? "DioDefault" : i == 1 ? "DioHeaven" : "DioShadow");
                group.transform.SetParent(model.transform, false);
                var dio = variants.dios[i] = Object.Instantiate(i == 0 ? Dio : i == 1 ? HeavenDio : ShadowDio, group.transform);
                var world = variants.worlds[i] = Object.Instantiate(i == 0 ? World : i == 1 ? HeavenWorld : ShadowWorld, group.transform);
                dio.name = "DioVisual"; world.name = "TheWorldVisual";
                if (display) { dio.AddComponent<DioDisplayAnimation>(); world.AddComponent<DioDisplayAnimation>().HoverStand = true; }
                foreach (var renderer in group.GetComponentsInChildren<MeshRenderer>(true))
                    infos.Add(new CharacterModel.RendererInfo { renderer = renderer, defaultMaterial = renderer.sharedMaterial,
                        defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On, ignoreOverlays = false });
            }
            character.baseRendererInfos = infos.ToArray();
            controller.skins = new SkinDef[3];
            for (int i = 0; i < 3; i++)
            {
                var skin = ScriptableObject.CreateInstance<SkinDef>();
                skin.name = i == 0 ? "EOHDioDefaultSkin" : i == 1 ? "EOHDioHeavenSkin" : "EOHDioShadowSkin";
                skin.nameToken = i == 0 ? "DIO" : i == 1 ? "Heaven Ascension DIO" : "Shadow DIO & First The World";
                skin.icon = i == 0 ? DioUi.Icon("DioCard") : i == 1 ? HeavenIcon() : ShadowIcon();
                skin.rootObject = model;
                skin.skinDefParams = ScriptableObject.CreateInstance<SkinDefParams>();
                skin.skinDefParams.name = skin.name + "Params";
                skin.skinDefParams.rendererInfos = character.baseRendererInfos;
                skin.skinDefParams.gameObjectActivations = new[] {
                    new SkinDefParams.GameObjectActivation { gameObject = variants.groups[0], shouldActivate = i == 0 },
                    new SkinDefParams.GameObjectActivation { gameObject = variants.groups[1], shouldActivate = i == 1 },
                    new SkinDefParams.GameObjectActivation { gameObject = variants.groups[2], shouldActivate = i == 2 }
                };
                controller.skins[i] = skin;
            }
            variants.groups[1].SetActive(false);variants.groups[2].SetActive(false);
            return variants;
        }
        private static Sprite shadowIcon;
        private static Sprite ShadowIcon()
        {
            if(shadowIcon)return shadowIcon;
            var texture=Bundle.LoadAsset<Texture2D>("Assets/ShadowTextures/medal_img_costume_3dio_d_2p_0.png");
            return shadowIcon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));
        }
        private static Sprite heavenIcon;
        private static Sprite HeavenIcon()
        {
            if (heavenIcon) return heavenIcon;
            var texture = new Texture2D(64,64,TextureFormat.RGBA32,false);
            for (int y=0;y<64;y++) for (int x=0;x<64;x++)
                texture.SetPixel(x,y, Mathf.Abs(x-32)+Mathf.Abs(y-32)<20 ? new Color(1f,.8f,.24f) : new Color(.88f,.9f,.96f));
            texture.Apply();
            return heavenIcon = Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
        }
    }

    public sealed class DioSkinVisuals : MonoBehaviour
    {
        public GameObject[] groups, dios, worlds;
        public int Selected { get { for(int i=groups.Length-1;i>0;i--)if(groups[i].activeSelf)return i;return 0; } }
    }

    // Preserve EOH's bone animation while bypassing the imported GPU skinning
    // transform mismatch. Each instance owns its dynamic mesh and releases it.
    [DefaultExecutionOrder(100)]
    public sealed class DioCpuSkinning : MonoBehaviour
    {
        public SkinnedMeshRenderer source;
        public MeshFilter[] surfaces;
        public bool legsOnly, armsOnly;
        private Mesh baked;
        private Mesh[] sections;
        private readonly System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<Vector3> normals = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<Vector4> tangents = new System.Collections.Generic.List<Vector4>();
        private void Start()
        {
            baked = new Mesh { name = "DioBakedPose" };
            baked.MarkDynamic();
            sections = new Mesh[surfaces.Length];
            for (int i = 0; i < sections.Length; i++)
            {
                // CharacterModel expects one base material per renderer. Preserve
                // every imported material section as a separate one-submesh mesh.
                var mesh = Instantiate(source.sharedMesh);
                mesh.name = "DioSection" + i;
                mesh.subMeshCount = 1;
                var triangles = source.sharedMesh.GetTriangles(i);
                if (legsOnly || armsOnly)
                {
                    var weights = source.sharedMesh.boneWeights;
                    var bones = source.bones;
                    bool LegBone(int index)
                    {
                        for (var bone = bones[index]; bone && bone != source.rootBone; bone = bone.parent)
                            if (bone.name.IndexOf(armsOnly ? "upperarm" : "thigh", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        return false;
                    }
                    bool LegVertex(int vertex)
                    {
                        var w = weights[vertex];
                        float weight = (LegBone(w.boneIndex0) ? w.weight0 : 0) + (LegBone(w.boneIndex1) ? w.weight1 : 0)
                            + (LegBone(w.boneIndex2) ? w.weight2 : 0) + (LegBone(w.boneIndex3) ? w.weight3 : 0);
                        return weight >= .5f;
                    }
                    var selected = new System.Collections.Generic.List<int>();
                    for (int t = 0; t < triangles.Length; t += 3)
                        if (LegVertex(triangles[t]) && LegVertex(triangles[t+1]) && LegVertex(triangles[t+2]))
                        { selected.Add(triangles[t]); selected.Add(triangles[t+1]); selected.Add(triangles[t+2]); }
                    triangles = selected.ToArray();
                }
                mesh.SetTriangles(triangles, 0);
                mesh.MarkDynamic();
                surfaces[i].sharedMesh = sections[i] = mesh;
            }
        }
        private void LateUpdate()
        {
            if (!source || !baked) return;
            source.enabled = false;
            source.BakeMesh(baked);
            baked.GetVertices(vertices);
            baked.GetNormals(normals);
            baked.GetTangents(tangents);
            foreach (var mesh in sections)
            {
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTangents(tangents);
                mesh.bounds = baked.bounds;
            }
        }
        private void OnDestroy()
        {
            if (baked) Destroy(baked);
            if (sections != null) foreach (var mesh in sections) if (mesh) Destroy(mesh);
        }
    }

    [DefaultExecutionOrder(-25)]
    public sealed class DioVisualDriver : MonoBehaviour
    {
        public DioSkinVisuals variants;
        public GameObject StandPrefab { get { RefreshSkin(); return variants.Selected == 2 ? DioAssets.ShadowWorld : variants.Selected == 1 ? DioAssets.HeavenWorld : DioAssets.World; } }
        public GameObject dioRoot, worldRoot;
        public bool SuppressStand;
        public Renderer[] hiddenRenderers;
        private Animation dioAnimation, worldAnimation;
        private DioPoseBlend actorBlend, standBlend;
        private CharacterBody body;
        private float actionUntil, worldUntil;
        private string current;
        private float pauseUntil;
        private bool guarding;
        private Vector3 guardPosition;
        private Quaternion guardRotation;
        private Transform guardSpace, guardLeft, guardRight;
        private float nextSummon;
        private bool barrageAiming;
        private Ray barrageRay;
        private Transform barrageSpace, barrageLeft, barrageRight;
        private Vector3 barragePosition;
        private Quaternion barrageRotation;
        private Transform normalStandSpace, normalStandLeft, normalStandRight;
        private Transform punchLeft, punchRight, punchPelvis;
        private Quaternion punchRestRotation;
        private bool punchAiming;
        private float punchAlignment;
        private float standReach=2f;
        private LocalStandCombat.StandPrimaryController standPrimary;
        private AnimationState independentClip;
        private float primaryPrevious,primaryAge,primaryDuration;
        private bool returnPose;
        private bool sustainedBarrage, barragePunching;
        public void SustainBarrage(Ray aim)
        {
            RefreshSkin();
            independentClip=null;actionUntil=0;worldUntil=Time.time+.25f;returnPose=false;
            BeginBarrageAim(aim);
            bool punching=!standPrimary || !standPrimary.Active || standPrimary.Arrived;
            if(!sustainedBarrage || punching!=barragePunching){
                if(DioPlugin.SmoothPoses.Value && standBlend!=null)standBlend.Begin(.14f);
                Play(worldAnimation,punching?"_spa_01_lp":"_bas_s_nut",0,true);
            }
            sustainedBarrage=true;barragePunching=punching;
        }
        public void StopBarrage()
        {
            sustainedBarrage=false;worldUntil=actionUntil=0;current=null;
            EndBarrageAim();returnPose=false;
            if(DioPlugin.SmoothPoses.Value && standBlend!=null)standBlend.Begin(.14f);
            Play(worldAnimation,"_bas_s_nut",0,true);
        }
        public void PrimaryTime(float previous,float age){primaryPrevious=previous;primaryAge=age;}
        public void AimPunch(Ray ray,float alignment){punchAiming=true;punchAlignment=alignment;BeginBarrageAim(ray);}
        public void EndPunch(){punchAiming=false;EndBarrageAim();independentClip=null;if(standPrimary && standPrimary.TargetedAction)worldUntil=0;}
        public void StandPunch(float duration,int step)
        {
            RefreshSkin();
            if(DioPlugin.SmoothPoses.Value && standBlend!=null)standBlend.Begin(Mathf.Min(.16f,duration*.22f));
            foreach(var bone in worldRoot.GetComponentsInChildren<Transform>(true))
                if(bone.name.EndsWith(" pelvis")){punchPelvis=bone;break;}
            var idle=FindClip(worldAnimation,"_bas_s_nut");
            if(idle!=null)worldAnimation[idle].clip.SampleAnimation(worldAnimation.gameObject,0);
            if(punchPelvis)punchRestRotation=Quaternion.Inverse(worldRoot.transform.rotation)*punchPelvis.rotation;
            Play(worldAnimation,"_atk_l"+(step+1),duration,false);
        }
        public void IndependentPrimary(float duration,int step)
        {
            RefreshSkin();
            RestoreActionSpeed();
            // Let the player's locomotion continue while the stand performs the chain.
            // Do not cancel its world anchor between combo hits.
            actionUntil=0;worldUntil=Time.time+duration;returnPose=false;
            if(!worldRoot.activeSelf)SummonFeedback(body?body.corePosition:transform.position);
            worldRoot.SetActive(true);
            int clipStep=step<2?step+2:step;
            StandPunch(duration,clipStep);
            string clip=FindClip(worldAnimation,"_atk_l"+(clipStep+1));
            independentClip=clip==null?null:worldAnimation[clip];
            if(independentClip!=null){independentClip.speed=0;independentClip.wrapMode=WrapMode.ClampForever;}
            primaryDuration=duration;primaryPrevious=primaryAge=0;
        }
        public void EndExPose(){actionUntil=0;current=null;}
        public void SetStandReach(float reach)=>standReach=reach;
        public void ExThrust(Ray ray,float duration)
        {
            Action("_bas_nut",duration,false);
            string neutral=FindClip(dioAnimation,"_bas_nut");
            if(neutral!=null)dioAnimation[neutral].speed=0;
            Play(worldAnimation,"_atk_l3",duration,false);
            worldUntil=Time.time+duration+.15f;
            if(!worldRoot.activeSelf)SummonFeedback(ray.origin+ray.direction*2f);
            worldRoot.SetActive(true);BeginBarrageAim(ray);
        }
        public void BeginBarrageAim(Ray ray)
        {
            RefreshSkin();
            if(!barrageAiming)
            {
                barragePosition=worldRoot.transform.localPosition;barrageRotation=worldRoot.transform.localRotation;
                barrageSpace=barrageLeft=barrageRight=punchLeft=punchRight=null;
                foreach(var skin in worldRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.name.EndsWith(" body")){barrageSpace=skin.transform;break;}
                foreach(var bone in worldRoot.GetComponentsInChildren<Transform>(true))
                {if(bone.name.EndsWith(" l upperarm"))barrageLeft=bone;if(bone.name.EndsWith(" r upperarm"))barrageRight=bone;if(bone.name.EndsWith(" l hand"))punchLeft=bone;if(bone.name.EndsWith(" r hand"))punchRight=bone;}
            }
            barrageAiming=true;barrageRay=ray;standReach=2f;
        }
        public void SetBarrageAim(Ray ray)=>barrageRay=ray;
        public void EndBarrageAim()
        {
            if(!barrageAiming)return;
            barrageAiming=false;
            if(worldRoot){worldRoot.transform.localPosition=barragePosition;worldRoot.transform.localRotation=barrageRotation;}
        }
        public void SummonFeedback(Vector3 position)
        {
            if(Time.time<nextSummon)return;
            nextSummon=Time.time+.6f;
            DioFeedback.Sound(gameObject,"stand-summon",.7f);
            DioStandParticles.Emit(position);
        }
        public void BeginGuard()
        {
            RefreshSkin();
            if (standPrimary) standPrimary.Cancel();
            if(guarding)return;
            guarding=true;guardPosition=worldRoot.transform.localPosition;guardRotation=worldRoot.transform.localRotation;
            guardSpace=guardLeft=guardRight=null;
            foreach(var skin in worldRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.name.EndsWith(" body")){guardSpace=skin.transform;break;}
            foreach(var bone in worldRoot.GetComponentsInChildren<Transform>(true))
            {if(bone.name.EndsWith(" l hand"))guardLeft=bone;if(bone.name.EndsWith(" r hand"))guardRight=bone;}
            RestoreActionSpeed();current=null;actionUntil=float.PositiveInfinity;
            Play(dioAnimation,"_grd_nut_lp",0,true);Play(worldAnimation,"_grd_s_nut_lp",0,true);
            worldRoot.SetActive(true);
            SummonFeedback(transform.position+transform.forward*1.3f+Vector3.up);
        }
        public void EndGuard()
        {
            if(!guarding)return;
            guarding=false;actionUntil=0;worldUntil=0;current=null;
            if(worldRoot){worldRoot.transform.localPosition=guardPosition;worldRoot.transform.localRotation=guardRotation;worldRoot.SetActive(false);}
        }
        private readonly System.Collections.Generic.Dictionary<AnimationState, float> pausedStates = new System.Collections.Generic.Dictionary<AnimationState, float>();
        public void PauseAction(float seconds)
        {
            RestoreActionSpeed();
            foreach (var animation in new[] { dioAnimation, worldAnimation })
                if (animation)
                    foreach (AnimationState state in animation)
                        if (state.enabled) { pausedStates[state] = state.speed; state.speed = 0; }
            pauseUntil = Time.time + seconds;
            actionUntil += seconds;
            worldUntil += seconds;
        }
        private void RestoreActionSpeed()
        {
            foreach (var entry in pausedStates) entry.Key.speed = entry.Value;
            pausedStates.Clear();
        }
        private void Start()
        {
            body = GetComponent<CharacterBody>();
            standPrimary = LocalStandCombat.StandPrimaryController.Of(gameObject);
            RefreshSkin();
            dioAnimation = dioRoot.GetComponentInChildren<Animation>(true);
            worldAnimation = worldRoot.GetComponentInChildren<Animation>(true);
            DioPlugin.Log.LogInfo("DIO visual instance ready: " + dioAnimation.GetClipCount() + " body clips, " + worldAnimation.GetClipCount() + " Stand clips.");
        }
        private void RefreshSkin()
        {
            if (!variants) return;
            int selected = variants.Selected;
            if (dioRoot == variants.dios[selected] && dioAnimation) return;
            EndBarrageAim();
            RestoreActionSpeed();
            dioRoot = variants.dios[selected]; worldRoot = variants.worlds[selected];
            dioAnimation = dioRoot.GetComponentInChildren<Animation>(true);
            worldAnimation = worldRoot.GetComponentInChildren<Animation>(true);
            normalStandSpace=normalStandLeft=normalStandRight=null;
            actorBlend=new DioPoseBlend(dioRoot);standBlend=new DioPoseBlend(worldRoot);
            foreach(var skin in worldRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if(skin.name.EndsWith(" body")){normalStandSpace=skin.transform;break;}
            foreach(var bone in worldRoot.GetComponentsInChildren<Transform>(true))
            {if(bone.name.EndsWith(" l upperarm"))normalStandLeft=bone;if(bone.name.EndsWith(" r upperarm"))normalStandRight=bone;}
            current = null;
        }
        private void Update() => RefreshSkin();
        private static string FindClip(Animation animation, string suffix)
        {
            if (!animation) return null;
            foreach (AnimationState state in animation)
                if (state.name.EndsWith(suffix, StringComparison.Ordinal)) return state.name;
            return null;
        }
        private void Play(Animation animation, string suffix, float duration, bool loop)
        {
            string clip = FindClip(animation, suffix);
            if (clip == null) return;
            var state = animation[clip];
            DioPoseBlend blend=animation==dioAnimation?actorBlend:standBlend;
            if(DioPlugin.SmoothPoses.Value && blend!=null)blend.Begin(duration>0?Mathf.Min(.16f,duration*.22f):.2f);
            state.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            state.speed = duration > 0 ? state.length / duration : 1;
            state.time = 0;
            if(DioPlugin.SmoothPoses.Value)animation.Play(clip);else animation.CrossFade(clip,.06f);
        }
        public void Action(string suffix, float duration, bool summon, bool loop = false)
        {
            RefreshSkin();
            independentClip=null;returnPose=false;
            if (standPrimary) standPrimary.Cancel();
            if (!dioAnimation) return;
            RestoreActionSpeed();
            actionUntil = Time.time + duration;
            current = null;
            Play(dioAnimation, suffix, loop ? 0 : duration, loop);
            if (summon)
            {
                if (!worldRoot.activeSelf) SummonFeedback(transform.position+Vector3.up);
                worldUntil = actionUntil + .2f;
                worldRoot.SetActive(true);
                Play(worldAnimation, suffix, loop ? 0 : duration, loop);
            }
        }
        private void LateUpdate()
        {
            if (!body || !dioAnimation) return;
            if(independentClip!=null){
                float alpha=Mathf.Clamp01((Time.time-Time.fixedTime)/Mathf.Max(.001f,Time.fixedDeltaTime));
                float phase=Mathf.Clamp01(Mathf.Lerp(primaryPrevious,primaryAge,alpha)/Mathf.Max(.01f,primaryDuration));
                independentClip.time=phase*independentClip.length;
                independentClip.clip.SampleAnimation(worldAnimation.gameObject,independentClip.time);
            }
            if(standPrimary && standPrimary.ReturningHome && !standPrimary.Continuous && !returnPose){returnPose=true;Play(worldAnimation,"_bas_s_nut",0,true);}
            if(DioPlugin.SmoothPoses.Value){if(actorBlend!=null)actorBlend.Apply();if(standBlend!=null)standBlend.Apply();}
            if (pausedStates.Count > 0 && Time.time >= pauseUntil) RestoreActionSpeed();
            foreach (var renderer in hiddenRenderers) if (renderer) renderer.enabled = false;
            if(guarding)
            {
                var guard=GetComponent<DioStandGuard>();
                var direction=guard ? guard.Facing : transform.forward;
                worldRoot.SetActive(!SuppressStand);
                worldRoot.transform.position=body.footPosition+direction*1.3f+Vector3.up*.1f;
                worldRoot.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
                if(guardSpace&&guardLeft&&guardRight)
                {
                    var hands=(DioPoseSolver.VisualPosition(guardSpace,guardLeft)+DioPoseSolver.VisualPosition(guardSpace,guardRight))*.5f;
                    worldRoot.transform.position+=body.corePosition+direction*1.6f+Vector3.up*.25f-hands;
                }
                return;
            }
            bool independent = standPrimary && standPrimary.Visible;
            worldRoot.SetActive(!SuppressStand && ((Time.time < worldUntil && !(standPrimary && standPrimary.TargetedAction)) || independent));
            if (independent && !SuppressStand)
            {
                Vector3 facing=Vector3.ProjectOnPlane(standPrimary.RenderDirection,Vector3.up);
                if(facing.sqrMagnitude<.001f)facing=body.characterDirection?body.characterDirection.forward:transform.forward;
                // Pin the animated shoulders to the stand's own world-space anchor.
                DioPoseSolver.AimStand(worldRoot.transform,normalStandSpace,normalStandLeft,normalStandRight,
                    standPrimary.RenderPosition,facing,0f);
                if (standPrimary.Active) DioPoseSolver.LimitStandTurn(worldRoot.transform,punchPelvis,punchRestRotation);
                DioPoseSolver.AimStand(worldRoot.transform,normalStandSpace,normalStandLeft,normalStandRight,
                    standPrimary.RenderPosition,facing,0f);
            }
            else if(punchAiming&&!SuppressStand)
            {
                DioPoseSolver.AimPunchBody(worldRoot.transform,barrageSpace,barrageLeft,barrageRight,barrageRay.origin+Vector3.up*.45f,barrageRay.direction);
                DioPoseSolver.LimitStandTurn(worldRoot.transform,punchPelvis,punchRestRotation);
                // Re-anchor after the torso correction, preserving full limb animation.
                DioPoseSolver.AimPunchBody(worldRoot.transform,barrageSpace,barrageLeft,barrageRight,barrageRay.origin+Vector3.up*.45f,barrageRay.direction);
            }
            else if(barrageAiming&&!SuppressStand)
                DioPoseSolver.AimStand(worldRoot.transform,barrageSpace,barrageLeft,barrageRight,barrageRay.origin,barrageRay.direction,standReach);
            else if(worldRoot.activeSelf && !SuppressStand)
                DioPoseSolver.AimStand(worldRoot.transform,normalStandSpace,normalStandLeft,normalStandRight,
                    body.corePosition+Vector3.up*.45f,body.inputBank?body.inputBank.aimDirection:transform.forward,2f);
            if (Time.time < actionUntil) return;
            string next = body.characterMotor && !body.characterMotor.isGrounded ? "_bas_jump_lp" :
                body.characterMotor && body.characterMotor.velocity.sqrMagnitude > 1 ? "_bas_run_front" : "_bas_nut";
            if (next != current) { current = next; Play(dioAnimation, next, 0, true); }
        }
        private void OnDisable()=>EndBarrageAim();
    }

    public sealed class DioDisplayAnimation : MonoBehaviour
    {
        public bool HoverStand;
        private Vector3 hoverOrigin;
        private bool ready;
        private void Start()
        {
            gameObject.AddComponent<DioVisualDiagnostics>();
            var animation = GetComponentInChildren<Animation>();
            if (!animation) return;
            foreach (AnimationState state in animation)
                if (state.name.EndsWith("_bas_nut", StringComparison.Ordinal) || state.name.EndsWith("_bas_s_nut", StringComparison.Ordinal))
                { state.wrapMode = WrapMode.Loop; state.speed = HoverStand ? .65f : 1f; animation.Play(state.name); animation.Sample(); break; }
            if (HoverStand)
            {
                // Normalize the two Stand rigs' different imported offsets before
                // placing the visible model behind DIO's shoulder in the lobby.
                Bounds bounds = default; bool hasPoint = false;
                foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh = new Mesh(); skin.BakeMesh(mesh);
                    foreach (var vertex in mesh.vertices)
                    {
                        var point = transform.parent.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                        if (!hasPoint) { bounds = new Bounds(point, Vector3.zero); hasPoint = true; }
                        else bounds.Encapsulate(point);
                    }
                    Destroy(mesh);
                }
                if (hasPoint) transform.localPosition += new Vector3(.7f, .55f + bounds.extents.y, -.8f) - bounds.center;
                hoverOrigin = transform.localPosition; ready = true;
            }
        }
        private void LateUpdate()
        {
            if (HoverStand && ready)
                transform.localPosition = hoverOrigin + Vector3.up * (.08f * Mathf.Sin(Time.unscaledTime * 1.35f));
        }
    }

    public sealed class DioVisualDiagnostics : MonoBehaviour
    {
        private System.Collections.IEnumerator Start()
        {
            yield return new WaitForSeconds(12);
            DioPlugin.Log.LogInfo("VISUAL " + name + " active=" + gameObject.activeInHierarchy + " position=" + transform.position + " scale=" + transform.lossyScale);
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = new Mesh();
                r.BakeMesh(mesh);
                var points = mesh.vertices.Select(v => r.transform.TransformPoint(v)).ToArray();
                Bounds bounds = points.Length > 0 ? new Bounds(points[0], Vector3.zero) : default;
                foreach (var point in points) bounds.Encapsulate(point);
                DioPlugin.Log.LogInfo("RENDER " + r.name + " active=" + r.gameObject.activeInHierarchy + " enabled=" + r.enabled + " layer=" + r.gameObject.layer + " bounds=" + r.bounds + " actual=" + bounds + " material=" + r.sharedMaterial?.name + " shader=" + r.sharedMaterial?.shader?.name + " supported=" + r.sharedMaterial?.shader?.isSupported);
                Destroy(mesh);
            }
            foreach (var camera in Camera.allCameras) DioPlugin.Log.LogInfo("CAMERA " + camera.name + " mask=" + camera.cullingMask);
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
                DioPlugin.Log.LogInfo("SURFACE " + renderer.transform.parent.name + "/" + renderer.name + " id=" + renderer.GetInstanceID() + " mesh=" + renderer.GetComponent<MeshFilter>().sharedMesh?.name + " materials=" + renderer.sharedMaterials.Length + " material=" + renderer.sharedMaterial?.name + " enabled=" + renderer.enabled + " bounds=" + renderer.bounds);
            var model = GetComponentInParent<CharacterModel>();
            if (model)
                foreach (var info in model.baseRendererInfos)
                    DioPlugin.Log.LogInfo("REGISTERED " + info.renderer?.GetInstanceID() + " material=" + info.defaultMaterial?.name);
        }
    }
}
