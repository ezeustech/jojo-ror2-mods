using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using Diavolo;
namespace Diavolo {
    internal static class Plugin {
        internal static ConfigEntry<ushort>[] GamepadMoves;
        internal static ConfigEntry<ushort> BlockPad,JumpPad;
    }
}
public static class ControllerBindingsTests {
    public static void Main(){
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){
            string name=new AssemblyName(e.Name).Name+".dll";
            string[] folders={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Thunderstore Mod Manager\DataFolder\RiskOfRain2\profiles\Default\BepInEx\core"),System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("ROR2_GAME_DIR") ?? string.Empty, "Risk of Rain 2_Data/Managed")};
            foreach(string folder in folders){string p=Path.Combine(folder,name);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;
        };
        InitializePaths();Run();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitializePaths(){
        typeof(BepInEx.Paths).GetMethod("SetExecutablePath",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Path.GetFullPath("tests/ControllerBindingsTests.exe"),Path.GetFullPath("tests/bepinex-validation"),System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("ROR2_GAME_DIR") ?? string.Empty, "Risk of Rain 2_Data/Managed"),new string[0]});
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(){
        string path=Path.GetFullPath(@"tests\controller-binding-test.cfg");
        if(File.Exists(path))File.Delete(path);
        var cfg=new ConfigFile(path,false);ushort[] masks={1,1152,2176,640,136,132,384,130,192};
        Plugin.GamepadMoves=new ConfigEntry<ushort>[9];
        for(int i=0;i<9;i++)Plugin.GamepadMoves[i]=cfg.Bind("Gamepad","Move"+i,masks[i],"");
        Plugin.BlockPad=cfg.Bind<ushort>("Gamepad","Block",136,"");Plugin.JumpPad=cfg.Bind<ushort>("Gamepad","Jump",4224,"");
        Plugin.GamepadMoves[0].Value=65;Plugin.GamepadMoves[1].Value=768;
        ControllerBindings.Initialize(cfg);
        if(ControllerBindings.Choices.Count!=10)throw new Exception("Missing binding choices");
        if(ControllerBindings.Choices[0].Value!=ControllerBinding.L3_DPad_Up || Plugin.GamepadMoves[0].Value!=65)throw new Exception("Existing custom binding lost");
        if(ControllerBindings.Choices[1].Value!=ControllerBinding.Custom_config_mask || Plugin.GamepadMoves[1].Value!=768)throw new Exception("Custom mask overwritten");
        ControllerBindings.Choices[0].Value=ControllerBinding.R3_X;
        if(Plugin.GamepadMoves[0].Value!=16512)throw new Exception("UI binding not applied to runtime");
        Plugin.GamepadMoves[0].Value=512;
        if(ControllerBindings.Choices[0].Value!=ControllerBinding.RB)throw new Exception("Config change not reflected in UI");
        ControllerBindings.Choices[8].Value=ControllerBinding.Off;
        if(Plugin.BlockPad.Value!=0)throw new Exception("Cannot disable binding");
        cfg.Save();
        var reload=new ConfigFile(path,false);
        if(reload.Bind("Controller Bindings","Epitaph",ControllerBinding.Off,"").Value!=ControllerBinding.RB)throw new Exception("Binding not persisted");
        File.Delete(path);Console.WriteLine("PASS: 7 controller configuration/migration checks");
    }
}
