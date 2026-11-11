using System.Collections.Generic;
namespace Diavolo
{
    public static class VoiceCaptionText
    {
        private static readonly Dictionary<string,string> lines=new Dictionary<string,string>{
            {"death_voice","[Dying cry]"},{"chain1_voice","[Grunts]"},{"chain2_voice","[Grunts]"},{"chain3_voice","[Battle cry]"},
            {"chop_voice","Your life is mine!"},{"charge_voice","Now, I sever it!"},
            {"impale_voice","This is the end!"},{"barrage_voice","[Battle cry]"},{"blood_voice","[Battle cry]"},
            {"counter_voice","The pinnacle of truth lies in my ability!"},
            {"grab_voice","I will make sure you disappear."},{"ultimate_voice","I will make sure you disappear."},
            {"epitaph_voice","Epitaph!"},{"skip_voice","I erased half a second of time."},
            {"erase_voice","King Crimson!"},{"resume_voice","Time begins moving again!"},
            {"cancel_voice","There is no longer any problem."},{"groundsmash_voice","Now, I sever it!"}
        };
        public static string Get(string cue){string text;return lines.TryGetValue(cue,out text) ? text : null;}
    }
}
