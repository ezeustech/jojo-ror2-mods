using System.Collections.Generic;
using LocalModAudio;
using RoR2;
using R2API.Networking;
using R2API.Networking.Interfaces;
using UnityEngine;
using UnityEngine.Networking;

namespace EyesOfHeavenDio
{
    internal sealed class DioAudioRelay : MonoBehaviour
    {
        private uint nextOwner, nextServer;
        private readonly AudioSequence ownerSequence = new AudioSequence(), serverSequence = new AudioSequence();
        private readonly Dictionary<uint, DioAudioPlayback> playing = new Dictionary<uint, DioAudioPlayback>();
        private readonly HashSet<uint> persistent = new HashSet<uint>();
        private static DioAudioRelay Of(GameObject owner) { return owner.GetComponent<DioAudioRelay>() ?? owner.AddComponent<DioAudioRelay>(); }

        internal static DioAudioPlayback Emit(GameObject owner, string cue, float gain, bool serverEvent)
        {
            CharacterBody body = owner ? owner.GetComponent<CharacterBody>() : null;
            if (!body || !DioFeedback.HasClip(cue)) return null;
            if (serverEvent ? !NetworkServer.active : !body.hasEffectiveAuthority) return null;
            DioAudioRelay relay = Of(owner);
            uint sequence = serverEvent ? ++relay.nextServer : ++relay.nextOwner;
            var message = new DioSoundMessage(owner, cue, Mathf.Clamp01(gain), sequence, 0, serverEvent, NetworkServer.active);
            DioAudioPlayback playback;
            relay.Receive(message, out playback);
            if (NetworkServer.active) message.Send(NetworkDestination.Clients);
            else if (NetworkClient.active) message.Send(NetworkDestination.Server);
            if (playback) playback.Stopped += () => {
                if (!owner || !relay) return;
                var stop = new DioSoundMessage(owner, cue, 0f, sequence, 1, serverEvent, NetworkServer.active);
                if (NetworkServer.active) stop.Send(NetworkDestination.Clients);
                else if (NetworkClient.active) stop.Send(NetworkDestination.Server);
            };
            return playback;
        }
        private bool Receive(DioSoundMessage message, out DioAudioPlayback playback)
        {
            playback = null;
            uint key = message.Sequence | (message.ServerEvent ? 0x80000000u : 0u);
            if (message.Operation == 1) {
                if (playing.TryGetValue(key, out playback) && playback) Destroy(playback.gameObject);
                playing.Remove(key); playback=null; return true;
            }
            if (!(message.ServerEvent ? serverSequence : ownerSequence).Accept(message.Sequence)) return false;
            if (!NetworkClient.active) return true; // Dedicated hosts relay without opening an audio device.
            playback = DioFeedback.PlayLocal(gameObject, message.Cue, message.Gain);
            if (playback) {
                playing[key] = playback;
                if(message.Cue=="death-voice")persistent.Add(key);
                playback.Stopped += () => { if (this) playing.Remove(key); };
            }
            return true;
        }
        private void OnDestroy()
        {
            foreach(var entry in new Dictionary<uint,DioAudioPlayback>(playing))
                if(entry.Value && !persistent.Contains(entry.Key))Destroy(entry.Value.gameObject);
            playing.Clear();
        }
        internal static void Received(DioSoundMessage message)
        {
            if (!message.Owner || !message.Owner.GetComponent<DioVisualDriver>() || !DioFeedback.HasClip(message.Cue) ||
                message.Operation > 1 || message.Sequence == 0 || message.Sequence >= 0x80000000u ||
                float.IsNaN(message.Gain) || float.IsInfinity(message.Gain)) return;
            DioAudioRelay relay = Of(message.Owner);
            DioAudioPlayback ignored;
            if (NetworkServer.active) {
                if (message.Relayed || message.ServerEvent) return;
                if (!relay.Receive(message, out ignored)) return;
                message.Relayed = true;
                message.Send(NetworkDestination.Clients);
            } else if (message.Relayed) {
                CharacterBody body = message.Owner.GetComponent<CharacterBody>();
                // The owning client already played its predicted cue and owns its clock.
                if (!message.ServerEvent && DioInput.IsLocal(body)) return;
                relay.Receive(message, out ignored);
            }
        }
    }
    public sealed class DioSoundMessage : INetMessage
    {
        internal GameObject Owner;
        internal string Cue;
        internal float Gain;
        internal uint Sequence;
        internal byte Operation;
        internal bool ServerEvent, Relayed;
        public DioSoundMessage() { }
        internal DioSoundMessage(GameObject owner,string cue,float gain,uint sequence,byte operation,bool serverEvent,bool relayed)
        { Owner=owner;Cue=cue;Gain=gain;Sequence=sequence;Operation=operation;ServerEvent=serverEvent;Relayed=relayed; }
        public void Serialize(NetworkWriter writer)
        { writer.Write(Owner);writer.Write(Cue);writer.Write(Gain);writer.Write(Sequence);writer.Write(Operation);writer.Write(ServerEvent);writer.Write(Relayed); }
        public void Deserialize(NetworkReader reader)
        { Owner=reader.ReadGameObject();Cue=reader.ReadString();Gain=reader.ReadSingle();Sequence=reader.ReadUInt32();Operation=reader.ReadByte();ServerEvent=reader.ReadBoolean();Relayed=reader.ReadBoolean(); }
        public void OnReceived() { DioAudioRelay.Received(this); }
    }
}
