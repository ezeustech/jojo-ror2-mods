using System.Runtime.CompilerServices;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
namespace Diavolo
{
    internal static class DiavoloModOptions
    {
        // Isolate the optional assembly so the survivor still loads without its UI plugin.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Register() {
            ModSettingsManager.SetModDescription("Low VFX keeps the stage, video overlay and steady red erasure tint. Epitaph: D-pad Up. Hold R3 (right stick click): RT Barrage, LT Blood, LB Chop, RB Counter. Normal skills are blocked while R3 is held; release the trigger/button before using it normally. Rebind below. Main skills use Settings > Controls.","local.diavolo.survivor","Diavolo");
            Add(new CheckBoxOption(Plugin.LowVfx));
            Add(new CheckBoxOption(Plugin.CosmicWorld,new CheckBoxConfig { checkIfDisabled=()=>Plugin.LowVfx.Value }));
            Add(new CheckBoxOption(Plugin.GamepadEnabled));
            Add(new IntSliderOption(Plugin.GamepadIndex,new IntSliderConfig { min=0,max=3 }));
            foreach(var binding in ControllerBindings.Choices)Add(new ChoiceOption(binding));
            Add(new CheckBoxOption(Plugin.Subtitles));
            Add(new SliderOption(Plugin.VoiceVolume,new SliderConfig { min=0,max=1 }));
            Add(new SliderOption(Plugin.SfxVolume,new SliderConfig { min=0,max=1 }));
            Add(new SliderOption(Plugin.PartyVolume,new SliderConfig { min=0,max=1 }));
            Add(new CheckBoxOption(Plugin.SmoothPoses));
        }
        private static void Add(BaseOption option){ModSettingsManager.AddOption(option,"local.diavolo.survivor","Diavolo");}
    }
}
