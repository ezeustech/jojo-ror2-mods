using System;
using System.IO;
using System.Reflection;
public static class ValidateNativeGate253 {
    static int checks;
    static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
    public static void Run(){
        string game=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("ROR2_GAME_DIR") ?? throw new System.InvalidOperationException("Set ROR2_GAME_DIR to your game installation."), "Risk of Rain 2_Data/Managed");
        string profile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Thunderstore Mod Manager\DataFolder\RiskOfRain2\profiles\Default\BepInEx");
        ResolveEventHandler resolve=delegate(object sender,ResolveEventArgs e){
            string name=new AssemblyName(e.Name).Name+".dll";
            foreach(string folder in new[]{game,Path.Combine(profile,"core")}){string file=Path.Combine(folder,name);if(File.Exists(file))return Assembly.LoadFrom(file);}
            var found=Directory.GetFiles(Path.Combine(profile,"plugins"),name,SearchOption.AllDirectories);return found.Length>0 ? Assembly.LoadFrom(found[0]) : null;
        };
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        try {
            var gameAssembly=Assembly.LoadFrom(Path.Combine(game,"RoR2.dll"));
            var controller=gameAssembly.GetType("RoR2.PlayerCharacterMasterController",true);
            var plugin=Assembly.LoadFrom(Path.GetFullPath("../dist/Diavolo/Diavolo.dll"));
            var create=plugin.GetType("Diavolo.NativeSkillGate",true).GetMethod("BuildSuppressor",BindingFlags.Static|BindingFlags.NonPublic);
            Check(create.MakeGenericMethod(controller).Invoke(null,null)!=null,"Native buffer layout incompatible");
            var buffer=controller.GetNestedType("ButtonBuffer",BindingFlags.Public|BindingFlags.NonPublic);
            object instance=Activator.CreateInstance(buffer);
            var push=buffer.GetMethod("PushValue");var poll=buffer.GetMethod("Poll");var stop=buffer.GetMethod("SuppressUntilReleased");
            Func<bool> value=()=>{object state=poll.Invoke(instance,null);return (bool)state.GetType().GetField("value").GetValue(state);};
            push.Invoke(instance,new object[]{true});stop.Invoke(instance,null);
            Check(!value(),"R3 modifier failed to suppress a buffered press");
            push.Invoke(instance,new object[]{true});Check(!value(),"Releasing R3 while trigger held fired a normal skill");
            push.Invoke(instance,new object[]{false});Check(!value(),"Trigger release fired a normal skill");
            push.Invoke(instance,new object[]{true});Check(value(),"Fresh trigger press did not restore normal skill");
            stop.Invoke(instance,null);Check(!value(),"R3 failed to stop an already-held normal skill");
            File.WriteAllText("../docs/native-gate-validation-2.5.3.txt","PASS: "+checks+" actual RoR2 buffer-layout, buffered-press suppression, held-trigger release latch and fresh-input recovery checks. Physical controller playtest pending.\n");
        }finally{AppDomain.CurrentDomain.AssemblyResolve-=resolve;}
    }
}
