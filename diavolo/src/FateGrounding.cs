using UnityEngine;
using RoR2;
namespace Diavolo
{
    public static class FateGrounding
    {
        public static bool IsFlyer(CharacterBody body)
        {
            // Some grounded bosses also advertise isFlying; use actual airborne families.
            // CharacterBody.isFlying defaults true for many motorless ground bosses.
            // Identify genuinely airborne families instead of using that fallback.
            string name=body.name.ToLowerInvariant();
            foreach(string flyer in new[]{"vagrant","wisp","jellyfish","roboball","flyingvermin","soluswing","vulture","greaterconstruct","minorconstruct","flyinglunar","solusprobe","drone"})
                if(name.Contains(flyer))return true;
            return false;
        }
        public static float RootOffset(CharacterBody body)
        {
            CapsuleCollider capsule=body.characterMotor && body.characterMotor.Motor ? body.characterMotor.Motor.Capsule : body.GetComponent<CapsuleCollider>();
            if(capsule) {
                Vector3 scale=capsule.transform.lossyScale;
                float half=capsule.height*.5f*Mathf.Abs(capsule.direction==0 ? scale.x : capsule.direction==2 ? scale.z : scale.y);
                Vector3 axis=capsule.direction==0 ? capsule.transform.right : capsule.direction==2 ? capsule.transform.forward : capsule.transform.up;
                float radial=capsule.radius*(capsule.direction==0 ? Mathf.Max(Mathf.Abs(scale.y),Mathf.Abs(scale.z)) : capsule.direction==2 ? Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y)) : Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)));
                float extent=Mathf.Abs(axis.y)*Mathf.Max(0,half-radial)+radial;
                return body.transform.position.y-(capsule.transform.TransformPoint(capsule.center).y-extent);
            }
            if(body.characterMotor)return body.characterMotor.capsuleHeight*.5f-body.characterMotor.capsuleYOffset;
            Collider solid=body.GetComponent<Collider>();
            if(solid && solid.enabled && !solid.isTrigger)return body.transform.position.y-solid.bounds.min.y;
            if(body.modelLocator && body.modelLocator.modelTransform) {
                CharacterModel model=body.modelLocator.modelTransform.GetComponent<CharacterModel>();
                float bottom=float.PositiveInfinity;
                if(model)foreach(CharacterModel.RendererInfo info in model.baseRendererInfos)
                    if(info.renderer && info.renderer.bounds.size.y>.1f)bottom=Mathf.Min(bottom,info.renderer.bounds.min.y);
                if(!float.IsInfinity(bottom))return body.transform.position.y-bottom;
            }
            return body.transform.position.y-body.footPosition.y;
        }
        public static void Dimensions(CharacterBody body, out float radius, out float height)
        {
            CapsuleCollider capsule=body.characterMotor && body.characterMotor.Motor ? body.characterMotor.Motor.Capsule : body.GetComponent<CapsuleCollider>();
            radius=Mathf.Max(.35f,body.radius);height=Mathf.Max(2,radius*2);
            if(capsule){Vector3 scale=capsule.transform.lossyScale;radius=Mathf.Max(.2f,capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)));height=Mathf.Max(radius*2,capsule.height*Mathf.Abs(scale.y));}
        }
    }
}
