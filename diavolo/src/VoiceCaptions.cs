using System.Collections.Generic;
using UnityEngine;
using RoR2;
namespace Diavolo
{
    public sealed class VoiceCaptions : MonoBehaviour
    {
        private sealed class Line {public GameObject sound;public CharacterBody owner;public string text;public float started,length;public bool death;}
        private static VoiceCaptions instance;private readonly List<Line> lines=new List<Line>();private GUIStyle style;
        private void Awake(){instance=this;}
        public static void Show(CharacterBody body,GameObject sound,string cue,float length){
            if(!instance || !Plugin.Subtitles.Value || !sound)return;string text=VoiceCaptionText.Get(cue);if(text==null)return;
            if(instance.lines.Count>0 && instance.lines[instance.lines.Count-1].text==text)instance.lines.RemoveAt(instance.lines.Count-1);
            instance.lines.Add(new Line{sound=sound,owner=body,text=text,started=Time.time,length=length,death=cue=="death_voice"});
            if(instance.lines.Count>8)instance.lines.RemoveAt(0);
        }
        private void Update(){
            if(!Plugin.Subtitles.Value || !Run.instance){lines.Clear();return;}
            for(int i=lines.Count-1;i>=0;i--){var line=lines[i];float age=Time.time-line.started;
                if((!line.death && (!line.owner || !line.owner.healthComponent || !line.owner.healthComponent.alive)) || age>Mathf.Max(1.1f,line.length+.35f) || (!line.sound && age<line.length-.1f))lines.RemoveAt(i);
            }
        }
        private void OnGUI(){
            if(!Plugin.Subtitles.Value || !Run.instance || lines.Count==0)return;
            if(style==null){style=new GUIStyle(GUI.skin.label);style.alignment=TextAnchor.MiddleCenter;style.wordWrap=true;style.normal.textColor=Color.white;}
            float scale=Mathf.Clamp(Screen.height/1080f,.7f,1.5f);style.fontSize=Mathf.RoundToInt(22*scale);int rows=Mathf.Min(lines.Count,3);
            float width=Mathf.Min(760*scale,Screen.width-32),height=42*scale;int oldDepth=GUI.depth;GUI.depth=-1100;
            Color previous=GUI.color;
            for(int i=0;i<rows;i++){
                Rect rect=new Rect((Screen.width-width)*.5f,Screen.height*.80f+i*height,width,height-2*scale);
                GUI.color=new Color(0,0,0,.7f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(rect,lines[lines.Count-rows+i].text,style);
            }
            GUI.color=previous;GUI.depth=oldDepth;
        }
        private void OnDestroy(){lines.Clear();if(instance==this)instance=null;}
    }
}
