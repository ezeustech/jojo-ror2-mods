using UnityEngine;
namespace Diavolo
{
    public sealed class CrimsonSpikes : MonoBehaviour
    {
        private Mesh mesh; private Material material; private float born,height;
        private Vector3 origin;
        public static void Create(Vector3[] ground,float victimHeight,float victimWidth)
        {
            if(ground==null || ground.Length!=3)return;
            Shader shader=DiavoloPresentation.Asset<Shader>("Assets/CrimsonModel.shader");if(!shader)return;
            for(int i=0;i<3;i++) {
                GameObject obj=new GameObject("King Crimson ground spike "+(i+1));
                CrimsonSpikes fx=obj.AddComponent<CrimsonSpikes>();fx.height=Mathf.Max(1.2f,victimHeight*1.25f);fx.born=Time.time;
                fx.origin=ground[i]-Vector3.up*(fx.height*.22f);obj.transform.position=fx.origin;
                fx.mesh=SpikeMesh.Create(fx.height,Mathf.Max(.08f,victimWidth*.075f));
                obj.AddComponent<MeshFilter>().sharedMesh=fx.mesh;
                fx.material=new Material(shader);fx.material.SetColor("_Color",new Color(.19f,.12f,.14f));fx.material.mainTexture=Texture2D.whiteTexture;
                obj.AddComponent<MeshRenderer>().sharedMaterial=fx.material;
            }
        }
        private void Update()
        {
            float age=Time.time-born;
            if(age>3.2f){Destroy(gameObject);return;}
            float sink=age>2.5f ? (age-2.5f)/.7f : 0;
            transform.position=origin-Vector3.up*height*sink;
        }
        private void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
