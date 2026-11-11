using UnityEngine;
namespace EyesOfHeavenDio
{
    // Short-lived fire lobes, expanding ground shockwave and rising smoke.
    public sealed class DioRollerExplosion : MonoBehaviour
    {
        private Material fire, smoke, ringMaterial;
        private Transform[] lobes;
        private Vector3[] offsets;
        private LineRenderer ring;
        private float age;
        public static void Spawn(Vector3 point)
        {
            var obj=new GameObject("RoadRollerExplosion");DioTimeWorld.MarkRollerEffect(obj);obj.transform.position=point;obj.AddComponent<DioRollerExplosion>();
        }
        private void Start()
        {
            fire=new Material(DioSpecialAssets.Ghost);smoke=new Material(DioSpecialAssets.Ghost);
            fire.SetColor("_Color",new Color(1,.45f,.04f,.85f));smoke.SetColor("_Color",new Color(.22f,.17f,.12f,.5f));
            lobes=new Transform[16];offsets=new Vector3[16];
            for(int i=0;i<16;i++)
            {
                var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var collider=sphere.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                sphere.transform.SetParent(transform,false);lobes[i]=sphere.transform;
                offsets[i]=new Vector3(Mathf.Cos(i*2.4f),.2f+(i%3)*.2f,Mathf.Sin(i*2.4f));
                sphere.GetComponent<Renderer>().sharedMaterial=i<8?fire:smoke;
            }
            var wave=new GameObject("BlastShockwave");wave.transform.SetParent(transform,false);
            ring=wave.AddComponent<LineRenderer>();ringMaterial=new Material(DioFeedback.RingShader);ring.sharedMaterial=ringMaterial;
            ring.useWorldSpace=false;ring.positionCount=64;ring.loop=true;
        }
        private void Update()
        {
            if(DioTimeWorld.Active&&!GetComponent<DioTimeStopExempt>())return;
            age+=Time.deltaTime;
            if(age>=1.6f){Destroy(gameObject);return;}
            float blast=Mathf.Clamp01(age/.32f), fade=1-Mathf.Clamp01(age/.65f);
            fire.SetColor("_Color",new Color(1,Mathf.Lerp(.85f,.18f,blast),.04f,fade*.85f));
            smoke.SetColor("_Color",new Color(.22f,.17f,.12f,.45f*(1-age/1.6f)));
            for(int i=0;i<lobes.Length;i++)
            {
                bool flame=i<8;
                lobes[i].localPosition=offsets[i]*(flame?blast*3.5f:age*3)+Vector3.up*(flame?0:age*2);
                lobes[i].localScale=Vector3.one*(flame?Mathf.Lerp(.3f,5f,blast):1.5f+age*2);
            }
            float radius=Mathf.Lerp(.5f,12f,Mathf.Clamp01(age/.5f));
            ring.startWidth=ring.endWidth=.25f*(1-Mathf.Clamp01(age/.7f));
            ring.startColor=ring.endColor=new Color(1,.6f,.15f,fade);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,-1.05f,Mathf.Sin(a)*radius));}
        }
        private void OnDestroy(){if(fire)Destroy(fire);if(smoke)Destroy(smoke);if(ringMaterial)Destroy(ringMaterial);}
    }
}
