using System;
using System.Collections.Generic;
using System.IO;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace EyesOfHeavenDio
{
    internal static class DioSpecialAssets
    {
        internal static GameObject Roller;
        internal static Shader Ghost;
        internal static void Load(Material gameMaterial)
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(typeof(DioPlugin).Assembly.Location), "diospecial"));
            if (!bundle) throw new InvalidOperationException("Missing diospecial bundle");
            Roller = bundle.LoadAsset<GameObject>("Assets/RoadRoller/RoadRoller.prefab");
            Ghost = bundle.LoadAsset<Shader>("Assets/RoadRoller/DioGhost.shader");
            if (!Roller || !Ghost) throw new InvalidOperationException("Missing special move assets");
            foreach (var renderer in Roller.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterial;
                var mat = new Material(gameMaterial); mat.mainTexture = source.mainTexture;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                if (mat.HasProperty("_EmColor")) mat.SetColor("_EmColor", Color.black);
                if (mat.HasProperty("_NormalTex")) mat.SetTexture("_NormalTex", source.GetTexture("_BumpMap"));
                renderer.sharedMaterial = mat;
            }
        }
    }
    internal static class DioSpecialGeometry
    {
        internal static bool ClearKnife(Vector3 point) => !Physics.CheckSphere(point, .9f,
            LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide);
        internal static Bounds TargetBounds(CharacterBody body)
        {
            var bounds = new Bounds(body.corePosition, Vector3.one * Mathf.Max(1f, body.radius * 2));
            var root = body.modelLocator && body.modelLocator.modelTransform ? body.modelLocator.modelTransform : body.transform;
            foreach (var hurt in root.GetComponentsInChildren<HurtBox>())
            {
                if (hurt.healthComponent != body.healthComponent) continue;
                var collider = hurt.GetComponent<Collider>(); if (collider) bounds.Encapsulate(collider.bounds);
            }
            return bounds;
        }
        internal static bool BodySpace(CharacterBody body, Vector3 position)
        {
            var capsule = body.GetComponent<CapsuleCollider>();
            float radius = capsule ? capsule.radius * Mathf.Max(body.transform.lossyScale.x, body.transform.lossyScale.z) : .5f;
            float half = capsule ? Mathf.Max(radius, capsule.height * body.transform.lossyScale.y * .5f) : 1f;
            Vector3 center = capsule ? capsule.transform.TransformPoint(capsule.center) - body.transform.position : body.corePosition - body.transform.position;
            return !Physics.CheckCapsule(position + center + Vector3.up*(half-radius), position + center - Vector3.up*(half-radius), Mathf.Max(.1f,radius-.03f), LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }
        internal static CharacterBody AimEnemy(CharacterBody owner, Ray ray, float range)
        {
            if (Physics.Raycast(ray, out var wall, range, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) range = wall.distance;
            CharacterBody result = null;
            foreach (var hit in Physics.SphereCastAll(ray,.7f,range,LayerIndex.entityPrecise.mask,QueryTriggerInteraction.Collide))
            {
                var hurt = hit.collider.GetComponent<HurtBox>(); var victim = hurt && hurt.healthComponent ? hurt.healthComponent.body : null;
                if (!victim || victim == owner || !victim.healthComponent.alive || victim.isPlayerControlled || !victim.teamComponent || victim.teamComponent.teamIndex == owner.teamComponent.teamIndex || hit.distance >= range) continue;
                result = victim; range = hit.distance;
            }
            return result;
        }
        internal static CharacterBody AimExEnemy(CharacterBody owner, Ray ray, float range)
        {
            CharacterBody result=null;float best=float.PositiveInfinity;
            foreach(var collider in Physics.OverlapSphere(owner.corePosition,range,LayerIndex.entityPrecise.mask,QueryTriggerInteraction.Collide))
            {
                var hurt=collider.GetComponent<HurtBox>();var victim=hurt&&hurt.healthComponent?hurt.healthComponent.body:null;
                if(!victim||victim==owner||!victim.healthComponent.alive||victim.isPlayerControlled||!victim.teamComponent||victim.teamComponent.teamIndex==owner.teamComponent.teamIndex)continue;
                Vector3 point=collider.ClosestPoint(ray.origin+ray.direction*Vector3.Distance(ray.origin,victim.corePosition));
                Vector3 offset=point-ray.origin;float distance=offset.magnitude;
                float angle=Vector3.Angle(ray.direction,offset);
                if(distance>range||angle>12f||Physics.Linecast(owner.corePosition,victim.corePosition,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))continue;
                float score=angle+distance*.08f;
                if(score<best){best=score;result=victim;}
            }
            return result;
        }
        internal static Vector3 SafeExMove(CharacterBody body, Vector3 from, Vector3 desired)
        {
            var capsule=body.GetComponent<CapsuleCollider>();
            float radius=capsule?capsule.radius*Mathf.Max(body.transform.lossyScale.x,body.transform.lossyScale.z):.5f;
            float half=capsule?Mathf.Max(radius,capsule.height*body.transform.lossyScale.y*.5f):1f;
            Vector3 center=capsule?capsule.transform.TransformPoint(capsule.center)-body.transform.position:body.corePosition-body.transform.position;
            float feetOffset=(body.transform.position-body.footPosition).y;
            return DioDashCollision.Move(from,desired,center,radius,half,feetOffset,LayerIndex.world.mask);
        }
    }
    // Weapon state; suspends only this actor's movement, never other players' input/cameras.
    public abstract class DioSpecialState : BaseSkillState
    {
        private bool motorEnabled, kinematicEnabled;
        protected Vector3 original;
        protected bool movementLocked;
        protected void LockMovement()
        {
            GetComponent<DioStandJump>()?.Cancel(); original = transform.position;
            if (characterMotor)
            {
                motorEnabled = characterMotor.enabled; kinematicEnabled = characterMotor.Motor.enabled;
                characterMotor.velocity = Vector3.zero; characterMotor.enabled = false; characterMotor.Motor.enabled = false;
                movementLocked = true;
            }
        }
        protected void MoveTo(Vector3 point) { TeleportHelper.TeleportGameObject(gameObject, point); }
        public override void OnExit()
        {
            if (movementLocked && characterMotor)
            { characterMotor.enabled = motorEnabled; characterMotor.Motor.enabled = kinematicEnabled; characterMotor.velocity = Vector3.zero; }
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
    public sealed class DioRoadRoller : DioSpecialState
    {
        private Vector3 landing, returnPoint, apex, footOffset, escape, steerOrigin;
        private GameObject roller, marker;
        private CharacterModel model;
        private DioRollerPose pose;
        private DioAudioPlayback punchesAudio;
        private float audioAge;
        private bool finishedPunch;
        private bool hidden, returned, slammed, escaped, exploded, invincible;
        private float dropHeight, slamAt, nextPunch;
        private const float DropSeconds = DioSpecialMath.RollerDrop;
        public override void OnEnter()
        {
            base.OnEnter();
            // The host must protect a remote-owned DIO before the local-motion guard.
            if(NetworkServer.active)
            {
                characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);invincible=true;
                DioPlugin.Log.LogInfo("Road Roller host protection enabled; locally controlled="+isAuthority);
            }
            if(!isAuthority)return;
            LockMovement();footOffset=original-characterBody.footPosition;
            landing=characterBody.footPosition;
            if(Physics.Raycast(characterBody.corePosition,Vector3.down,out var ground,2000,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))landing=ground.point;
            steerOrigin=landing;returnPoint=landing+footOffset+Vector3.up*.15f;
            model=characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>();
            apex=original+Vector3.up*8;
            GetComponent<DioVisualDriver>()?.Action("_bas_jump_lp",.3f,false);
            DioFeedback.Sound(gameObject,"stand-swing",.8f);
            marker=new GameObject("RoadRollerLandingMarker");marker.transform.position=landing+Vector3.up*.08f;marker.AddComponent<DioRollerMarker>();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();if(!isAuthority)return;
            audioAge=punchesAudio?Mathf.Max(audioAge,punchesAudio.PlaybackSeconds):audioAge+Time.fixedDeltaTime;
            if(!characterBody.healthComponent.alive){outer.SetNextStateToMain();return;}
            if(!returned&&fixedAge<DioSpecialMath.RollerLeap){MoveTo(Vector3.Lerp(original,apex,fixedAge/DioSpecialMath.RollerLeap));return;}
            if(!hidden&&!returned){if(model)model.invisibilityCount++;hidden=true;DioFeedback.Sound(gameObject,"time-stop",.7f);}
            if(!returned&&fixedAge<DioSpecialMath.RollerLeap+DioSpecialMath.RollerHidden)
            {
                // Input remains live while the motor is suspended: steer the invisible
                // actor and strike marker together using normal movement controls.
                Vector3 move=inputBank?Vector3.ProjectOnPlane(inputBank.moveVector,Vector3.up):Vector3.zero;
                Vector3 candidate=landing+Vector3.ClampMagnitude(move,1)*Mathf.Max(22f,moveSpeedStat*2)*Time.fixedDeltaTime;
                Vector3 horizontal=Vector3.ProjectOnPlane(candidate-steerOrigin,Vector3.up);
                horizontal=Vector3.ClampMagnitude(horizontal,60);candidate.x=steerOrigin.x+horizontal.x;candidate.z=steerOrigin.z+horizontal.z;
                if(Physics.Raycast(candidate+Vector3.up*1.5f,Vector3.down,out var floor,2000,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))landing=floor.point;
                returnPoint=landing+footOffset+Vector3.up*.15f;
                MoveTo(returnPoint+Vector3.up*8);
                if(marker)marker.transform.position=landing+Vector3.up*.08f;
                return;
            }
            if(!returned)
            {
                returned=true;audioAge=0;punchesAudio=DioFeedback.Sound(gameObject,"road-roller-voice",1f);Reveal();dropHeight=DioSpecialMath.RollerDropHeight(original.y,landing.y);
                roller=Object.Instantiate(DioSpecialAssets.Roller,landing+Vector3.up*dropHeight,Quaternion.identity);
                roller.AddComponent<DioTimeStopExempt>();
                pose=gameObject.AddComponent<DioRollerPose>();pose.Begin();pose.Mode=0;
                GetComponent<DioVisualDriver>()?.Action("_bas_nut",DropSeconds,false);
                DioFeedback.Sound(gameObject,"road-fall",1f);
            }
            float drop=Mathf.Clamp01(audioAge/DropSeconds);
            if(!slammed)
            {
                if(roller)roller.transform.position=landing+Vector3.up*dropHeight*(1-drop*drop);
                MoveTo(landing+Vector3.up*(3.3f+dropHeight*(1-drop*drop)));
                if(drop<1)return;
                slammed=true;slamAt=DropSeconds;nextPunch=DioSpecialMath.RollerVoicePunchStart;
                if(marker)Object.Destroy(marker);
                DioFeedback.Sound(gameObject,"impact-heavy",1);DioFeedback.Sound(gameObject,"road-crack",.9f);
                DioFeedback.Impact(landing+Vector3.up,5);DioGroundBreak.Spawn(landing,2.5f);
                Blast(40.5f,10,150);
                pose.Mode=1;
                GetComponent<DioVisualDriver>()?.Action("_bas_nut",DioSpecialMath.RollerPunch,false,true);

            }
            float phase=audioAge-slamAt;
            if(pose)pose.PunchPhase=audioAge<DioSpecialMath.RollerVoicePunchStart?Mathf.Min(.24f,phase):.25f+audioAge-DioSpecialMath.RollerVoicePunchStart;
            if(!finishedPunch&&audioAge>=DioSpecialMath.RollerVoiceFinisher)
            {
                finishedPunch=true;if(pose)pose.Finisher=true;
                DioFeedback.Sound(gameObject,"impact-heavy",1f);
                DioFeedback.Impact(landing+Vector3.up*3.4f,3f);
                DioGroundBreak.Spawn(landing,2f);Blast(12,10,200);
                AddRecoil(-5f,-1f,-.5f,.5f);
            }
            if(phase<DioSpecialMath.RollerPunch)
            {
                if(!finishedPunch&&audioAge>=nextPunch)
                {
                    nextPunch+=.15f;
                    Vector3 hit=landing+Vector3.up*3.4f+transform.forward*.5f+transform.right*(Mathf.Sin(phase*Mathf.PI/.15f)*.28f);
                    DioFeedback.Impact(hit,.9f);DioFeedback.Sound(gameObject,"impact",.35f);Blast(.8f,10,0);
                }
                return;
            }
            if(!escaped)
            {
                escaped=true;
                pose.Mode=2;GetComponent<DioVisualDriver>()?.Action("_bas_jump_lp",DioSpecialMath.RollerEscape,false);
                DioFeedback.Sound(gameObject,"stand-swing",.9f);
                Vector3 direction=Vector3.ProjectOnPlane(-GetAimRay().direction,Vector3.up).normalized;
                if(direction.sqrMagnitude<.1f)direction=-transform.forward;
                escape=returnPoint+direction*9;
                if(Physics.Raycast(escape+Vector3.up*6,Vector3.down,out var floor,100,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))escape=floor.point+footOffset+Vector3.up*.15f;
                if(!DioSpecialGeometry.BodySpace(characterBody,escape))escape=returnPoint;
            }
            float hop=Mathf.Clamp01((phase-DioSpecialMath.RollerPunch)/DioSpecialMath.RollerEscape);
            MoveTo(Vector3.Lerp(landing+Vector3.up*3.3f,escape,hop)+Vector3.up*(Mathf.Sin(hop*Mathf.PI)*7));
            if(hop>=1&&!exploded)
            {
                exploded=true;if(roller)Object.Destroy(roller);
                DioFeedback.Sound(gameObject,"road-explosion",1);DioRollerExplosion.Spawn(landing+Vector3.up*1.2f);DioFeedback.Impact(landing+Vector3.up,6);Blast(35,12,900);
            }
            if(exploded&&phase>=DioSpecialMath.RollerPunch+DioSpecialMath.RollerEscape+.2f)outer.SetNextStateToMain();
        }
        private void Blast(float coefficient,float radius,float force)
        {
            if(NetworkServer.active)new BlastAttack{attacker=gameObject,inflictor=gameObject,teamIndex=GetTeam(),position=landing+Vector3.up,radius=radius,baseDamage=damageStat*coefficient,baseForce=force,bonusForce=Vector3.up*force,crit=RollCrit(),procCoefficient=coefficient<1?.1f:1,falloffModel=BlastAttack.FalloffModel.None,attackerFiltering=AttackerFiltering.NeverHitSelf}.Fire();
        }
        private void Reveal(){if(hidden&&model)model.invisibilityCount=Mathf.Max(0,model.invisibilityCount-1);hidden=false;}
        public override void OnExit()
        {
            Reveal();if(marker)Object.Destroy(marker);if(roller)Object.Destroy(roller);
            if(punchesAudio)Object.Destroy(punchesAudio.gameObject);
            if(pose){pose.Restore();Object.Destroy(pose);}
            if(isAuthority&&movementLocked)MoveTo(escaped?escape:slammed?returnPoint:original);
            if(invincible&&characterBody)
            {
                characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);invincible=false;
                DioPlugin.Log.LogInfo("Road Roller host protection released.");
            }
            base.OnExit();
        }
    }
    public sealed class DioRollerMarker : MonoBehaviour
    {
        private Material material;
        private void Start()
        {
            material=new Material(DioFeedback.RingShader);var line=gameObject.AddComponent<LineRenderer>();line.sharedMaterial=material;
            line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.startWidth=line.endWidth=.08f;line.startColor=line.endColor=new Color(1,.75f,.2f,.6f);
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*4);}
        }
        private void OnDestroy(){if(material)Destroy(material);}
    }
    public sealed class DioExKnives : DioSpecialState
    {
        private bool invincible,savedSuppress;
        private DioVisualDriver visual;
        private CharacterBody target;
        private Vector3 dashEnd;
        private float arrived;
        private bool connected, resumed;
        private int thrown;
        private GameObject ghost;
        private Transform ghostHand, ghostVisualSpace;
        private Animation ghostAnimation;
        private DioGrabHold hold;
        private readonly List<Material> materials=new List<Material>();
        private readonly List<DioKnife> knives=new List<DioKnife>();
        public override void OnEnter()
        {
            base.OnEnter();if(!isAuthority)return;
            if(NetworkServer.active){characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);invincible=true;}
            visual=GetComponent<DioVisualDriver>();savedSuppress=visual.SuppressStand;
            LockMovement();var ray=GetAimRay();target=DioSpecialGeometry.AimExEnemy(characterBody,ray,38);
            if(target)ray=new Ray(characterBody.corePosition,(target.corePosition-characterBody.corePosition).normalized);
            float range=target?Mathf.Clamp(Vector3.Distance(characterBody.corePosition,DioSpecialGeometry.TargetBounds(target).ClosestPoint(characterBody.corePosition))-5f,0,30):30f;
            dashEnd=original+ray.direction*range;
            visual.ExThrust(ray,DioSpecialMath.ExDash);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();if(!isAuthority)return;
            if(fixedAge<DioSpecialMath.ExDash)
            {
                MoveTo(DioSpecialGeometry.SafeExMove(characterBody,transform.position,Vector3.Lerp(original,dashEnd,fixedAge/DioSpecialMath.ExDash)));
                var aim=GetAimRay();if(target)aim=new Ray(characterBody.corePosition,(target.corePosition-characterBody.corePosition).normalized);
                visual.SetBarrageAim(aim);visual.SetStandReach(Mathf.Lerp(2f,7f,Mathf.SmoothStep(0,1,fixedAge/DioSpecialMath.ExDash)));return;
            }
            if(!connected)
            {
                MoveTo(DioSpecialGeometry.SafeExMove(characterBody,transform.position,dashEnd));
                // Reacquire once near the end so a small target movement does not waste the rush.
                if(!target||!target.healthComponent.alive)target=DioSpecialGeometry.AimExEnemy(characterBody,GetAimRay(),9);
                if(!target||!target.healthComponent.alive||Vector3.Distance(characterBody.corePosition,DioSpecialGeometry.TargetBounds(target).ClosestPoint(characterBody.corePosition))>8.5f||Physics.Linecast(characterBody.corePosition,target.corePosition,LayerIndex.world.mask))
                {if(fixedAge>.5f){GetComponent<DioExtraMoves>()?.ExFailed();outer.SetNextStateToMain();}return;}
                connected=true;arrived=fixedAge;
                visual.EndBarrageAim();visual.SuppressStand=true;
                visual.Action("_spa_01_lp",DioSpecialMath.ExRecovery,false,true);
                DioFeedback.Sound(gameObject,"ex-stand-power",1f);
                DioFeedback.Impact(DioSpecialGeometry.TargetBounds(target).ClosestPoint(characterBody.corePosition),1.1f);
                DioFeedback.Sound(gameObject,"impact-heavy",.6f);
                if(NetworkServer.active&&!target.GetComponent<DioGrabHold>())
                {hold=target.gameObject.AddComponent<DioGrabHold>();hold.Begin(characterBody,target,Vector3.forward,true,DioSpecialMath.ExFreeze);}
                DioFeedback.ExTransition(characterBody,false);
                ghost=Object.Instantiate(GetComponent<DioVisualDriver>().StandPrefab);ghost.name="SubtleWorldKnifeRush";
                GetComponent<DioVisualDriver>().SummonFeedback(characterBody.corePosition);
                foreach(var renderer in ghost.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var source=renderer.sharedMaterial;
                    var mat=source.shader.name=="Dio/ShadowStandFade"?new Material(source):new Material(DioSpecialAssets.Ghost);
                    mat.mainTexture=source.mainTexture;mat.SetColor("_Color",new Color(1,.92f,.7f,.14f));
                    renderer.sharedMaterial=mat;materials.Add(mat);
                }
                foreach(var skin in ghost.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.name.EndsWith(" body")){ghostVisualSpace=skin.transform;break;}
                ghostAnimation=ghost.GetComponentInChildren<Animation>(true);
                foreach(var bone in ghost.GetComponentsInChildren<Transform>(true))if(bone.name.EndsWith(" r hand",StringComparison.Ordinal)){ghostHand=bone;break;}
                var animation=ghostAnimation;
                if(animation)foreach(AnimationState state in animation)if(state.name.EndsWith("_atk_l3",StringComparison.Ordinal)){state.wrapMode=WrapMode.Loop;state.speed=.7f;animation.Play(state.name);break;}
            }
            if(!target||!target.healthComponent.alive){outer.SetNextStateToMain();return;}
            float age=fixedAge-arrived;
            if(!resumed&&age>=DioSpecialMath.ExFreeze){resumed=true;ReleaseTarget();DioFeedback.ExTransition(characterBody,true);DioFeedback.Sound(gameObject,"stand-swing",.55f);}
            // All throws happen during the short target-only stop; knives leave a hand position.
            while(thrown<DioSpecialMath.ExKnifeCount&&age>=thrown*DioSpecialMath.ExThrowInterval)
            {
                int index=thrown++;var bounds=DioSpecialGeometry.TargetBounds(target);
                float a=index*Mathf.PI*2/DioSpecialMath.ExKnifeCount;
                var offset=DioSpecialMath.KnifeOffset(a,bounds.extents.x,bounds.extents.z);
                Vector3 outward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 point=bounds.center+new Vector3(offset.x,((index%3)-1)*bounds.extents.y*.55f,offset.z);
                if(Physics.Raycast(point+Vector3.up*2,Vector3.down,out var ringFloor,5,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))point.y=Mathf.Max(point.y,ringFloor.point.y+1.15f);
                Vector3 hand=point+outward*2f;
                if(!DioSpecialGeometry.ClearKnife(point)||!DioSpecialGeometry.ClearKnife(hand)||Physics.CheckCapsule(hand,point,.9f,LayerIndex.world.mask|LayerIndex.entityPrecise.mask,QueryTriggerInteraction.Collide))continue;
                if(ghost)
                {
                    ghost.transform.position=hand-Vector3.up*1.35f;ghost.transform.rotation=Util.QuaternionSafeLookRotation(bounds.center-hand);
                    if(ghostAnimation)ghostAnimation.Sample();
                    // Translate the Stand so the actual animated hand meets the validated
                    // throw origin, rather than moving the origin into terrain/targets.
                    if(ghostHand)ghost.transform.position+=hand-DioPoseSolver.VisualPosition(ghostVisualSpace,ghostHand);
                }
                Vector3 scatter=new Vector3(Mathf.Cos(index*2.4f)*Mathf.Min(.55f,bounds.extents.x*.35f),Mathf.Sin(index*1.7f)*Mathf.Min(.7f,bounds.extents.y*.35f),Mathf.Sin(index*2.4f)*Mathf.Min(.55f,bounds.extents.z*.35f));
                knives.Add(DioKnife.ThrowStaged(gameObject,hand,point,target,Time.time+Mathf.Max(0,DioSpecialMath.ExFreeze+.025f+index*DioSpecialMath.ExReleaseInterval-age),damageStat*1.6f,RollCrit(),scatter));
                if(index%5==0)DioFeedback.Sound(gameObject,"swing",.16f);
            }
            if(ghost&&age>DioSpecialMath.ExFreeze+.06f)ghost.SetActive(false);
            if(age>=DioSpecialMath.ExRecovery)outer.SetNextStateToMain();
        }
        private void ReleaseTarget(){if(hold){hold.Release();Object.Destroy(hold);hold=null;}}
        public override void OnExit()
        {
            ReleaseTarget();
            if(invincible&&characterBody){characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);invincible=false;}
            if(visual){visual.EndBarrageAim();visual.EndExPose();visual.SuppressStand=savedSuppress;}
            if(connected&&!resumed&&isAuthority)DioFeedback.ExTransition(characterBody,true);
            if(ghost)Object.Destroy(ghost);foreach(var mat in materials)if(mat)Object.Destroy(mat);
            if(!resumed||!characterBody||!characterBody.healthComponent.alive)foreach(var knife in knives)if(knife)Object.Destroy(knife.gameObject);
            base.OnExit();
        }
    }
}
