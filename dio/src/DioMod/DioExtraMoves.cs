using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EyesOfHeavenDio
{
    public sealed class DioExtraMoves : MonoBehaviour
    {
        private CharacterBody body;
        private EntityStateMachine weapon;
        private float barrageReady, drainReady, rollerReady, exReady;
        public bool RoadRollerActive => weapon && weapon.state is DioRoadRoller;
        public bool SpecialBusy => weapon && (weapon.state is DioRoadRoller || weapon.state is DioExKnives);
        public void ExFailed() { exReady = Time.time + 3f; }
        public void RoadRollerFailed() { rollerReady = Time.time + 1f; }
        public void BarrageEnded() { barrageReady = Time.time + 8f; }
        private void Start()
        {
            body = GetComponent<CharacterBody>();
            foreach (var machine in GetComponents<EntityStateMachine>()) if (machine.customName == "Weapon") weapon = machine;
        }
        private void Update()
        {
            if (!DioInput.Allowed(body) || !body || !body.hasEffectiveAuthority || !body.healthComponent.alive || !weapon || Time.timeScale == 0 || (MPEventSystemManager.kbmEventSystem && MPEventSystemManager.kbmEventSystem.isCursorVisible)) return;
            if (SpecialBusy) return;
            if (DioInput.Pressed(DioPlugin.StandBlockKey.Value, 5) && weapon.SetInterruptState(new DioStandBlock(), InterruptPriority.Skill)) return;
            if (DioInput.Pressed(DioPlugin.RoadRollerKey.Value, 3) && Time.time >= rollerReady &&
                weapon.SetInterruptState(new DioRoadRoller(), InterruptPriority.PrioritySkill)) { rollerReady = Time.time + 45; return; }
            if (DioInput.Pressed(DioPlugin.ExKnivesKey.Value, 4) && Time.time >= exReady &&
                weapon.SetInterruptState(new DioExKnives(), InterruptPriority.PrioritySkill)) { exReady = Time.time + 40; return; }
            if (DioInput.Pressed(DioPlugin.BarrageKey.Value, 0) && Time.time >= barrageReady &&
                weapon.SetInterruptState(new DioBarrage(), InterruptPriority.Skill)) barrageReady = Time.time + 8;
            if (DioInput.Pressed(DioPlugin.DrainKey.Value, 1) && Time.time >= drainReady &&
                weapon.SetInterruptState(new DioBloodDrain(), InterruptPriority.Skill)) drainReady = Time.time + 12;
        }
        private GUIStyle moveStyle,cooldownStyle;
        private readonly System.Collections.Generic.List<RectTransform> buffRects=new System.Collections.Generic.List<RectTransform>();
        private Vector2 panelAnchor;private readonly Vector3[] healthCorners=new Vector3[4];
        private void OnGUI()
        {
            if(!DioInput.IsLocal(body)||!body||!body.hasEffectiveAuthority||!Run.instance)return;
            panelAnchor=FindPanelAnchor();
            DrawMove("Barrage","MUDA barrage",DioPlugin.BarrageKey.Value,0,barrageReady);
            DrawMove("BloodDrain","Blood drain",DioPlugin.DrainKey.Value,1,drainReady);
            DrawMove("StandJump","Stand jump",DioPlugin.StandJumpKey.Value,2,GetComponent<DioStandJump>()?.ReadyAt??0);
            DrawMove("RoadRoller","Ultimate: Road Roller",DioPlugin.RoadRollerKey.Value,3,rollerReady);
            DrawMove("ExKnives","EX knives",DioPlugin.ExKnivesKey.Value,4,exReady);
            DrawMove("StandBlock","Stand block",DioPlugin.StandBlockKey.Value,5,0,GetComponent<DioStandGuard>()?.Blocking==true?"GUARD":"HOLD");
        }
        private Vector2 FindPanelAnchor(){
            float scale=Mathf.Clamp(Screen.height/1080f,.7f,1.5f);Vector2 result=new Vector2(20*scale,Screen.height-100*scale);
            foreach(var hud in RoR2.UI.HUD.readOnlyInstanceList)if(hud && hud.targetBodyObject==gameObject && hud.healthBar){
                RectTransform rect=hud.healthBar.transform as RectTransform;if(!rect)continue;
                Canvas canvas=rect.GetComponentInParent<Canvas>();Camera camera=canvas && canvas.renderMode!=RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                rect.GetWorldCorners(healthCorners);float left=float.PositiveInfinity,top=float.NegativeInfinity;
                foreach(Vector3 corner in healthCorners){Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,corner);left=Mathf.Min(left,point.x);top=Mathf.Max(top,point.y);}
                float reservedTop=top;
                if(hud.buffDisplay){hud.buffDisplay.GetComponentsInChildren<RectTransform>(false,buffRects);foreach(RectTransform buffs in buffRects){buffs.GetWorldCorners(healthCorners);foreach(Vector3 corner in healthCorners)reservedTop=Mathf.Max(reservedTop,RectTransformUtility.WorldToScreenPoint(camera,corner).y);}}
                // Keep the panel against the left safe edge, above both health and buff rows.
                result=new Vector2(16*scale,Screen.height-reservedTop-54*scale);break;
            }
            result.y=Mathf.Max(6*34*scale+28,result.y);return result;
        }
        private void DrawMove(string icon,string name,KeyCode key,int row,float ready,string status=null)
        {
            float scale=Mathf.Clamp(Screen.height/1080f,.7f,1.5f),x=panelAnchor.x,y=panelAnchor.y-(5-row)*34*scale;
            var old=GUI.color;GUI.color=new Color(.055f,.035f,.015f,.82f);
            GUI.DrawTexture(new Rect(x,y,330*scale,32*scale),Texture2D.whiteTexture);
            GUI.color=ready>Time.time?new Color(.5f,.5f,.5f,.85f):Color.white;
            var texture=DioUi.Texture(icon);if(texture)GUI.DrawTexture(new Rect(x+3*scale,y+2*scale,28*scale,28*scale),texture,ScaleMode.ScaleToFit);
            GUI.color=old;
            if(moveStyle==null){moveStyle=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleLeft};cooldownStyle=new GUIStyle(moveStyle){alignment=TextAnchor.MiddleRight};}
            moveStyle.fontSize=cooldownStyle.fontSize=Mathf.RoundToInt(14*scale);
            string binding=ExtraGamepad.RecentlyUsed?ExtraGamepad.Label(DioPlugin.GamepadMoves[row].Value):key.ToString();
            GUI.Label(new Rect(x+38*scale,y,231*scale,32*scale),binding+"  "+name,moveStyle);
            cooldownStyle.normal.textColor=ready>Time.time?new Color(1,.45f,.45f):new Color(.85f,.9f,.85f);
            GUI.Label(new Rect(x+270*scale,y,52*scale,32*scale),status??(ready>Time.time?Mathf.CeilToInt(ready-Time.time)+"s":"READY"),cooldownStyle);
        }
    }
    public sealed class DioBarrage : BaseSkillState
    {
        private LocalStandCombat.StandPrimaryController stand;
        private DioVisualDriver visual;
        private DioAudioPlayback barrageAudio;
        private float nextPunch=.28f, nextVoice;
        private float nextSync;
        private HurtBox lastSent;
        public override void OnEnter()
        {
            base.OnEnter();
            stand=LocalStandCombat.StandPrimaryController.Of(gameObject);
            stand.BeginContinuous(GetAimRay(),isAuthority);
            visual=GetComponent<DioVisualDriver>();
            if(visual)visual.SustainBarrage(GetAimRay());
        }
        public override void Update()
        {
            base.Update();
            if(visual)visual.SustainBarrage(GetAimRay());
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(isAuthority && (!DioInput.Allowed(characterBody) || !DioInput.Held(DioPlugin.BarrageKey.Value,0))){outer.SetNextStateToMain();return;}
            stand.TickContinuous(GetAimRay(),Time.fixedDeltaTime,isAuthority);
            if(isAuthority && (stand.Target!=lastSent || fixedAge>=nextSync)){
                lastSent=stand.Target;nextSync=fixedAge+.5f;
                DioBarrageTargetMessage.Publish(gameObject,lastSent);
            }
            Ray ray=GetAimRay();
            bool ready=stand.Active ? stand.TryAttackRay(out ray) : !stand.Visible;
            if(!stand.Active)ray=GetAimRay();
            if(!ready){nextPunch=fixedAge+.06f;return;}
            if(fixedAge>=nextVoice){
                nextVoice=fixedAge+DioFeedback.ClipDuration("barrage",4.4f);
                barrageAudio=DioFeedback.Sound(gameObject,"barrage",1f);
            }
            if(isAuthority && fixedAge>=nextPunch){
                nextPunch=fixedAge+Mathf.Max(.07f,.156f/LocalStandCombat.StandPrimaryRules.AnimationSpeed(attackSpeedStat));
                new BulletAttack { owner=gameObject,weapon=gameObject,origin=ray.origin,aimVector=ray.direction,
                    maxDistance=stand.Active?3.5f:8f,radius=1.1f,damage=damageStat*1.1f,force=30f,
                    isCrit=RollCrit(),procCoefficient=.4f,bulletCount=1,smartCollision=true,stopperMask=LayerIndex.world.mask,
                    hitEffectPrefab=null,falloffModel=BulletAttack.FalloffModel.None,
                    hitCallback=(BulletAttack attack,ref BulletAttack.BulletHit hit)=>{
                        if(hit.hitHurtBox && hit.hitHurtBox.healthComponent && hit.hitHurtBox.healthComponent.alive && hit.hitHurtBox.healthComponent.body && hit.hitHurtBox.healthComponent.body.teamComponent && hit.hitHurtBox.healthComponent.body.teamComponent.teamIndex!=GetTeam()){
                            DioFeedback.Sound(gameObject,"impact",.35f);DioFeedback.Impact(hit.point,.3f);
                        }
                        return BulletAttack.defaultHitCallback(attack,ref hit);
                    }
                }.Fire();
            }
        }
        public override void OnExit()
        {
            if(barrageAudio)Object.Destroy(barrageAudio.gameObject);
            if(stand)stand.StopContinuous();
            if(visual)visual.StopBarrage();
            GetComponent<DioExtraMoves>()?.BarrageEnded();
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.PrioritySkill;
    }
    public sealed class DioBloodDrain : BaseSkillState
    {
        private bool fired;
        private DioGrabHold hold;
        private DioDrainPose grabPose;
        private float caughtAt, nextDrain;
        private int ticks;
        private bool crit;
        public override void OnEnter()
        {
            base.OnEnter(); StartAimMode(2.5f);
            GetComponent<DioVisualDriver>()?.Action("_atk_l2", 2.1f, false);
            DioFeedback.Sound(gameObject, "swing", .8f);
            crit = RollCrit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (NetworkServer.active && !fired && fixedAge >= .3f)
            {
                fired = true; var ray = GetAimRay();
                // Probe only: damage comes from the drain ticks after a successful grab.
                new BulletAttack { owner = gameObject, weapon = gameObject, origin = ray.origin, aimVector = ray.direction,
                    maxDistance = 8, radius = .8f, damage = 0, procCoefficient = 0, bulletCount = 1,
                    hitCallback = (BulletAttack attack, ref BulletAttack.BulletHit hit) => {
                        var health = hit.hitHurtBox ? hit.hitHurtBox.healthComponent : null;
                        var victim = health ? health.body : null;
                        if (victim && health.alive && !victim.isPlayerControlled && victim.teamComponent &&
                            victim.teamComponent.teamIndex != GetTeam() && !victim.GetComponent<DioGrabHold>())
                        {
                            hold = victim.gameObject.AddComponent<DioGrabHold>();
                            hold.Begin(characterBody, victim, ray.direction);
                            grabPose=gameObject.AddComponent<DioDrainPose>();grabPose.Begin(victim);
                            caughtAt = fixedAge; nextDrain = fixedAge;
                            HealToFull();
                            DioFeedback.ServerSound(gameObject, "blood-drain-voice", 1);
                            DioFeedback.Impact(victim.corePosition, 1.4f, true);
                        }
                        // Stop at the first enemy or wall; never pierce cover or grab several targets.
                        return !hit.collider;
                    }
                }.Fire();
            }
            if (NetworkServer.active && hold && hold.Victim && hold.Victim.healthComponent.alive && fixedAge >= nextDrain && ticks < 6)
            {
                var victim = hold.Victim;
                ticks++; nextDrain += .25f;
                DioFeedback.Impact(victim.corePosition, .65f, true);
                victim.healthComponent.TakeDamage(new DamageInfo { attacker = gameObject, inflictor = gameObject,
                    damage = damageStat, crit = crit, procCoefficient = .2f, position = victim.corePosition });
                if (ticks == 6) HealToFull();
            }
            if (isAuthority && ((hold && fixedAge >= caughtAt + 1.5f) || (!hold && fixedAge >= 1.1f))) outer.SetNextStateToMain();
        }
        private void HealToFull()
        {
            var health = characterBody.healthComponent;
            if (health && health.alive) health.Heal(Mathf.Max(0, health.fullHealth - health.health), default(ProcChainMask));
        }
        public override void OnExit()
        {
            if(grabPose){grabPose.enabled=false;Object.Destroy(grabPose);}
            if (hold) { hold.Release(); Object.Destroy(hold); }
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
    public sealed class DioKnife : MonoBehaviour
    {
        private GameObject owner;
        private Vector3 direction;
        private float damage, traveled, stoppedTravel;
        private DioTimeWorld stopWindow;
        private bool staged;
        private Vector3 stagePoint;
        private float releaseAt;
        private float hitVolume = .6f;
        private CharacterBody exTarget;
        private Vector3 targetOffset;
        public static DioKnife ThrowStaged(GameObject owner, Vector3 hand, Vector3 point, CharacterBody target, float release, float damage, bool crit,Vector3 targetOffset=default)
        {
            var knife = Launch(owner, hand, (point-hand).normalized, damage, crit);
            knife.hitVolume = .22f; knife.staged = true; knife.stagePoint = point; knife.releaseAt = release; knife.exTarget = target; knife.targetOffset=targetOffset;return knife;
        }
        private bool crit;
        private static Material material;
        private static Mesh mesh;
        public static DioKnife Launch(GameObject owner, Vector3 origin, Vector3 direction, float damage, bool crit)
        {
            var obj = new GameObject("DioKnife");
            obj.AddComponent<MeshFilter>(); obj.AddComponent<MeshRenderer>();
            obj.name = "DioKnife"; obj.transform.position = origin;
            obj.transform.rotation = Util.QuaternionSafeLookRotation(direction) * Quaternion.AngleAxis(Random.Range(-50f, 50f), Vector3.forward);
            obj.transform.localScale = Vector3.one;
            var knife = obj.AddComponent<DioKnife>(); knife.owner = owner; knife.direction = direction; knife.damage = damage; knife.crit = crit;
            if(!material)material = new Material(DioFeedback.RingShader);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if(!mesh)mesh=DioKnifeMesh.Create();
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            return knife;
        }
        private void FixedUpdate()
        {
            if (!owner) { Destroy(gameObject); return; }
            if (staged)
            {
                if (!exTarget || !exTarget.healthComponent.alive) { Destroy(gameObject); return; }
                if (Time.time < releaseAt)
                {
                    Vector3 next = Vector3.MoveTowards(transform.position, stagePoint, 65f * Time.fixedDeltaTime);
                    if (!DioSpecialGeometry.ClearKnife(next) || Physics.Linecast(transform.position, next, LayerIndex.world.mask)) { Destroy(gameObject); return; }
                    transform.position = next;
                    transform.rotation = Util.QuaternionSafeLookRotation(exTarget.corePosition+targetOffset - next);
                    return;
                }
                if (!DioSpecialGeometry.ClearKnife(transform.position)) { Destroy(gameObject); return; }
                staged = false; direction = (exTarget.corePosition+targetOffset - transform.position).normalized;
                transform.rotation = Util.QuaternionSafeLookRotation(direction);
            }
            float distance = 65 * Time.fixedDeltaTime;
            if (DioTimeWorld.StopsProjectileOf(owner))
            {
                if (stopWindow != DioTimeWorld.Active) { stopWindow = DioTimeWorld.Active; stoppedTravel = 0; }
                distance = Mathf.Min(distance, Mathf.Max(0, 3f - stoppedTravel));
                if (distance <= 0) return;
                stoppedTravel += distance;
            }
            else { stopWindow = null; stoppedTravel = 0; }
            bool collided = false;
            new BulletAttack { owner = owner, weapon = owner, origin = transform.position, aimVector = direction,
                maxDistance = distance, radius = .18f, damage = damage, isCrit = crit, procCoefficient = .7f,
                bulletCount = 1, hitEffectPrefab = null, falloffModel = BulletAttack.FalloffModel.None,
                hitCallback = (BulletAttack attack, ref BulletAttack.BulletHit hit) => { if (hit.collider) { if(!collided)DioFeedback.KnifeImpact(owner,hit.point,hitVolume);collided = true; } return BulletAttack.defaultHitCallback(attack, ref hit); }
            }.Fire();
            transform.position += direction * distance; traveled += distance;
            if (collided || traveled >= 120) Destroy(gameObject);
        }
    }
}
