// Engine substitutes expose collisions and scene actors to the production controller.
// These tests do not validate Unity rendering, real physics queries or transport.
using System.Reflection;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object o) => o != null; }
    public class GameObject : Object
    {
        private readonly Dictionary<Type, Component> components = new();
        public Transform transform = new();
        public Transform[] bones;
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:class => (bones??new[]{transform}).Cast<T>().ToArray();
        public T GetComponent<T>() where T : class => components.TryGetValue(typeof(T), out var c) ? c as T : null;
        public T AddComponent<T>() where T : Component
        {
            var c = (T)Activator.CreateInstance(typeof(T), true); c.gameObject = this; components[typeof(T)] = c;
            typeof(T).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(c, null);
            return c;
        }
    }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform; public T GetComponent<T>() where T : class => gameObject.GetComponent<T>(); }
    public class MonoBehaviour : Component { }
    public class Transform : Object { public Vector3 position,localPosition;public Quaternion localRotation=Quaternion.identity; public Vector3 forward = Vector3.forward; }
    public struct Quaternion
    {
        private System.Numerics.Quaternion value;
        public static Quaternion identity=>new(){value=System.Numerics.Quaternion.Identity};
        public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new(){value=System.Numerics.Quaternion.Slerp(a.value,b.value,t)};
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 up => new(0,1,0); public static Vector3 forward => new(0,0,1);
        public float sqrMagnitude => x*x+y*y+z*z; public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > .00001f ? this/magnitude : new();
        public static Vector3 operator +(Vector3 a,Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b) => new(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b) => new(a.x/b,a.y/b,a.z/b);
        public static float Distance(Vector3 a,Vector3 b) => (a-b).magnitude;
        public static float Dot(Vector3 a,Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b) => new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 MoveTowards(Vector3 a,Vector3 b,float distance) => Distance(a,b)<=distance?b:a+(b-a).normalized*distance;
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
        public override string ToString() => $"({x}, {y}, {z})";
    }
    public struct Ray { public Vector3 origin,direction;public Ray(Vector3 o,Vector3 d){origin=o;direction=d;} }
    public struct RaycastHit { public float distance; }
    public enum QueryTriggerInteraction { Ignore }
    public static class Physics
    {
        public static Func<Vector3,Vector3,bool> BlockLine = (a,b)=>false;
        public static bool BlockMovement;
        public static bool Linecast(Vector3 a,Vector3 b,int mask,QueryTriggerInteraction query) => BlockLine(a,b);
        public static bool SphereCast(Vector3 a,float r,Vector3 d,out RaycastHit hit,float length,int mask,QueryTriggerInteraction query) { hit=new();return BlockMovement; }
    }
    public static class Time { public static float time,fixedTime;public static int frameCount;public static float fixedDeltaTime=.02f,deltaTime=.02f; }
    public static class Mathf {
        public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float x)=>Math.Clamp(x,0,1);
        public static float Exp(float x)=>MathF.Exp(x);
        public static float MoveTowards(float a,float b,float step)=>Math.Abs(b-a)<=step?b:a+Math.Sign(b-a)*step;
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}
    }
}
namespace RoR2
{
    using UnityEngine;
    public enum TeamIndex { None=-1,Neutral,Player,Monster }
    public struct TeamMask { private int bits; public static TeamMask all=>new(){bits=-1};public void RemoveTeam(TeamIndex t){bits&=~(1<<(int)t);}public bool Contains(TeamIndex t)=>(bits&(1<<(int)t))!=0; }
    public class TeamComponent : Object { public TeamIndex teamIndex; }
    public class HealthComponent : Object { public bool alive=true; public CharacterBody body; }
    public class CharacterDirection : Object { public Vector3 forward=Vector3.forward; }
    public class CharacterMotor : Object { }
    public class InputBankTest : Object { public Vector3 aimOrigin,aimDirection=Vector3.forward; public ButtonState skill1=new(); }
    public class ButtonState { public bool down; }
    public class CharacterBody : Component
    {
        public HealthComponent healthComponent;public TeamComponent teamComponent=new();
        public CharacterDirection characterDirection=new();public InputBankTest inputBank=new();
        public Vector3 corePosition=>transform.position; public float moveSpeed=7f;public HurtBox mainHurtBox;
        public CharacterMotor characterMotor;public Vector3 footPosition=>corePosition-new Vector3(0,1,0);
    }
    public class HurtBox : Component { public HealthComponent healthComponent; }
    public class LocalUser { public CharacterBody cachedBody; }
    public static class LocalUserManager { public static List<LocalUser> readOnlyLocalUsersList=new(); }
    public class Indicator {public Indicator(GameObject owner,GameObject prefab){}public Transform targetTransform;public bool active;}
    public static class LegacyResourcesAPI { public static T Load<T>(string path) where T:class=>null; }
    public class LayerIndex {public static LayerIndex world=new();public int mask=1;}
    public class BullseyeSearch
    {
        public enum SortMode { Angle }
        public static List<HurtBox> Candidates=new();
        public TeamMask teamMaskFilter;public bool filterByLoS;public Vector3 searchOrigin,searchDirection;
        public SortMode sortMode;public float maxDistanceFilter,maxAngleFilter;private GameObject excluded;
        public void RefreshCandidates(){}public void FilterOutGameObject(GameObject o){excluded=o;}
        private float Angle(HurtBox h)=>MathF.Acos(Math.Clamp(Vector3.Dot((h.transform.position-searchOrigin).normalized,searchDirection.normalized),-1,1))*180/MathF.PI;
        public IEnumerable<HurtBox> GetResults()=>Candidates.Where(h=>h.gameObject!=excluded && teamMaskFilter.Contains(h.healthComponent.body.teamComponent.teamIndex) &&
            Vector3.Distance(h.transform.position,searchOrigin)<=maxDistanceFilter && Angle(h)<=maxAngleFilter &&
            (!filterByLoS || !Physics.Linecast(searchOrigin,h.transform.position,1,QueryTriggerInteraction.Ignore))).OrderBy(Angle);
    }
}
