// Small transport/engine substitutes for exercising the production lifecycle code.
namespace UnityEngine {
 public class Object { public static implicit operator bool(Object o)=>o!=null; }
 public class GameObject:Object { public Dictionary<Type,object> components=new(); }
 public class Component:Object {public GameObject gameObject=new();public T GetComponent<T>() where T:class=>gameObject.components.TryGetValue(typeof(T),out var c)?c as T:null;}
 public static class Time{public static float time;}
 public static class Mathf{public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace UnityEngine.Networking {
 public class NetworkBehaviour:UnityEngine.Component{public uint dirty;public void SetDirtyBit(uint bit){dirty|=bit;}public virtual bool OnSerialize(NetworkWriter w,bool initial)=>false;public virtual void OnDeserialize(NetworkReader r,bool initial){}}
 public static class NetworkServer{public static bool active;}
 public class NetworkWriter{public MemoryStream stream=new();public void Write(bool x){new BinaryWriter(stream).Write(x);}public void Write(float x){new BinaryWriter(stream).Write(x);}}
 public class NetworkReader{private BinaryReader reader;public NetworkReader(byte[] bytes){reader=new BinaryReader(new MemoryStream(bytes));}public bool ReadBoolean()=>reader.ReadBoolean();public float ReadSingle()=>reader.ReadSingle();}
}
namespace RoR2{
 public class Health:UnityEngine.Object{public bool alive=true;}
 public class CharacterBody:UnityEngine.Object{public bool local;public Health healthComponent=new();}
 public class Run:UnityEngine.Object{public static Run instance=new();}
}
namespace EyesOfHeavenDio{
 public static class DioInput{public static bool IsLocal(RoR2.CharacterBody b)=>b.local;}
 public static class DioPlugin{public class Setting{public float Value=1.9f;}public static Setting TimeResumeTickTime=new();}
 public static class DioFeedback{
  public static List<bool> transitions=new();public static int sounds;public static float duration;
  public static void MainTimeTransition(bool ending,bool local,float seconds=15,bool replay=true,RoR2.CharacterBody owner=null){transitions.Add(ending);duration=seconds;}
  public static void Sound(UnityEngine.GameObject o,string name,float volume){sounds++;}
 }
}
