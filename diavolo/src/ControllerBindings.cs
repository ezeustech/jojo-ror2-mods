using System;
using System.Collections.Generic;
using BepInEx.Configuration;
namespace Diavolo
{
    public enum ControllerBinding
    {
        Off=0, DPad_Up=1, DPad_Down=2, DPad_Left=4, DPad_Right=8,
        L3=64, R3=128, LB=256, RB=512, LT=1024, RT=2048, A=4096, B=8192, X=16384, Y=32768,
        R3_DPad_Up=129, R3_DPad_Down=130, R3_DPad_Left=132, R3_DPad_Right=136,
        R3_A=4224, R3_B=8320, R3_X=16512, R3_Y=32896, R3_LB=384, R3_RB=640, R3_LT=1152, R3_RT=2176,
        L3_DPad_Up=65, L3_DPad_Down=66, L3_DPad_Left=68, L3_DPad_Right=72,
        L3_A=4160, L3_LB=320, L3_RB=576, L3_LT=1088, L3_RT=2112,
        L3_R3=192, Custom_config_mask=65535
    }
    internal static class ControllerBindings
    {
        internal static readonly List<ConfigEntry<ControllerBinding>> Choices=new List<ConfigEntry<ControllerBinding>>();
        internal static void Initialize(ConfigFile config) {
            int[] indices={0,1,2,3,5,6,7,8};
            string[] names={"Epitaph","Blood Throw","Barrage (hold)","Counter","Puttsun Cancel","Downward Chop","Ground Smash","Final Verdict"};
            for(int i=0;i<indices.Length;i++)Bind(config,names[i],Plugin.GamepadMoves[indices[i]]);
            Bind(config,"Block (hold)",Plugin.BlockPad);Bind(config,"Directional Stand Jump",Plugin.JumpPad);
        }
        private static ControllerBinding FromMask(ushort mask) {
            return Enum.IsDefined(typeof(ControllerBinding),(int)mask) ? (ControllerBinding)mask : ControllerBinding.Custom_config_mask;
        }
        private static void Bind(ConfigFile config,string name,ConfigEntry<ushort> legacy) {
            var entry=config.Bind("Controller Bindings",name,FromMask(legacy.Value),
                "Choose a button or chord. R3 = right stick click; L3 = left stick click. Hold R3 before pressing the trigger/button. While R3 is held, normal skills and bindings without R3 are blocked. Release the trigger/button before using it normally again. Custom_config_mask preserves your numeric Gamepad binding.");
            bool syncing=false;
            entry.SettingChanged+=delegate {
                if(syncing || entry.Value==ControllerBinding.Custom_config_mask)return;
                syncing=true;legacy.Value=(ushort)entry.Value;syncing=false;
            };
            legacy.SettingChanged+=delegate {
                if(syncing)return;
                syncing=true;entry.Value=FromMask(legacy.Value);syncing=false;
            };
            if(entry.Value!=ControllerBinding.Custom_config_mask)legacy.Value=(ushort)entry.Value;
            Choices.Add(entry);
        }
    }
}
