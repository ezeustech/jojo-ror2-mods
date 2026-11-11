using System;
using System.IO;
using RoR2;
using UnityEngine;

namespace Diavolo
{
    public sealed class DiavoloPresentation : MonoBehaviour
    {
        private static AssetBundle bundle;
        private CharacterBody body;
        private LocalStandCombat.StandPrimaryController standPrimary;
        private GameObject diavolo, stand;
        private Animation actorAnimation, standAnimation;
        private float attackUntil;
        private float supportUntil, auraUntil, nextItemHide;
        public void Support(float duration) { supportUntil = Time.time + duration; auraUntil = supportUntil; }
        private bool erased;
        private CharacterBody grabbed;
        private CharacterBody impaled;
        private CharacterBody ultimateTarget;
        private float ultimateStarted;
        public void UltimateLunge(CharacterBody target) { ultimateTarget = target; ultimateStarted = Time.time; }
        public void UltimateFinish() { ultimateTarget = null; }
        private float impaleHoldUntil;
        public void ImpaleContact(CharacterBody target) { impaled = target; impaleHoldUntil = Time.time + DiavoloAudio.Duration("impact_impale_sfx",1.15f); DiavoloAudio.Impact(body, "impact_impale"); }
        private float grabStarted;
        private Transform leftHand, rightHand, standSpace, leftShoulder, rightShoulder, pelvis;
        private Quaternion pelvisNeutral;private Vector3 standNormalScale;private float nextSummon;
        private BarrageTorsoPose barrageTorso;
        private Transform actorHand, actorSpace;
        private string attackMove;
        private string locomotionClip;
        private float locomotionStarted;
        public string CurrentMove { get { return Time.time < attackUntil ? attackMove : null; } }
        private float attackStarted;
        private bool independentPrimary,primaryAudioPending;
        private float primaryPrevious,primaryAge,primaryDuration;
        private float AttackAge {
            get {
                if(!independentPrimary)return Time.time-attackStarted;
                float alpha=Mathf.Clamp01((Time.time-Time.fixedTime)/Mathf.Max(.001f,Time.fixedDeltaTime));
                return Mathf.Lerp(primaryPrevious,primaryAge,alpha);
            }
        }
        private float AttackDuration { get { return independentPrimary?primaryDuration:attackUntil-attackStarted; } }
        public void PrimaryTime(float previous,float age,float duration){
            primaryPrevious=previous;primaryAge=age;primaryDuration=duration;
            attackUntil=Time.time+Mathf.Max(.06f,duration-age);
            if(primaryAudioPending && standPrimary && standPrimary.Arrived && age>=Mathf.Min(.1f,duration*.15f)){
                primaryAudioPending=false;if(!erased && body)DiavoloAudio.Play(body,attackMove);
            }
        }
        public void EndPrimary(){independentPrimary=primaryAudioPending=false;attackUntil=0;grabbed=null;}
        private EohHairPose hair;
        private PoseTransition actorTransition,standTransition;
        private Vector3 standOffset=new Vector3(.8f,.3f,-.8f),standOffsetFrom;
        private float standOffsetBegan,standOffsetSequence=-1;
        private EohSkinning[] actorSkins=new EohSkinning[0],standSkins=new EohSkinning[0];
        private Vector3 smashTarget;
        public void SetSmashTarget(Vector3 point) { smashTarget = point; }
        private readonly EohMaterials materials = new EohMaterials(), actorMaterials = new EohMaterials();
        private uint skinUsed;
        private float dodgeStarted = -10f;
        private readonly System.Collections.Generic.Dictionary<string, AnimationClip> clips = new System.Collections.Generic.Dictionary<string, AnimationClip>();
        public void Dodge() { dodgeStarted = Time.time; }
        public void SetGrabTarget(CharacterBody target) { grabbed = target; grabStarted = Time.time; }
        public static T Asset<T>(string name) where T : UnityEngine.Object
        {
            if (!bundle) { string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "diavoloassets"); if (File.Exists(path)) bundle = AssetBundle.LoadFromFile(path); }
            return bundle ? bundle.LoadAsset<T>(name) : null;
        }

        private void Start()
        {
            body = GetComponent<CharacterBody>();
            standPrimary = LocalStandCombat.StandPrimaryController.Of(gameObject);
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "diavoloassets");
            if (!bundle && File.Exists(path)) bundle = AssetBundle.LoadFromFile(path);
            if (!bundle) { Debug.LogWarning("Diavolo: missing diavoloassets bundle; using Mercenary placeholder."); return; }
            skinUsed=body.skinIndex;
            diavolo = Create(body.skinIndex==1 ? "5dvl11" : "5dvl01"); stand = Create("5kcr01");
            if (diavolo) actorAnimation = diavolo.GetComponentInChildren<Animation>();
            if(stand){standNormalScale=stand.transform.localScale;barrageTorso=new BarrageTorsoPose(stand);}
            if (stand) standAnimation = stand.GetComponentInChildren<Animation>();
            if (actorAnimation) actorAnimation.cullingType = AnimationCullingType.AlwaysAnimate;
            if (standAnimation) standAnimation.cullingType = AnimationCullingType.AlwaysAnimate;
            if (diavolo && skinUsed!=1) hair = new EohHairPose(diavolo);
            CacheClips(actorAnimation, "actor"); CacheClips(standAnimation, "stand");
            if(diavolo){actorTransition=new PoseTransition(diavolo);actorSkins=diavolo.GetComponentsInChildren<EohSkinning>(true);}
            if(stand){standTransition=new PoseTransition(stand);standSkins=stand.GetComponentsInChildren<EohSkinning>(true);}
            AnimationClip smash = Asset<AnimationClip>("Assets/Generated/ASBRGroundSmash.anim");
            if (smash) clips["stand_asbr_groundsmash"] = smash;
            else Debug.LogWarning("Diavolo: ASBR ground smash clip missing; using EOH heavy fallback.");
            AnimationClip barrage = Asset<AnimationClip>("Assets/Generated/DioBarrage.anim");
            if (barrage) clips["stand_dio_barrage"] = barrage;
            if (diavolo) {
                foreach (Transform bone in diavolo.GetComponentsInChildren<Transform>()) if (bone.name.EndsWith(" r hand")) actorHand = bone;
                foreach (SkinnedMeshRenderer skin in diavolo.GetComponentsInChildren<SkinnedMeshRenderer>()) if (skin.name.Contains(" body")) { actorSpace = skin.transform; break; }
            }
            if (stand) {
                foreach (Transform bone in stand.GetComponentsInChildren<Transform>()) {
                    if(bone.name.EndsWith(" l upperarm"))leftShoulder=bone;if(bone.name.EndsWith(" r upperarm"))rightShoulder=bone;if(bone.name.EndsWith(" pelvis"))pelvis=bone;
                    if (bone.name.EndsWith(" l hand")) leftHand = bone;
                    if (bone.name.EndsWith(" r hand")) rightHand = bone;
                }
                foreach (SkinnedMeshRenderer skin in stand.GetComponentsInChildren<SkinnedMeshRenderer>()) if (skin.name.Contains(" body")) { standSpace = skin.transform; break; }
            }
            if (diavolo && body.modelLocator && body.modelLocator.modelTransform) foreach (Renderer renderer in body.modelLocator.modelTransform.GetComponentsInChildren<Renderer>()) renderer.forceRenderingOff = true;
            SetErased(erased);
        }
        private GameObject Create(string code)
        {
            GameObject prefab = bundle.LoadAsset<GameObject>("Assets/Generated/" + code + "Visual.prefab");
            if (!prefab) return null;
            GameObject visual = Instantiate(prefab);
            visual.name = code + " EOH visual";
            visual.AddComponent<ErasureVisible>();
            visual.transform.localScale *= code == "5kcr01" ? 1.05f : 1.2f;
            (code=="5kcr01" ? materials : actorMaterials).Apply(visual, Asset<Shader>("Assets/CrimsonModel.shader"));
            foreach (SkinnedMeshRenderer skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.gameObject.AddComponent<EohSkinning>();
            return visual;
        }
        public void SetErased(bool value)
        {
            if (body && value != erased) CrimsonFx.Burst(body.corePosition, "5dlv01_eff_crack00_0", 7f, .65f);
            erased = value;
            if (value) Support(2f);
            RefreshVisibility();
        }
        private void RefreshVisibility()
        {
            bool visible = !erased;
            if (erased && body) {
                // Server authority also covers remote players; only a local player
                // actually controlling this body may see its erased visuals.
                foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
                    if (user.cachedBody == body) { visible = true; break; }
            }
            if (diavolo && diavolo.activeSelf != visible) diavolo.SetActive(visible);
            bool standVisible = visible && (erased || (Time.time < attackUntil && !(standPrimary && standPrimary.TargetedAction)) || Time.time < supportUntil || (standPrimary && standPrimary.Visible));
            if (stand && stand.activeSelf != standVisible){stand.SetActive(standVisible);if(standVisible && Time.time>=nextSummon){nextSummon=Time.time+.7f;DiavoloAudio.Impact(body,"summon");}}
        }
        public void Attack(string move, float duration, bool independent=false)
        {
            if (standPrimary && !independent) standPrimary.Cancel();
            independentPrimary=independent;primaryAudioPending=independent;
            primaryPrevious=primaryAge=0;primaryDuration=duration;grabbed=null;
            if (!independent && !erased && body) DiavoloAudio.Play(body, move);
            attackUntil = Time.time + duration;
            attackStarted = Time.time; attackMove = move;
        }
        public void HoldBlock(){independentPrimary=primaryAudioPending=false;if(standPrimary)standPrimary.Cancel();attackMove="block";attackUntil=Time.time+.2f;}
        public void StopAttack() { independentPrimary=primaryAudioPending=false;attackUntil = 0f; }
        public void SustainBarrage(bool first)
        {
            if(first){attackStarted=Time.time;attackMove="barrage";}
            independentPrimary=primaryAudioPending=false;grabbed=null;
            attackUntil=Time.time+.25f;
        }
        private void CacheClips(Animation animation, string prefix)
        {
            if (!animation) return;
            animation.Stop(); animation.enabled = false;
            foreach (AnimationState state in animation) {
                int start = state.name.LastIndexOf('|');
                string name = state.name.Substring(start + 1);
                int suffix = name.IndexOf('_');
                if (suffix >= 0) clips[prefix + name.Substring(suffix)] = state.clip;
            }
        }
        private void Sample(Animation animation, string prefix, string suffix, float phase, bool loop)
        {
            AnimationClip clip;
            if (!animation || !clips.TryGetValue(prefix + suffix, out clip)) return;
            clip.SampleAnimation(animation.gameObject, (loop ? Mathf.Repeat(phase, 1f) : Mathf.Clamp01(phase)) * clip.length);
        }
        private void SampleMoves()
        {
            bool attacking = Time.time < attackUntil;
            float age = AttackAge;
            float phase = Mathf.Clamp01(age / Mathf.Max(.01f, AttackDuration));
            string idle = body.characterMotor && !body.characterMotor.isGrounded ? "_bas_jump_lp" : body.inputBank && body.inputBank.moveVector.sqrMagnitude > .05f ? "_bas_run_front" : "_bas_nut";
            if(locomotionClip!=idle){locomotionClip=idle;locomotionStarted=Time.time;}
            Sample(actorAnimation, "actor", idle, (Time.time-locomotionStarted) / (idle == "_bas_run_front" ? .633333f : 3.366667f), true);
            Sample(standAnimation, "stand", "_bas_s_nut", Time.time / 2.7f, true);
            if(pelvis)pelvisNeutral=pelvis.localRotation;
            if(barrageTorso!=null)barrageTorso.Capture();
            if (!attacking) return;
            if(attackMove=="block"){Sample(actorAnimation,"actor","_grd_nut_lp",Time.time,true);Sample(standAnimation,"stand","_grd_s_nut_lp",Time.time,true);
            }else if(attackMove=="jump"){Sample(actorAnimation,"actor","_bas_jump",phase,false);Sample(standAnimation,"stand","_gmc_01_jump",phase,false);
            }else if (attackMove == "counter") {
                Sample(actorAnimation, "actor", "_bas_jojo_02_lp", age / 1.7f, true);
                Sample(standAnimation, "stand", "_grd_s_nut_lp", age, true);
            } else if (attackMove == "ultimate") {
                Sample(actorAnimation, "actor", "_bas_jojo_02_lp", age / 1.7f, true);
                Sample(standAnimation, "stand", "_atk_h1", Mathf.Min(.4f, age / .35f * .4f), false);
            } else if (attackMove == "charge") {
                Sample(actorAnimation,"actor","_spa_02_lp",age/.767f,true);
                Sample(standAnimation,"stand","_spa_02_lp",age/.767f,true);
            } else if (attackMove == "impale") {
                float contact = .18f / Mathf.Max(.1f, body.attackSpeed);
                float hold=DiavoloAudio.Duration("impact_impale_sfx",1.15f);
                float frame=age<contact?Mathf.Lerp(0,.45f,age/contact):age<contact+hold?.45f:Mathf.Lerp(.45f,1,(age-contact-hold)/.32f);
                Sample(actorAnimation,"actor","_spa_02_ed",frame,false);
                Sample(standAnimation,"stand","_spa_02_ed",frame,false);
            } else if (attackMove == "barrage") {
                if(standPrimary && standPrimary.Visible && !standPrimary.Arrived)return;
                float punch = age / (.12f / LocalStandCombat.StandPrimaryRules.AnimationSpeed(body.attackSpeed));
                string clip = ((int)punch % 2 == 0) ? "_atk_l2" : "_atk_l3";
                if(!(standPrimary && standPrimary.Continuous))Sample(actorAnimation, "actor", "_atk_h1", .25f, false);
                if(clips.ContainsKey("stand_dio_barrage")) Sample(standAnimation,"stand","_dio_barrage",age/.7f,true);
                else Sample(standAnimation, "stand", clip, Mathf.Lerp(.15f,.7f,Mathf.Repeat(punch,1f)), false);
            } else if (attackMove.StartsWith("chain", StringComparison.Ordinal)) {
                string clip = attackMove == "chain1" ? "_atk_l2" : attackMove == "chain2" ? "_atk_l3" : "_atk_h1";
                float frame = phase < .35f ? Mathf.Lerp(.1f,.4f,phase/.35f) : Mathf.Lerp(.4f,1f,(phase-.35f)/.65f);
                if(!(standPrimary && standPrimary.TargetedAction))Sample(actorAnimation, "actor", clip, frame, false);
                Sample(standAnimation, "stand", clip, frame, false);
            } else if (attackMove == "groundsmash") {
                if(!(standPrimary && standPrimary.TargetedAction))Sample(actorAnimation, "actor", "_atk_h2", phase, false);
                Sample(standAnimation, "stand", clips.ContainsKey("stand_asbr_groundsmash") ? "_asbr_groundsmash" : "_atk_h2", phase, false);
            } else if (attackMove == "blood") {
                Sample(actorAnimation, "actor", "_spa_01_hit", Mathf.Lerp(.05f,.55f,phase), false);
            } else if (attackMove == "grab") {
                if(!(standPrimary && standPrimary.TargetedAction))Sample(actorAnimation, "actor", "_atk_h1", phase, false);
                float contact=independentPrimary?.85f/1.3f:.85f/1.2f;
                float frame=phase<=contact?Mathf.Lerp(0,.4f,phase/contact):Mathf.Lerp(.4f,1,(phase-contact)/(1-contact));
                Sample(standAnimation, "stand", "_atk_h1", frame, false);
            } else {
                string clip = attackMove == "chop" ? "_atk_h2" : "_atk_h1";
                // The authored strike peaks near 40%; align it with the gameplay contact.
                float contact = attackMove == "chop" ? .25f : .36f;
                float frame = phase <= contact ? Mathf.Lerp(0,.4f,phase/contact) : Mathf.Lerp(.4f,1f,(phase-contact)/(1f-contact));
                Sample(actorAnimation, "actor", clip, frame, false);
                Sample(standAnimation, "stand", clip, frame, false);
            }
        }
        private void RefreshActorSkin(){
            skinUsed=body.skinIndex;if(diavolo){diavolo.SetActive(false);Destroy(diavolo);}actorMaterials.Dispose();
            diavolo=Create(skinUsed==1 ? "5dvl11" : "5dvl01");actorHand=null;actorSpace=null;hair=null;
            if(!diavolo)return;actorAnimation=diavolo.GetComponentInChildren<Animation>();CacheClips(actorAnimation,"actor");
            actorTransition=new PoseTransition(diavolo);actorSkins=diavolo.GetComponentsInChildren<EohSkinning>(true);if(skinUsed!=1)hair=new EohHairPose(diavolo);
            foreach(Transform bone in diavolo.GetComponentsInChildren<Transform>())if(bone.name.EndsWith(" r hand"))actorHand=bone;
            foreach(SkinnedMeshRenderer skin in diavolo.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin.name.Contains(" body")){actorSpace=skin.transform;break;}
        }
        private void LateUpdate()
        {
            if (!body) return;
            if(bundle && body.skinIndex!=skinUsed)RefreshActorSkin();
            RefreshVisibility();
            if (Time.time >= nextItemHide && body.modelLocator && body.modelLocator.modelTransform) {
                nextItemHide = Time.time + .2f;
                foreach (Renderer renderer in body.modelLocator.modelTransform.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = true;
            }
            materials.SetErasureContrast(false);
            bool auraOn = (erased || Time.time < auraUntil) && !(erased && Plugin.LowVfx.Value);
            bool jumpLegs=attackMove=="jump" && Time.time<attackUntil;
            materials.LegsOnly(jumpLegs,body.footPosition.y+1.25f);
            if(stand)stand.transform.localScale=standNormalScale*(jumpLegs?1.25f/1.05f:1f);
            if (diavolo) foreach (EohSkinning skin in actorSkins) skin.SetAura(auraOn);
            if (stand) foreach (EohSkinning skin in standSkins) skin.SetAura(auraOn && !jumpLegs);
            bool inAttack=Time.time<attackUntil;
            string pose=inAttack ? attackMove : body.characterMotor && !body.characterMotor.isGrounded ? "jump" : body.inputBank && body.inputBank.moveVector.sqrMagnitude>.05f ? "run" : "idle";
            if(inAttack && attackMove=="barrage" && standPrimary && standPrimary.Visible && !standPrimary.Arrived)pose="barrage_travel";
            float blend=inAttack ? Mathf.Min(.18f,AttackDuration*.28f) : .2f;
            if(Plugin.SmoothPoses.Value){
                if(actorTransition!=null)actorTransition.BeforeSample(pose,inAttack ? attackStarted : 0,Time.time,blend);
                if(standTransition!=null)standTransition.BeforeSample(pose,inAttack ? attackStarted : 0,Time.time,blend);
            }
            SampleMoves();
            if(Plugin.SmoothPoses.Value){
                if(actorTransition!=null)actorTransition.AfterSample(Time.time);
                if(standTransition!=null)standTransition.AfterSample(Time.time);
            }
            Quaternion rotation = body.characterDirection ? Quaternion.Euler(0, body.characterDirection.yaw, 0) : transform.rotation;
            Quaternion modelRotation = rotation;
            if (diavolo) diavolo.transform.SetPositionAndRotation(body.footPosition, modelRotation);
            if (diavolo && Time.time-dodgeStarted < .22f) diavolo.transform.position += rotation * Vector3.right * (Mathf.Sin((Time.time-dodgeStarted)/.22f*Mathf.PI)*.65f);
            if (stand) {
                float sequence=inAttack ? attackStarted : -1;
                if(sequence!=standOffsetSequence){standOffsetSequence=sequence;standOffsetFrom=standOffset;standOffsetBegan=Time.time;}
                Vector3 desiredOffset=inAttack ? new Vector3(.35f,.15f,1.3f) : new Vector3(.8f,.3f,-.8f);
                standOffset=Plugin.SmoothPoses.Value ? Vector3.Lerp(standOffsetFrom,desiredOffset,Mathf.SmoothStep(0,1,(Time.time-standOffsetBegan)/blend)) : desiredOffset;
                stand.transform.SetPositionAndRotation(body.footPosition+rotation*standOffset,modelRotation);
            }
            if(stand && jumpLegs)stand.transform.position=body.footPosition;
            if(stand && attackMove=="barrage" && inAttack && body.inputBank && !(standPrimary && standPrimary.Visible)){
                if(pelvis)pelvis.localRotation=pelvisNeutral;
                if(barrageTorso!=null)barrageTorso.Apply();
                Vector3 aim=body.inputBank.aimDirection.normalized;Vector3 right=Vector3.Cross(Vector3.up,aim);if(right.sqrMagnitude<.01f)right=rotation*Vector3.right;
                stand.transform.rotation=Quaternion.LookRotation(aim,Vector3.Cross(aim,right.normalized));
                if(leftShoulder && rightShoulder){Vector3 center=(CrimsonPose.VisualPosition(standSpace,leftShoulder)+CrimsonPose.VisualPosition(standSpace,rightShoulder))*.5f;stand.transform.position+=body.corePosition+aim*1.5f-center;}
                float beat=(Time.time-attackStarted)*Mathf.Max(.1f,body.attackSpeed)/.12f;
                CrimsonPose.Arm(rightHand,standSpace,body.corePosition+aim*Mathf.Lerp(1.7f,3.2f,(Mathf.Sin(beat*Mathf.PI)+1)*.5f)+right*.32f,right);
                CrimsonPose.Arm(leftHand,standSpace,body.corePosition+aim*Mathf.Lerp(3.2f,1.7f,(Mathf.Sin(beat*Mathf.PI)+1)*.5f)-right*.32f,-right);
            }
            if (stand && standPrimary && standPrimary.Visible) {
                Vector3 facing=Vector3.ProjectOnPlane(standPrimary.RenderDirection,Vector3.up);
                if(facing.sqrMagnitude<.001f)facing=rotation*Vector3.forward;
                stand.transform.SetPositionAndRotation(standPrimary.RenderPosition-Vector3.up*1.3f,Quaternion.LookRotation(facing));
                if(attackMove!="groundsmash" && leftShoulder && rightShoulder){
                    Vector3 shoulders=(CrimsonPose.VisualPosition(standSpace,leftShoulder)+CrimsonPose.VisualPosition(standSpace,rightShoulder))*.5f;
                    stand.transform.position+=standPrimary.RenderPosition-shoulders;
                }
            }
            if (stand && attackMove == "groundsmash" && Time.time < attackUntil && !(standPrimary && standPrimary.Visible)) {
                float phase = Mathf.Clamp01((Time.time - attackStarted) / Mathf.Max(.01f, attackUntil - attackStarted));
                float reach = phase <= .6f ? Mathf.SmoothStep(0f, 1f, phase / .6f) : 1f - Mathf.SmoothStep(0f, 1f, (phase - .6f) / .4f);
                stand.transform.position = Vector3.Lerp(stand.transform.position, smashTarget - rotation * Vector3.forward, reach);
            }
            if (hair != null) hair.Apply(-(rotation * Vector3.forward));
            if (stand && ultimateTarget && attackMove == "ultimate" && Time.time < attackUntil) {
                Vector3 toward = ultimateTarget.corePosition - body.corePosition;
                Vector3 direction = toward.sqrMagnitude > .01f ? toward.normalized : rotation * Vector3.forward;
                Vector3 destination = ultimateTarget.corePosition - direction * (ultimateTarget.radius + .65f) - Vector3.up;
                stand.transform.position = Vector3.Lerp(stand.transform.position, destination, Mathf.SmoothStep(0,1,(Time.time-ultimateStarted)/.35f));
                Vector3 flat = Vector3.ProjectOnPlane(direction,Vector3.up);
                if (flat.sqrMagnitude > .01f) stand.transform.rotation = Quaternion.LookRotation(flat);
                if (Time.time-ultimateStarted >= .3f) {
                    Vector3 collar = ultimateTarget.corePosition + Vector3.up * Mathf.Min(.5f,ultimateTarget.radius);
                    CrimsonPose.Arm(leftHand, standSpace, collar, -stand.transform.right+Vector3.up);
                }
            }
            if (impaled && rightHand && attackMove == "impale" && Time.time < attackUntil && Time.time < impaleHoldUntil){
                CrimsonPose.Arm(rightHand,standSpace,impaled.corePosition,rotation*Vector3.right);
                CrimsonPose.Arm(actorHand,actorSpace,impaled.corePosition,rotation*Vector3.right);
            }
            if (attackMove == "blood" && Time.time < attackUntil && body.inputBank && actorHand) {
                float age = Time.time - attackStarted;
                float reach = age <= .18f ? Mathf.SmoothStep(0,1,age/.18f) : 1f-Mathf.SmoothStep(0,1,(age-.18f)/.42f);
                Vector3 start = CrimsonPose.VisualPosition(actorSpace,actorHand);
                CrimsonPose.Arm(actorHand,actorSpace,Vector3.Lerp(start,body.corePosition+body.inputBank.aimDirection*1.1f,reach),rotation*Vector3.right);
            }
            if (grabbed && attackMove=="grab" && Time.time<attackUntil && (!independentPrimary || (standPrimary && standPrimary.Arrived))) {
                Vector3 collar = grabbed.corePosition + Vector3.up * Mathf.Min(.5f, grabbed.radius);
                CrimsonPose.Arm(leftHand, standSpace, collar, -body.transform.right + Vector3.up);
                float grabAge=AttackAge*(independentPrimary?1.3f:1.2f)/Mathf.Max(.1f,AttackDuration);
                float phase = Mathf.Clamp01((grabAge-.65f)/.2f);
                if (rightHand) CrimsonPose.Arm(rightHand, standSpace, Vector3.Lerp(CrimsonPose.VisualPosition(standSpace,rightHand), collar+Vector3.up*.25f,phase), body.transform.right+Vector3.up*.3f);
            }
        }
        public void Blood(Ray aim)
        {
            BloodFx.Throw(aim);
        }
        private void OnDestroy()
        {
            foreach (GameObject visual in new GameObject[] { diavolo, stand }) if (visual) {
                Destroy(visual);
            }
            materials.Dispose();actorMaterials.Dispose();
        }
    }
}
