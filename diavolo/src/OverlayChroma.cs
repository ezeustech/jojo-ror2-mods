using System;
namespace Diavolo
{
    internal static class OverlayChroma
    {
        // Key residual green, including compression fringes, then despill the
        // retained RGB. Existing transparency and red/blue effect pixels stay.
        internal static void Clean(ref byte r,ref byte g,ref byte b,ref byte a)
        {
            if(a==0){r=g=b=0;return;}
            int neutral=Math.Max(r,b),excess=g-neutral;
            if(excess<=0)return;
            float key=Math.Max(0f,Math.Min(1f,(excess-5f)/65f));
            key=key*key*(3f-2f*key);
            a=(byte)Math.Round(a*(1f-key));
            g=(byte)neutral;
            if(a==0)r=g=b=0;
        }
    }
}
