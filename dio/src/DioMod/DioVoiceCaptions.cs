using System.Collections.Generic;
using RoR2;
using UnityEngine;
namespace EyesOfHeavenDio
{
    public sealed class DioVoiceCaptions : MonoBehaviour
    {
        private sealed class Line { public DioAudioPlayback audio;public CharacterBody owner;public string cue;public float duration; }
        private static DioVoiceCaptions instance;
        private readonly List<Line> lines=new List<Line>();
        private GUIStyle style;
        private void Awake(){instance=this;}
        internal static string Text(string cue,float seconds)
        {
            if(cue.StartsWith("za-warudo-voice-"))return "The World!";
            if(cue.StartsWith("voice-muda"))return "Useless, useless, useless!";
            if(cue=="voice-wryy")return "WRYYY!";
            if(cue=="shift-punch-voice")return "Die!";
            if(cue=="barrage")return "Useless, useless, useless!";
            // Blood drain and Road Roller translations were supplied by the user.
            if(cue=="blood-drain-voice")return "This is the greatest high!";
            if(cue=="road-roller-voice")
            {
                if(seconds<2.5f)return "It's a road roller!";
                if(seconds<DioSpecialMath.RollerVoicePunchStart)return "It's too late! You can't escape!";
                if(seconds<DioSpecialMath.RollerVoiceFinisher-.55f)return "Useless! Useless! Useless!";
                return "I'll smash you flat!";
            }
            if(cue=="death-voice")return "[DIO's dying cries]";
            return null;
        }
        internal static void Show(GameObject owner,DioAudioPlayback audio,string cue,float duration)
        {
            if(!instance||!DioPlugin.Subtitles.Value||!audio||Text(cue,0)==null)return;
            var body=owner?owner.GetComponent<CharacterBody>():null;
            if(!DioInput.IsLocal(body))return;
            instance.lines.Add(new Line{owner=body,audio=audio,cue=cue,duration=duration});
            if(instance.lines.Count>6)instance.lines.RemoveAt(0);
        }
        private void Update()
        {
            if(!Run.instance||!DioPlugin.Subtitles.Value){lines.Clear();return;}
            for(int i=lines.Count-1;i>=0;i--)
            {
                var line=lines[i];
                if(!line.audio||line.audio.PlaybackSeconds>line.duration+.2f||
                    (line.cue!="death-voice"&&(!line.owner||!line.owner.healthComponent||!line.owner.healthComponent.alive)))lines.RemoveAt(i);
            }
        }
        private void OnGUI()
        {
            if(!DioPlugin.Subtitles.Value||!Run.instance||lines.Count==0)return;
            if(style==null){style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true};style.normal.textColor=Color.white;}
            float scale=Mathf.Clamp(Screen.height/1080f,.7f,1.5f);style.fontSize=Mathf.RoundToInt(22*scale);
            int oldDepth=GUI.depth;GUI.depth=-1100;Color oldColor=GUI.color;
            int count=Mathf.Min(2,lines.Count);float width=Mathf.Min(800*scale,Screen.width-32);
            for(int i=0;i<count;i++)
            {
                var line=lines[lines.Count-count+i];if(!line.audio)continue;
                var rect=new Rect((Screen.width-width)*.5f,Screen.height*.8f+i*48*scale,width,46*scale);
                GUI.color=new Color(0,0,0,.72f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(rect,Text(line.cue,line.audio.PlaybackSeconds),style);
            }
            GUI.color=oldColor;GUI.depth=oldDepth;
        }
        private void OnDestroy(){lines.Clear();if(instance==this)instance=null;}
    }
    public sealed class DioAttackVoice : MonoBehaviour
    {
        private DioAudioPlayback voice;private float next;
        public void Play(bool finisher=false)
        {
            if(Time.time<next||voice)return;
            string cue=finisher?"voice-wryy":Random.value<.5f?"voice-muda-01":"voice-muda-02";
            voice=DioFeedback.Sound(gameObject,cue,.65f);next=Time.time+DioFeedback.ClipDuration(cue,2)+.25f;
        }
        private void OnDestroy(){if(voice)Destroy(voice.gameObject);}
    }
}
