using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
namespace Diavolo
{
    public static class BloodFx
    {
        private static Material material;private static int emissionFrame,emissions;
        private static readonly System.Collections.Generic.Queue<GameObject> pool=new System.Collections.Generic.Queue<GameObject>();
        internal static void Recycle(GameObject obj){obj.SetActive(false);if(pool.Count<24)pool.Enqueue(obj);else Object.Destroy(obj);}
        public static void Impact(Vector3 position, Vector3 direction, bool heavy)
        {
            if (!NetworkServer.active) return;
            if (NetworkClient.active) Spray(position, direction, heavy ? 230 : 12, false, heavy);
            new BloodFxMessage(position, direction, heavy).Send(NetworkDestination.Clients);
        }
        public static void Throw(Ray aim) { Spray(aim.origin + aim.direction * .7f, aim.direction, 85, true, false); }
        public static void Spray(Vector3 position, Vector3 direction, int count, bool thrown, bool heavy)
        {
            if(emissionFrame!=Time.frameCount){emissionFrame=Time.frameCount;emissions=0;}
            if(!thrown && emissions++>=12)return;
            if (!material) {
                Shader shader = DiavoloPresentation.Asset<Shader>("Assets/CrimsonFX.shader");
                if (!shader) return;
                Texture2D texture = new Texture2D(32,32,TextureFormat.RGBA32,false);
                for (int y=0;y<32;y++) for (int x=0;x<32;x++) {
                    float radius = new Vector2((x-15.5f)/15f,(y-15.5f)/15f).magnitude;
                    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01((1-radius)*6f)));
                }
                texture.Apply();
                material = new Material(shader); material.mainTexture = texture;
                material.SetColor("_Color", new Color(.32f,.008f,.016f,.92f));
            }
            GameObject obj=null;while(pool.Count>0 && !obj)obj=pool.Dequeue();
            if(!obj){obj=new GameObject("Crimson blood droplets");obj.AddComponent<ParticleSystem>();obj.AddComponent<BloodLifetime>();}
            obj.SetActive(true);
            obj.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction.sqrMagnitude > .001f ? direction.normalized : Vector3.up));
            ParticleSystem ps = obj.GetComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.duration = .12f; main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(thrown ? .78f : .2f, thrown ? .85f : heavy ? .65f : .4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(thrown ? 60f : 3f, thrown ? 62f : heavy ? 18f : 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(thrown ? .055f : .04f, heavy ? .26f : .11f);
            main.gravityModifier = thrown ? .08f : 1.2f;
            var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0,(short)count) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = thrown ? 6f : heavy ? 48f : 30f; shape.radius = thrown ? .06f : .12f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0,1,1,0));
            var collision = ps.collision; collision.enabled = false; collision.type = ParticleSystemCollisionType.World; collision.collidesWith = RoR2.LayerIndex.world.mask; collision.lifetimeLoss = 1f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            ps.Play(); obj.GetComponent<BloodLifetime>().Expires=Time.time+1.2f;
        }
    }
    public sealed class BloodLifetime:MonoBehaviour {public float Expires;private void Update(){if(Time.time>=Expires)BloodFx.Recycle(gameObject);}}
    public sealed class BloodFxMessage : INetMessage
    {
        private Vector3 position, direction; private bool heavy;
        public BloodFxMessage() { }
        public BloodFxMessage(Vector3 position, Vector3 direction, bool heavy) { this.position=position; this.direction=direction; this.heavy=heavy; }
        public void Serialize(NetworkWriter writer) { writer.Write(position); writer.Write(direction); writer.Write(heavy); }
        public void Deserialize(NetworkReader reader) { position=reader.ReadVector3(); direction=reader.ReadVector3(); heavy=reader.ReadBoolean(); }
        public void OnReceived() { if (!NetworkServer.active) BloodFx.Spray(position,direction,heavy ? 230 : 12,false,heavy); }
    }
}
