using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Path = System.IO.Path;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
namespace Diavolo
{
    public sealed class TimeOverlay : MonoBehaviour
    {
        private static TimeOverlay instance;
        private readonly List<Texture2D> frames = new List<Texture2D>();
        private readonly HashSet<GameObject> tinted = new HashSet<GameObject>();
        private float fps = 23.976024f, began = -100f, speed = 1f;
        private bool ready;
        private float nextReconcile;
        private void Awake() { instance = this; }
        private IEnumerator Start()
        {
            string directory = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "TimeOverlay");
            if (!Directory.Exists(directory)) yield break;
            string timing = Path.Combine(directory,"timing.txt"); float parsed;
            if (File.Exists(timing) && float.TryParse(File.ReadAllText(timing),NumberStyles.Float,CultureInfo.InvariantCulture,out parsed) && parsed > 0) fps=parsed;
            string[] paths=Directory.GetFiles(directory,"*.png"); Array.Sort(paths,StringComparer.Ordinal);
            foreach (string path in paths) {
                Texture2D frame=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if (ImageConversion.LoadImage(frame,File.ReadAllBytes(path),false)) {
                    Color32[] pixels=frame.GetPixels32();
                    for(int i=0;i<pixels.Length;i++)OverlayChroma.Clean(ref pixels[i].r,ref pixels[i].g,ref pixels[i].b,ref pixels[i].a);
                    frame.SetPixels32(pixels);frame.Apply(false,false);
                    // 20 full RGBA frames otherwise occupy about 40 MB. DXT5
                    // preserves alpha while dropping the resident GPU cost 4x.
                    frame.Compress(false);frame.Apply(false,true);
                    frames.Add(frame);
                } else Destroy(frame);
                yield return null;
            }
            ready=frames.Count>0;
        }
        // 0: erasure tint; 1: return + video; 2: instant skip video.
        public static void Send(CharacterBody owner, byte operation)
        {
            if (!NetworkServer.active || !owner || owner.GetComponent<TimeEraseDecoy>()) return;
            if (operation == 0 || operation == 2 || operation == 3) SkipHealing.Apply(owner);
            if (NetworkClient.active) Receive(owner.gameObject,operation);
            new TimeOverlayMessage(owner.gameObject,operation).Send(NetworkDestination.Clients);
        }
        internal static void Receive(GameObject owner, byte operation)
        {
            if (!instance || !owner) return;
            CharacterBody source=owner.GetComponent<CharacterBody>();
            if (!source || !source.teamComponent) return;
            bool viewer=false, own=false;
            foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList) {
                CharacterBody local=user.cachedBody;
                if (!local || !local.teamComponent || local.teamComponent.teamIndex != source.teamComponent.teamIndex) continue;
                viewer=true; if (local==source) own=true;
            }
            if (!viewer) return;
            if (operation==0) instance.tinted.Add(owner);
            else if(operation==1) instance.tinted.Remove(owner);
            instance.speed=operation==3 ? (own ? 1.6f : 2.4f) : own ? 1f : 1.5f;
            instance.began=Time.unscaledTime;
        }
        private void Update()
        {
            if(!Run.instance){tinted.Clear();return;}
            if(Time.unscaledTime<nextReconcile)return;
            nextReconcile=Time.unscaledTime+.1f;
            // Recover from message ordering and joining an erasure already in progress.
            // One player's return/instant skip must not cancel another player's tint.
            tinted.Clear();
            foreach(CharacterBody source in CharacterBody.readOnlyInstancesList) {
                if(!source || !source.teamComponent || !source.healthComponent || !source.healthComponent.alive || source.GetComponent<TimeEraseDecoy>())continue;
                var temporal=source.GetComponent<TemporalController>();if(!temporal || !temporal.Erasing)continue;
                foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList) {
                    var local=user.cachedBody;
                    if(local && local.teamComponent && local.teamComponent.teamIndex==source.teamComponent.teamIndex){tinted.Add(source.gameObject);break;}
                }
            }
        }
        private void OnGUI()
        {
            if (!Run.instance) { tinted.Clear(); began=-100f; return; }
            tinted.RemoveWhere(owner => !owner || !owner.GetComponent<TemporalController>() || !owner.GetComponent<TemporalController>().Erasing);
            Color previous=GUI.color; int depth=GUI.depth; GUI.depth=-1000;
            int index=Mathf.FloorToInt((Time.unscaledTime-began)*fps*speed);
            bool cosmic=false; foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList) { var effect=user.cachedBody ? user.cachedBody.GetComponent<CosmicErasure>() : null; if(effect && effect.Active)cosmic=true; }
            if (tinted.Count>0 && !cosmic && (Plugin.LowVfx.Value || index>=frames.Count)) { GUI.color=new Color(.55f,.005f,.015f,.22f); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); }
            if (ready && index>=0 && index<frames.Count) {
                GUI.color=Color.white;
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),frames[index],ScaleMode.StretchToFill,true);
            }
            GUI.color=previous; GUI.depth=depth;
        }
        private void OnDestroy() { foreach (Texture2D frame in frames) if (frame) Destroy(frame); if (instance==this) instance=null; }
    }
    public sealed class TimeOverlayMessage : INetMessage
    {
        private GameObject owner; private byte operation;
        public TimeOverlayMessage() { }
        public TimeOverlayMessage(GameObject owner,byte operation) { this.owner=owner; this.operation=operation; }
        public void Serialize(NetworkWriter writer) { writer.Write(owner); writer.Write(operation); }
        public void Deserialize(NetworkReader reader) { owner=reader.ReadGameObject(); operation=reader.ReadByte(); }
        public void OnReceived() { if (!NetworkServer.active && operation<=3) TimeOverlay.Receive(owner,operation); }
    }
}
