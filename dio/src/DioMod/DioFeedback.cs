using System;
using System.Collections.Generic;
using System.IO;
using RoR2;
using UnityEngine;
using Path = System.IO.Path;

namespace EyesOfHeavenDio
{
    internal static class DioFeedback
    {
        private sealed class PcmClip { public byte[] Data; public int Channels, Rate; }
        private static readonly Dictionary<string, PcmClip> clips = new Dictionary<string, PcmClip>();
        internal static Shader RippleShader, RingShader, TimeOverlayShader;
        internal static Texture2D StopAtlas, ResumeAtlas;
        internal static float ClipDuration(string name, float fallback)
            => clips.TryGetValue(name, out var clip) ? (float)clip.Data.Length / (clip.Channels * 2 * clip.Rate) : fallback;
        internal static string RandomTimeStopVoice()
        {
            var choices=new List<string>();
            foreach(var key in clips.Keys)if(key.StartsWith("za-warudo-voice-",StringComparison.Ordinal))choices.Add(key);
            choices.Sort(StringComparer.Ordinal);
            return choices.Count>0?choices[UnityEngine.Random.Range(0,choices.Count)]:null;
        }
        internal static void LoadAudio()
        {
            string folder = Path.Combine(Path.GetDirectoryName(typeof(DioPlugin).Assembly.Location), "Audio");
            if (!Directory.Exists(folder)) { DioPlugin.Log.LogWarning("DIO Audio folder missing."); return; }
            foreach (string file in Directory.GetFiles(folder, "*.wav"))
            {
                try
                {
                    using (var reader = new BinaryReader(File.OpenRead(file)))
                    {
                        if (new string(reader.ReadChars(4)) != "RIFF") continue;
                        reader.ReadInt32();
                        if (new string(reader.ReadChars(4)) != "WAVE") continue;
                        int channels = 0, rate = 0, bits = 0, format = 0;
                        byte[] pcm = null;
                        while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
                        {
                            string tag = new string(reader.ReadChars(4));
                            int length = reader.ReadInt32();
                            long next = reader.BaseStream.Position + length + (length & 1);
                            if (length < 0 || next > reader.BaseStream.Length) throw new InvalidDataException("Invalid WAV chunk.");
                            if (tag == "fmt ")
                            {
                                format = reader.ReadInt16(); channels = reader.ReadInt16(); rate = reader.ReadInt32();
                                reader.ReadInt32(); reader.ReadInt16(); bits = reader.ReadInt16();
                            }
                            else if (tag == "data") pcm = reader.ReadBytes(length);
                            reader.BaseStream.Position = next;
                        }
                        if (format != 1 || bits != 16 || channels < 1 || rate < 1 || pcm == null) throw new InvalidDataException("Expected PCM16 WAV.");
                        if (pcm.Length == 0 || pcm.Length % (channels * 2) != 0) throw new InvalidDataException("Invalid PCM sample alignment.");
                        string name = Path.GetFileNameWithoutExtension(file);
                        clips[name] = new PcmClip { Data = pcm, Channels = channels, Rate = rate };
                    }
                }
                catch (Exception error) { DioPlugin.Log.LogWarning("DIO audio: " + file + ": " + error.Message); }
            }
            DioPlugin.Log.LogInfo("Loaded " + clips.Count + " DIO sound clips (EOH and user-provided audio).");
        }
        internal static void LoadShaders()
        {
            string file = Path.Combine(Path.GetDirectoryName(typeof(DioPlugin).Assembly.Location), "diofeedback");
            var bundle = AssetBundle.LoadFromFile(file);
            if (!bundle) return;
            DioUi.Load(bundle);
            RippleShader = bundle.LoadAsset<Shader>("Assets/Feedback/DioRipple.shader");
            RingShader = bundle.LoadAsset<Shader>("Assets/Feedback/DioRing.shader");
            TimeOverlayShader = bundle.LoadAsset<Shader>("Assets/Feedback/DioTimeOverlay.shader");
            StopAtlas = bundle.LoadAsset<Texture2D>("Assets/Feedback/DioStopAtlas.png");
            ResumeAtlas = bundle.LoadAsset<Texture2D>("Assets/Feedback/DioResumeAtlas.png");
            DioPlugin.Log.LogInfo("Time overlay loaded: shader=" + (bool)TimeOverlayShader + " start=" + (bool)StopAtlas + " end=" + (bool)ResumeAtlas);
        }
        internal static DioAudioPlayback Sound(GameObject owner, string name, float volume)
            => DioAudioRelay.Emit(owner, name, volume, false);
        internal static DioAudioPlayback ServerSound(GameObject owner, string name, float volume)
            => DioAudioRelay.Emit(owner, name, volume, true);
        internal static bool HasClip(string name) => name != null && clips.ContainsKey(name);
        private static readonly List<DioAudioPlayback> effectStreams = new List<DioAudioPlayback>();
        internal static DioAudioPlayback PlayLocal(GameObject owner, string name, float volume)
        {
            if (!clips.TryGetValue(name, out var clip)) { DioPlugin.Log.LogWarning("Missing DIO audio: " + name); return null; }
            CharacterBody body=owner ? owner.GetComponent<CharacterBody>() : null;
            float listener=0f;
            if(body)foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList) {
                CharacterBody ear=user.cachedBody;if(!ear)continue;
                bool teammate=ear.teamComponent && body.teamComponent && ear.teamComponent.teamIndex==body.teamComponent.teamIndex;
                listener=Mathf.Max(listener,LocalModAudio.AudioMixRules.ListenerGain(ear==body,teammate,Vector3.Distance(ear.corePosition,body.corePosition),DioPlugin.PartyVolume.Value));
            }
            // Keep the owner's silent stream available to timing-sensitive moves.
            if(listener<=0f && !DioInput.IsLocal(body))return null;
            bool voice=LocalModAudio.AudioMixRules.Voice(name);
            effectStreams.RemoveAll(stream=>!stream);
            if(!voice && effectStreams.Count>=12)return null;
            var sound = new GameObject("DioSound_" + name);
            var playback = sound.AddComponent<DioAudioPlayback>();
            float category=voice?DioPlugin.VoiceVolume.Value:DioPlugin.SfxVolume.Value;
            if (playback.Begin(clip.Data, clip.Channels, clip.Rate, Mathf.Clamp01(volume * DioPlugin.SoundVolume.Value * category * listener)))
            { if(!voice)effectStreams.Add(playback);DioVoiceCaptions.Show(owner,playback,name,(float)clip.Data.Length/(clip.Channels*2*clip.Rate)); return playback; }
            UnityEngine.Object.Destroy(sound);
            return null;
        }
        internal static void TimePulse(GameObject owner, Vector3 origin, bool local, bool playSound = true)
        {
            if (playSound) Sound(owner, "time-stop", 1f);
            Rings(origin);
            if (local && RippleShader)
                foreach (var camera in Camera.allCameras)
                    if (camera.name == "Scene Camera")
                    {
                        var pulse = camera.GetComponent<DioScreenPulse>();
                        if (!pulse) pulse = camera.gameObject.AddComponent<DioScreenPulse>();
                        pulse.Trigger();
                    }
        }
        internal static void MainTimeTransition(bool ending, bool local, float seconds=15f, bool replay=true, CharacterBody owner=null)
        {
            if (!local) return;
            foreach (var camera in Camera.allCameras)
                if (camera.name == "Scene Camera")
                {
                    var pulse = camera.GetComponent<DioScreenPulse>();
                    if (!pulse) pulse = camera.gameObject.AddComponent<DioScreenPulse>();
                    pulse.TriggerMain(ending,seconds,replay,owner);
                }
        }
        internal static void ExTransition(CharacterBody owner, bool ending)
        {
            if (!owner || !owner.hasEffectiveAuthority) return;
            foreach (var rig in UnityEngine.Object.FindObjectsOfType<CameraRigController>())
                if (rig.targetBody == owner && rig.localUserViewer != null && rig.sceneCam)
                {
                    var pulse = rig.sceneCam.GetComponent<DioScreenPulse>();
                    if (!pulse) pulse = rig.sceneCam.gameObject.AddComponent<DioScreenPulse>();
                    pulse.TriggerEx(ending);
                }
        }
        internal static void Rings(Vector3 origin)
        {
            if (!RingShader) return;
            var obj = new GameObject("DioTimeRipple");
            obj.transform.position = origin;
            obj.AddComponent<DioTimeRings>();
        }
        internal static void Impact(Vector3 origin, float scale, bool drain = false)
        {
            if (!RingShader) return;
            var obj = new GameObject("DioImpact"); DioTimeWorld.MarkRollerEffect(obj); obj.transform.position = origin;
            var burst = obj.AddComponent<DioImpactBurst>(); burst.Size = scale; burst.Drain = drain;
        }
        private static float nextKnifeEffect, nextKnifeSound;
        internal static void KnifeImpact(GameObject owner,Vector3 point,float volume)
        {
            // Bound rendering/audio work independently of damage and proc counts.
            if(Time.time>=nextKnifeEffect){nextKnifeEffect=Time.time+.045f;DioStandParticles.Emit(point,true);}
            if(Time.time>=nextKnifeSound){nextKnifeSound=Time.time+.065f;if(UnityEngine.Networking.NetworkServer.active)ServerSound(owner,"knife-impact",volume);else Sound(owner,"knife-impact",volume);}
        }
    }

    public sealed class DioImpactBurst : MonoBehaviour
    {
        public float Size = 1;
        public bool Drain;
        private Material material;
        private LineRenderer[] rays;
        private float age;
        private void Start()
        {
            material = new Material(DioFeedback.RingShader);
            rays = new LineRenderer[12];
            for (int i = 0; i < rays.Length; i++)
            {
                var child = new GameObject("ImpactRay"); child.transform.SetParent(transform, false);
                var ray = rays[i] = child.AddComponent<LineRenderer>();
                ray.sharedMaterial = material; ray.useWorldSpace = false; ray.positionCount = 2;
                var direction = Quaternion.Euler(i * 137.5f, i * 73f, 0) * Vector3.forward;
                ray.SetPosition(0, direction * .15f); ray.SetPosition(1, direction * (i % 2 == 0 ? 2 : 1.2f));
                ray.startWidth = .18f; ray.endWidth = 0;
            }
        }
        private void Update()
        {
            if (DioTimeWorld.Active && !GetComponent<DioTimeStopExempt>()) return;
            age += Time.deltaTime; float t = age / .22f;
            if (t >= 1) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * Size * Mathf.Lerp(.4f, 1.5f, t);
            foreach (var ray in rays)
            {
                Color color = Drain ? new Color(.9f, .05f, .2f, 1-t) : new Color(1, .8f, .2f, 1-t);
                ray.startColor = ray.endColor = color;
            }
        }
        private void OnDestroy() { if (material) Destroy(material); }
    }

    public sealed class DioScreenPulse : MonoBehaviour
    {
        private Material material;
        private Material overlayMaterial;
        private float started = -100;
        private float overlayStarted = -100;
        private bool overlayEnding, mainGrey;
        private CharacterBody mainOwner;
        private bool trackedOwner;
        private float mainGreyEnds=float.PositiveInfinity;
        private float overlaySpeed = 1f, exGreyUntil = -100;
        public void Trigger() { started = Time.time; enabled = true; }
        public void TriggerMain(bool ending,float seconds=15f,bool replay=true,CharacterBody owner=null)
        {
            if(owner){mainOwner=owner;trackedOwner=true;}
            overlaySpeed = 1f; exGreyUntil=-100;
            mainGrey=true;mainGreyEnds=ending?Time.time+19f/24f:Time.time+Mathf.Clamp(seconds,0,15)+1f;
            if(!ending&&!replay&&enabled&&Time.time-overlayStarted>.01f)return;
            overlayStarted = Time.time; overlayEnding = ending; enabled = true;
            started = -100; // No procedural ripple/inversion on the main time stop.
        }
        public void TriggerEx(bool ending)
        {
            overlayStarted=Time.time;overlayEnding=ending;enabled=true;started=-100;
            overlaySpeed = 6f;
            exGreyUntil = ending ? Time.time + .06f : Time.time + DioSpecialMath.ExFreeze;
        }
        private void Update()
        {
            if(!Run.instance){mainGrey=false;exGreyUntil=-100;overlayStarted=-100;started=-100;}
            if(mainGrey&&!overlayEnding&&trackedOwner&&(!mainOwner||!mainOwner.healthComponent||!mainOwner.healthComponent.alive))TriggerMain(true);
            if(mainGrey&&Time.time>=mainGreyEnds){if(!overlayEnding)TriggerMain(true);else mainGrey=false;}
            // An inactive image effect still forces an extra fullscreen copy.
            if (Time.time - started > .65f && Time.time - overlayStarted >= (overlayEnding ? 19f : 20f) / (24f * overlaySpeed) && Time.time >= exGreyUntil && !mainGrey) enabled = false;
        }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            float overlayAge = Time.time - overlayStarted;
            int frame = Mathf.FloorToInt(overlayAge * 24f * overlaySpeed);
            bool showOverlay = frame >= 0 && frame < (overlayEnding ? 19 : 20);
            if ((showOverlay || mainGrey || Time.time < exGreyUntil) && DioFeedback.TimeOverlayShader)
            {
                if (!overlayMaterial) overlayMaterial = new Material(DioFeedback.TimeOverlayShader);
                var atlas = overlayEnding ? DioFeedback.ResumeAtlas : DioFeedback.StopAtlas;
                overlayMaterial.SetFloat("_Stopped", showOverlay || mainGrey || Time.time < exGreyUntil ? 1 : 0);
                overlayMaterial.SetFloat("_HasOverlay", showOverlay && atlas ? 1 : 0);
                if (showOverlay && atlas)
                {
                    overlayMaterial.SetTexture("_OverlayTex", atlas);
                    // Inset half a pixel so bilinear filtering never samples the next tile.
                    overlayMaterial.SetVector("_FrameRect", new Vector4((frame % 5) / 5f + .5f / atlas.width,
                        1f - (frame / 5 + 1) / 4f + .5f / atlas.height,
                        1f / 5f - 1f / atlas.width, 1f / 4f - 1f / atlas.height));
                }
                Graphics.Blit(source, destination, overlayMaterial);
                return;
            }
            float age = Time.time - started;
            if ((age > .65f) || !DioFeedback.RippleShader) { Graphics.Blit(source, destination); return; }
            if (!material) material = new Material(DioFeedback.RippleShader);
            material.SetFloat("_Progress", Mathf.Clamp01(age / .65f));
            material.SetFloat("_Stopped", DioTimeWorld.Active ? 1 : 0);
            Graphics.Blit(source, destination, material);
        }
        private void OnDestroy() { if (material) Destroy(material); if (overlayMaterial) Destroy(overlayMaterial); }
    }

    public sealed class DioTimeRings : MonoBehaviour
    {
        private Material material;
        private LineRenderer[] rings;
        private float age;
        private void Awake()
        {
            material = new Material(DioFeedback.RingShader);
            rings = new LineRenderer[3];
            for (int j = 0; j < rings.Length; j++)
            {
                var child = new GameObject("Ring" + j);
                child.transform.SetParent(transform, false);
                var line = rings[j] = child.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = false; line.loop = true;
                line.positionCount = 97; line.widthMultiplier = .15f;
                for (int i = 0; i < 97; i++)
                {
                    float angle = i * Mathf.PI * 2 / 97;
                    var point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                    if (j == 1) point = Quaternion.Euler(90, 0, 0) * point;
                    if (j == 2) point = Quaternion.Euler(0, 90, 0) * point;
                    line.SetPosition(i, point);
                }
            }
        }
        private void Update()
        {
            age += Time.deltaTime;
            float t = age / .6f;
            if (t >= 1) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * Mathf.Lerp(.3f, 12f, t);
            for (int i = 0; i < rings.Length; i++)
            {
                var color = i == 1 ? new Color(.65f, .2f, 1f, 1 - t) : new Color(1f, .8f, .15f, 1 - t);
                rings[i].startColor = rings[i].endColor = color;
            }
        }
        private void OnDestroy() { if (material) Destroy(material); }
    }
}
