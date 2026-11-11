using UnityEngine;
namespace EyesOfHeavenDio
{
    public sealed class DioStandParticles : MonoBehaviour
    {
        private Material material;
        private ParticleSystem particles;
        private float age;
        internal static void Emit(Vector3 position, bool impact=false)
        {
            if(!DioFeedback.RingShader)return;
            var root=new GameObject(impact?"DioGuardSparks":"DioSummonGold");root.transform.position=position;
            root.AddComponent<DioStandParticles>().Begin(impact);
        }
        private void Begin(bool impact)
        {
            particles=gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=false;main.duration=.8f;main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.65f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(impact?1.6f:.25f,impact?4f:1.2f);
            main.startSize=new ParticleSystem.MinMaxCurve(.025f,.065f);main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.gravityModifier=-.12f;main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.65f,.1f,.8f),new Color(1,.92f,.5f,.9f));
            var emission=particles.emission;emission.enabled=false;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=impact?.12f:.7f;
            var fade=particles.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.7f,.2f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            material=new Material(DioFeedback.RingShader);
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=1.5f;renderer.velocityScale=.08f;
            particles.Play();particles.Emit(impact?22:32);
        }
        private void Update(){if(DioTimeWorld.Active)return;age+=Time.deltaTime;if(age>1.2f)Destroy(gameObject);}
        private void OnDestroy(){if(material)Destroy(material);}
    }
}
