using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace EyesOfHeavenDio
{
    // Windows XInput (including Steam Input's Xbox emulation). Main skills stay on RoR2 input.
    public static class ExtraGamepad
    {
        [StructLayout(LayoutKind.Sequential)] private struct Pad { public ushort buttons; public byte leftTrigger,rightTrigger; public short lx,ly,rx,ry; }
        [StructLayout(LayoutKind.Sequential)] private struct State { public uint packet; public Pad pad; }
        [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")] private static extern uint Read(uint index,out State state);
        [DllImport("xinput1_3.dll",EntryPoint="XInputGetState")] private static extern uint ReadLegacy(uint index,out State state);
        private static float lastUse=-100;
        public static bool RecentlyUsed {get {Poll();return Time.unscaledTime-lastUse<10;}}
        public static bool R3Held {get {Poll();return (current&128)!=0;}}
        public static bool ModifierHeld {get {Poll();return (current&192)!=0;}}
        public static bool KeyDown(KeyCode key){return !ModifierHeld && Input.GetKeyDown(key);}
        public static bool KeyHeld(KeyCode key){return !ModifierHeld && Input.GetKey(key);}
        private static readonly ushort[] labelBits={64,128,1,2,4,8,256,512,1024,2048,4096,8192,16384,32768,16,32};
        private static readonly string[] labelNames={"L3","R3","Up","Down","Left","Right","LB","RB","LT","RT","A","B","X","Y","Start","Back"};
        public static string Label(ushort mask){string label="";for(int i=0;i<labelBits.Length;i++)if((mask&labelBits[i])!=0)label+=labelNames[i]+"+";return label.Length>0 ? label.TrimEnd('+') : mask==0 ? "Off" : "0x"+mask.ToString("X");}
        private static readonly ushort[] bindings=new ushort[6];
        private static int frame=-1; private static ushort current,previous; private static bool legacy,unavailable;
        public static void PollFrame(){Poll();}
        private static void Poll(){
            if(frame==Time.frameCount)return;frame=Time.frameCount;previous=current;current=0;
            for(int i=0;i<DioPlugin.GamepadMoves.Length;i++)bindings[i]=DioPlugin.GamepadMoves[i].Value;

            if(unavailable || !DioPlugin.GamepadEnabled.Value)return;
            try { State state; uint result=legacy ? ReadLegacy((uint)DioPlugin.GamepadIndex.Value,out state) : Read((uint)DioPlugin.GamepadIndex.Value,out state);if(result==0){current=state.pad.buttons;if(state.pad.leftTrigger>30)current|=1024;if(state.pad.rightTrigger>30)current|=2048;}if(current!=0 && current!=previous)lastUse=Time.unscaledTime; }
            catch(DllNotFoundException){if(!legacy){legacy=true;frame=-1;Poll();}else unavailable=true;}
            catch(EntryPointNotFoundException){unavailable=true;}
        }
        public static bool Held(ushort mask){Poll();return GamepadChordRules.Held(current,mask,bindings,0);}
        public static bool Pressed(ushort mask){Poll();return GamepadChordRules.Pressed(current,previous,mask,bindings,0);}
    }
}
