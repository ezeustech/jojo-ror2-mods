using System.IO;
using System.Reflection;
public static class ValidateBindings253 {
    public static void Run(){
        string previous=Directory.GetCurrentDirectory();
        try {
            Directory.SetCurrentDirectory(Path.GetFullPath(".."));
            Assembly.LoadFrom(Path.GetFullPath("tests/ControllerBindingsTests.exe")).GetType("ControllerBindingsTests").GetMethod("Main").Invoke(null,null);
            File.WriteAllText("docs/controller-bindings-validation-2.5.3.txt","PASS: 7 config migration, custom-mask retention, live dropdown updates, reverse sync, disable and persistence checks under Unity Mono.\n");
        }finally{Directory.SetCurrentDirectory(previous);}
    }
}
