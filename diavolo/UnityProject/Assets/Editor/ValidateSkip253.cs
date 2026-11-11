using UnityEngine;
using System;
using System.IO;
using Diavolo;
public static class ValidateSkip253
{
    static int checks;
    static GameObject Box(Vector3 p,Vector3 s){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.position=p;g.transform.localScale=s;return g;}
    static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
    static bool Find(Vector3 dir,out Vector3 p){return SkipPlacementGeometry.Find(new Vector3(0,.08f,0),new Ray(new Vector3(0,1.5f,0),dir.normalized),80,.6f,3f,1,0,out p);}
    public static void Run(){
        var floor=Box(new Vector3(0,-.5f,0),new Vector3(250,1,250));
        var ledge=Box(new Vector3(30,10,0),new Vector3(20,20,30));Physics.SyncTransforms();Vector3 p;
        Check(Find(new Vector3(1,.3f,0),out p)&&p.x>20&&Mathf.Abs(p.y-20.08f)<.02f,"Uphill skip stopped at mountain face instead of landing on top: "+p);
        Check(!Physics.CheckCapsule(p+Vector3.up*.6f,p+Vector3.up*2.4f,.582f,1),"Ledge landing penetrates geometry");
        Check(Find(Vector3.right,out p)&&p.x<20,"Horizontal skip crossed a wall");
        var ceiling=Box(new Vector3(0,6,0),new Vector3(12,1,12));Physics.SyncTransforms();
        Check(Find(new Vector3(1,.3f,0),out p)&&p.x<20,"Assist crossed a low ceiling");
        UnityEngine.Object.DestroyImmediate(ceiling);
        ledge.transform.position=new Vector3(30,20,0);ledge.transform.localScale=new Vector3(20,40,30);Physics.SyncTransforms();
        Check(Find(new Vector3(1,.6f,0),out p)&&Mathf.Abs(p.y-40.08f)<.02f,"Tall mountain top was unreachable: "+p);
        UnityEngine.Object.DestroyImmediate(ledge);
        Check(Find(new Vector3(1,-.05f,0),out p)&&Mathf.Abs(p.y-.08f)<.02f,"Flat ground landing regressed");
        Check(Find(Vector3.up,out p)&&(p-new Vector3(0,.08f,0)).magnitude<=80.01f,"Vertical sky skip exceeded range");
        var thin=Box(new Vector3(30,10,0),new Vector3(20,20,.3f));Physics.SyncTransforms();
        Check(Find(new Vector3(1,.3f,0),out p)&&p.x<20,"Assist perched on an unsupported narrow wall");
        UnityEngine.Object.DestroyImmediate(thin);UnityEngine.Object.DestroyImmediate(floor);Physics.SyncTransforms();
        Check(Find(new Vector3(1,.3f,0),out p)&&(p-new Vector3(0,.08f,0)).magnitude<=80.01f,"Open-air skip failed or exceeded range");
        File.WriteAllText("../docs/skip-validation-2.5.3.txt","PASS: "+checks+" mountain/ledge, capsule clearance, wall, ceiling, tall mountain, flat ground, sky range and narrow support checks. Live maps still require testing.\n");
        Debug.Log("PASS: "+checks+" Shift placement checks");
    }
}
