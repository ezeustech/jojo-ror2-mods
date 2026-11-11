using System.Collections.Generic;
using System.IO;
using Path=System.IO.Path;
using RoR2;
using UnityEngine;
using R2API.Networking.Interfaces;

namespace Diavolo
{
    internal static class DiavoloAudio
    {
        private sealed class Clip { public byte[] Data; public int Channels, Rate; }
        private static readonly PcmCache scaled = new PcmCache(24L*1024*1024);
        private static readonly List<GameObject> effectStreams = new List<GameObject>();
        public static float Duration(string name,float fallback=1f){Clip clip=LoadClip(name);return clip==null?fallback:(float)clip.Data.Length/(clip.Rate*clip.Channels*2);}
        private static readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        public static void Play(CharacterBody body,string name){Send(body,name,0);}
        public static void Impact(CharacterBody body,string name){Send(body,name,1);}
        public static void Layered(CharacterBody body,string name){Send(body,name,2);}
        public static void StopSfx(CharacterBody body){Send(body,"",3);}
        public static void EraseBass(CharacterBody body,bool play){Send(body,"",play?(byte)4:(byte)5);}
        private static void Send(CharacterBody body,string name,byte op){
            if(!UnityEngine.Networking.NetworkServer.active || !body || body.GetComponent<TimeEraseDecoy>())return;
            var channels=Channels(body);uint sequence=++channels.NextSequence;
            if(UnityEngine.Networking.NetworkClient.active)Receive(body,name,op);
            new AudioCueMessage(body.gameObject,name,op,sequence).Send(R2API.Networking.NetworkDestination.Clients);
        }
        private static DiavoloAudioChannels Channels(CharacterBody body){return body.GetComponent<DiavoloAudioChannels>()??body.gameObject.AddComponent<DiavoloAudioChannels>();}
        internal static void ReceiveNetwork(CharacterBody body,string name,byte op,uint sequence){if(body && Channels(body).Received.Accept(sequence))Receive(body,name,op);}
        internal static void Receive(CharacterBody body,string name,byte op){
            if(!body || body.GetComponent<TimeEraseDecoy>())return;
            float gain=0f;
            foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList){CharacterBody ear=user.cachedBody;if(!ear)continue;bool party=ear.teamComponent && body.teamComponent && ear.teamComponent.teamIndex==body.teamComponent.teamIndex;gain=Mathf.Max(gain,LocalModAudio.AudioMixRules.ListenerGain(ear==body,party,Vector3.Distance(ear.corePosition,body.corePosition),Plugin.PartyVolume.Value));}
            var channels=Channels(body);
            if(op==3){if(channels.MoveSfx)Object.Destroy(channels.MoveSfx);return;}
            if(op==4 || op==5){if(channels.EraseBass)Object.Destroy(channels.EraseBass);if(op==4)channels.EraseBass=PlayClip(body,"erase_bass_sfx",Plugin.SfxVolume.Value*gain);return;}
            if(op==0){if(channels.Voice)Object.Destroy(channels.Voice);if(channels.MoveSfx)Object.Destroy(channels.MoveSfx);channels.Voice=PlayClip(body,name+"_voice",Plugin.VoiceVolume.Value*gain);channels.MoveSfx=PlayClip(body,name+"_sfx",Plugin.SfxVolume.Value*gain);}
            else {if(op==2)PlayClip(body,name+"_voice",Plugin.VoiceVolume.Value*gain);PlayClip(body,name+"_sfx",Plugin.SfxVolume.Value*gain);}
        }
        public static System.Collections.IEnumerator Warmup(){
            string directory=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location),"Audio");
            if(Directory.Exists(directory))foreach(string file in Directory.GetFiles(directory,"*.wav")){string name=Path.GetFileNameWithoutExtension(file);Clip clip=LoadClip(name);if(clip!=null){float gain=name.EndsWith("_voice")?Plugin.VoiceVolume.Value:Plugin.SfxVolume.Value;Scaled(name,clip.Data,gain);Scaled(name,clip.Data,gain*Plugin.PartyVolume.Value);}yield return null;}
        }
        private static Clip LoadClip(string name)
        {
            Clip clip;
            if (!clips.TryGetValue(name, out clip)) {
                string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "Audio", name + ".wav");
                if (!File.Exists(path)) return null;
                clip = new Clip();
                using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
                    if (new string(reader.ReadChars(4)) != "RIFF") return null;
                    reader.ReadInt32(); if (new string(reader.ReadChars(4)) != "WAVE") return null;
                    while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length) {
                        string id = new string(reader.ReadChars(4)); int size = reader.ReadInt32(); long end = reader.BaseStream.Position + size;
                        if (size < 0 || end > reader.BaseStream.Length) return null;
                        if (id == "fmt ") { if (reader.ReadInt16() != 1) return null; clip.Channels = reader.ReadInt16(); clip.Rate = reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt16(); if (reader.ReadInt16() != 16) return null; }
                        if (id == "data") clip.Data = reader.ReadBytes(size);
                        reader.BaseStream.Position = end + (size & 1);
                    }
                }
                if (clip.Data == null || clip.Channels < 1 || clip.Rate < 1) return null;
                clips[name] = clip;
            }
            return clip;
        }
        private static byte[] Scaled(string name,byte[] pcm,float volume){
            volume=Mathf.Clamp01(volume);int gain=Mathf.RoundToInt(volume*64);string key=name+":"+gain;byte[] result;
            if(scaled.TryGet(key,out result))return result;result=new byte[pcm.Length];
            for(int i=0;i+1<pcm.Length;i+=2){short v=(short)(pcm[i]|pcm[i+1]<<8);short output=(short)Mathf.Clamp(Mathf.RoundToInt(v*gain/64f),short.MinValue,short.MaxValue);result[i]=(byte)output;result[i+1]=(byte)(output>>8);}
            scaled.Put(key,result);return result;
        }
        private static GameObject PlayClip(CharacterBody body,string name,float volume){
            if(volume<=0f)return null;
            bool voice=LocalModAudio.AudioMixRules.Voice(name);
            effectStreams.RemoveAll(stream=>!stream);if(!voice && effectStreams.Count>=12)return null;
            Clip clip=LoadClip(name);if(clip==null)return null;
            GameObject sound = new GameObject("Diavolo " + name);
            // Independent lifetime lets death audio finish after the body is destroyed.
            if (!sound.AddComponent<DiavoloAudioPlayback>().Begin(Scaled(name,clip.Data,volume), clip.Channels, clip.Rate, 1f)) { Object.Destroy(sound); return null; }
            if(!voice)effectStreams.Add(sound);
            VoiceCaptions.Show(body,sound,name,(float)clip.Data.Length/(clip.Channels*clip.Rate*2));
            return sound;
        }
    }
    public sealed class DiavoloAudioChannels : MonoBehaviour {
        public GameObject Voice, MoveSfx, EraseBass;
        internal uint NextSequence;
        internal readonly LocalModAudio.AudioSequence Received=new LocalModAudio.AudioSequence();
        private void OnDestroy(){if(Voice)Destroy(Voice);if(MoveSfx)Destroy(MoveSfx);if(EraseBass)Destroy(EraseBass);}
    }
}
