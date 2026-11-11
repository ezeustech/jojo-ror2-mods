using RoR2;
using UnityEngine;

namespace Diavolo
{
    public sealed class ExtraInputs : MonoBehaviour
    {
        private CharacterBody body;
        private GenericSkill block, jump;
        private GenericSkill epitaph, blood, barrage, counter, cancel, chop, smash, ultimate;
        private EntityStateMachine weapon;
        private GUIStyle moveStyle,cooldownStyle;
        private readonly System.Collections.Generic.List<RectTransform> buffRects=new System.Collections.Generic.List<RectTransform>();
        private Vector2 panelAnchor;private readonly Vector3[] healthCorners=new Vector3[4];
        private void Start()
        {
            body = GetComponent<CharacterBody>();
            foreach (EntityStateMachine machine in GetComponents<EntityStateMachine>()) if (machine.customName == "Weapon") weapon = machine;
            foreach (GenericSkill skill in GetComponents<GenericSkill>()) {
                if(skill.skillName=="BLOCK")block=skill;if(skill.skillName=="JUMP")jump=skill;
                if (skill.skillName == "EPITAPH") epitaph = skill;
                if (skill.skillName == "BLOOD") blood = skill;
                if (skill.skillName == "BARRAGE") barrage = skill;
                if (skill.skillName == "COUNTER") counter = skill;
                if (skill.skillName == "CANCEL") cancel = skill;
                if (skill.skillName == "CHOP") chop = skill;
                if (skill.skillName == "SMASH") smash = skill;
                if (skill.skillName == "ULTIMATE") ultimate = skill;
            }
        }
        private bool IsLocal() { foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList)if(user.cachedBody==body)return true;return false; }
        private bool InputAllowed() { foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList)if(user.cachedBody==body)return !user.pauseScreenController && (!user.eventSystem || !user.eventSystem.isCursorVisible);return false; }
        private void Update()
        {
            ExtraGamepad.PollFrame();
            if (!InputAllowed() || !Run.instance || !Application.isFocused || !IsLocal() || !body || !body.hasEffectiveAuthority || !body.healthComponent.alive || body.allSkillsDisabled || Time.timeScale == 0 || (MPEventSystemManager.kbmEventSystem && MPEventSystemManager.kbmEventSystem.isCursorVisible)) return;
            if ((ExtraGamepad.KeyDown(Plugin.EpitaphKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[0].Value)) && epitaph) epitaph.ExecuteIfReady();
            if ((ExtraGamepad.KeyDown(Plugin.BloodKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[1].Value)) && blood) blood.ExecuteIfReady();
            if ((ExtraGamepad.KeyDown(Plugin.BarrageKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[2].Value)) && barrage) barrage.ExecuteIfReady();
            if ((ExtraGamepad.KeyDown(Plugin.CounterKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[3].Value)) && counter) counter.ExecuteIfReady();
            if ((ExtraGamepad.KeyDown(Plugin.ChopKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[6].Value)) && chop) chop.ExecuteIfReady();
            if ((ExtraGamepad.KeyDown(Plugin.GroundSmashKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[7].Value)) && smash) smash.ExecuteIfReady();
            if ((ExtraGamepad.KeyDown(Plugin.UltimateKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[8].Value)) && ultimate) ultimate.ExecuteIfReady();
            if((ExtraGamepad.KeyDown(Plugin.BlockKey.Value)||ExtraGamepad.Pressed(Plugin.BlockPad.Value)) && block)block.ExecuteIfReady();
            if((ExtraGamepad.KeyDown(Plugin.JumpKey.Value)||ExtraGamepad.Pressed(Plugin.JumpPad.Value)) && jump)jump.ExecuteIfReady();
            ImpaleImpact impact = weapon ? weapon.state as ImpaleImpact : null;
            if ((ExtraGamepad.KeyDown(Plugin.CancelKey.Value) || ExtraGamepad.Pressed(Plugin.GamepadMoves[5].Value)) && cancel && impact != null && impact.CanPuttsunCancel) cancel.ExecuteIfReady();
        }
        private void OnGUI()
        {
            if (!IsLocal() || !body || !body.hasEffectiveAuthority || !Run.instance) return;
            Impale charging = weapon ? weapon.state as Impale : null;
            if (charging != null && body.healthComponent.alive) {
                float fill = charging.ChargeFraction;
                Rect bar = new Rect(Screen.width * .5f - 130f, Screen.height * .65f, 260f, 20f);
                Color previous = GUI.color;
                GUI.color = new Color(.08f,.02f,.025f,.9f); GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = fill >= 1f ? new Color(1f,.65f,.15f) : new Color(.7f,.04f,.08f);
                GUI.DrawTexture(new Rect(bar.x+2,bar.y+2,(bar.width-4)*fill,bar.height-4),Texture2D.whiteTexture);
                GUI.color = previous;
                GUI.Label(new Rect(bar.x,bar.y-25,320,24), fill >= 1f ? "IMPALE — FULLY CHARGED" : "IMPALE  " + Mathf.FloorToInt(fill*100f) + "%");
            }
            panelAnchor=FindPanelAnchor();
            Draw(epitaph, Plugin.EpitaphKey.Value, 0); Draw(blood, Plugin.BloodKey.Value, 1); Draw(barrage, Plugin.BarrageKey.Value, 2); Draw(counter, Plugin.CounterKey.Value, 3);
            Draw(cancel, Plugin.CancelKey.Value, 5);
            Draw(chop, Plugin.ChopKey.Value, 6); Draw(smash, Plugin.GroundSmashKey.Value, 7); Draw(ultimate, Plugin.UltimateKey.Value, 8);Draw(block,Plugin.BlockKey.Value,9);Draw(jump,Plugin.JumpKey.Value,10);
            TemporalController time = GetComponent<TemporalController>();
            if (time && time.Erasing) GUI.Label(new Rect(panelAnchor.x, panelAnchor.y-34*10*Mathf.Clamp(Screen.height/1080f,.7f,1.5f)-26,420,25), "TIME ERASED — " + Plugin.EpitaphKey.Value + ": lock fate / attack: resolve");
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
            result.y=Mathf.Max(10*34*scale+28,result.y);return result;
        }
        private void Draw(GenericSkill skill, KeyCode key, int row)
        {
            if (!skill) return;
            float scale=Mathf.Clamp(Screen.height/1080f,.7f,1.5f);
            int displayRow=row>4?row-1:row;
            float y=panelAnchor.y-(10-displayRow)*34*scale;
            float x=panelAnchor.x;
            Rect panel=new Rect(x,y,330*scale,32*scale);
            Color old=GUI.color;GUI.color=new Color(.055f,.015f,.025f,.82f);GUI.DrawTexture(panel,Texture2D.whiteTexture);
            GUI.color=skill.stock>0 ? Color.white : new Color(.5f,.5f,.5f,.85f);
            if(skill.icon)GUI.DrawTexture(new Rect(x+3*scale,y+2*scale,28*scale,28*scale),skill.icon.texture,ScaleMode.ScaleToFit);
            GUI.color=old;
            if(moveStyle==null){moveStyle=new GUIStyle(GUI.skin.label);moveStyle.alignment=TextAnchor.MiddleLeft;cooldownStyle=new GUIStyle(moveStyle);cooldownStyle.alignment=TextAnchor.MiddleRight;}
            GUIStyle style=moveStyle;style.fontSize=Mathf.RoundToInt(14*scale);
            ushort padMask=row==9?Plugin.BlockPad.Value:row==10?Plugin.JumpPad.Value:Plugin.GamepadMoves[row].Value;
            string title=Language.GetString(skill.skillNameToken).Replace("King Crimson: ","");
            GUI.Label(new Rect(x+38*scale,y,231*scale,32*scale),(ExtraGamepad.RecentlyUsed && padMask!=0 ? ExtraGamepad.Label(padMask) : key.ToString())+"  "+title,style);
            style=cooldownStyle;style.fontSize=moveStyle.fontSize;style.normal.textColor=skill.stock>0 ? new Color(.85f,.9f,.85f) : new Color(1,.45f,.45f);
            GUI.Label(new Rect(x+270*scale,y,52*scale,32*scale),skill.stock>0 ? "READY" : Mathf.CeilToInt(skill.cooldownRemaining)+"s",style);
        }
    }
}
