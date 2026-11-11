using System.Reflection;
namespace UnityEngine
{
    public class Object
    {
        internal bool destroyed;
        public static implicit operator bool(Object value)=>value!=null&&!value.destroyed;
        public static void Destroy(Object value)
        {
            if(!value)return;
            value.destroyed=true;
            if(value is GameObject obj)foreach(var component in obj.components.Values.ToArray()) {
                component.GetType().GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(component,null);
                component.destroyed=true;
            }
        }
    }
    public class GameObject:Object
    {
        internal readonly Dictionary<Type,Component> components=new();public uint netId;
        public T GetComponent<T>() where T:class=>components.TryGetValue(typeof(T),out var c)?c as T:null;
        public T AddComponent<T>() where T:Component { var c=(T)Activator.CreateInstance(typeof(T),true);c.gameObject=this;components[typeof(T)]=c;return c; }
    }
    public class Component:Object {public GameObject gameObject;public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();}
    public class MonoBehaviour:Component{}
    public static class Mathf {public static float Clamp01(float x)=>Math.Clamp(x,0,1);}
}
namespace UnityEngine.Networking
{
    public static class NetworkServer{public static bool active;}
    public static class NetworkClient{public static bool active;}
    public class NetworkWriter
    {
        public readonly MemoryStream stream=new();private BinaryWriter writer;public NetworkWriter(){writer=new(stream);}
        public void Write(UnityEngine.GameObject value)=>writer.Write(value.netId);
        public void Write(string value)=>writer.Write(value);public void Write(float value)=>writer.Write(value);
        public void Write(uint value)=>writer.Write(value);public void Write(byte value)=>writer.Write(value);public void Write(bool value)=>writer.Write(value);
    }
    public class NetworkReader
    {
        private BinaryReader reader;public NetworkReader(byte[] bytes){reader=new(new MemoryStream(bytes));}
        public UnityEngine.GameObject ReadGameObject()=>Harness.Current.owners[reader.ReadUInt32()];
        public string ReadString()=>reader.ReadString();public float ReadSingle()=>reader.ReadSingle();public uint ReadUInt32()=>reader.ReadUInt32();
        public byte ReadByte()=>reader.ReadByte();public bool ReadBoolean()=>reader.ReadBoolean();
    }
}
namespace R2API.Networking {public enum NetworkDestination{Server,Clients}}
namespace R2API.Networking.Interfaces
{
    public interface INetMessage {void Serialize(UnityEngine.Networking.NetworkWriter writer);void Deserialize(UnityEngine.Networking.NetworkReader reader);void OnReceived();}
    public static class NetMessageExtensions
    {
        public static void Send(this INetMessage message,R2API.Networking.NetworkDestination destination)
        {var writer=new UnityEngine.Networking.NetworkWriter();message.Serialize(writer);Harness.Current.sent.Add((destination,writer.stream.ToArray()));}
    }
}
namespace RoR2 {public class CharacterBody:UnityEngine.Component{public bool hasEffectiveAuthority,local;}}
namespace EyesOfHeavenDio
{
    public class DioVisualDriver:UnityEngine.Component{}
    public class DioAudioPlayback:UnityEngine.MonoBehaviour{internal event Action Stopped;private void OnDestroy(){Harness.Current.stops++;Stopped?.Invoke();}}
    public static class DioInput {public static bool IsLocal(RoR2.CharacterBody body)=>body&&body.local;}
    public static class DioFeedback
    {
        public static bool HasClip(string cue)=>cue is "impact" or "barrage" or "time-resume" or "death-voice";
        public static DioAudioPlayback PlayLocal(UnityEngine.GameObject owner,string cue,float volume)
        {Harness.Current.plays++;return new UnityEngine.GameObject().AddComponent<DioAudioPlayback>();}
    }
}
public sealed class Peer
{
    public bool server,client;public int plays,stops;
    public readonly Dictionary<uint,UnityEngine.GameObject> owners=new();
    public readonly List<(R2API.Networking.NetworkDestination destination,byte[] bytes)> sent=new();
    public Peer(bool server,bool client,bool owns)
    {
        this.server=server;this.client=client;var obj=new UnityEngine.GameObject{netId=1};
        var body=obj.AddComponent<RoR2.CharacterBody>();body.hasEffectiveAuthority=owns;body.local=owns;
        obj.AddComponent<EyesOfHeavenDio.DioVisualDriver>();owners[1]=obj;
    }
}
public static class Harness
{
    public static Peer Current;
    public static void Select(Peer peer){Current=peer;UnityEngine.Networking.NetworkServer.active=peer.server;UnityEngine.Networking.NetworkClient.active=peer.client;}
    public static void Deliver(Peer peer,byte[] bytes){Select(peer);var message=new EyesOfHeavenDio.DioSoundMessage();message.Deserialize(new(bytes));message.OnReceived();}
}
