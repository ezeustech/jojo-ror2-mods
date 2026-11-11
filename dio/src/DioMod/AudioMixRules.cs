using System;
namespace LocalModAudio
{
    internal static class AudioMixRules
    {
        internal static float ListenerGain(bool owner, bool teammate, float distance, float partyVolume)
        {
            if (owner) return 1f;
            if (!teammate || float.IsNaN(distance) || distance >= 80f) return 0f;
            float fade = Math.Max(0f, Math.Min(1f, (80f - distance) / 65f));
            return Math.Max(0f, Math.Min(1f, partyVolume)) * fade * fade;
        }
        internal static bool Voice(string cue)
        {
            return cue != null && (cue.IndexOf("voice", StringComparison.OrdinalIgnoreCase) >= 0 || cue == "barrage" || cue == "time-stop-r" || cue == "time-stop-main" || cue == "time-resume");
        }
    }
    internal sealed class AudioSequence
    {
        private bool initialized;
        private uint last;
        internal bool Accept(uint sequence)
        {
            if (sequence == 0 || (initialized && unchecked((int)(sequence - last)) <= 0)) return false;
            initialized = true; last = sequence; return true;
        }
    }
}
