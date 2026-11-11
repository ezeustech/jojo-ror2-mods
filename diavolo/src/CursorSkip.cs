using RoR2;
using UnityEngine;
namespace Diavolo
{
    public static class CursorSkip
    {
        public const float Range=80f;
        public static bool Destination(CharacterBody body,Ray aim,out Vector3 destination)
        {
            destination=body.transform.position;
            CapsuleCollider capsule=body.characterMotor && body.characterMotor.Motor ? body.characterMotor.Motor.Capsule : body.GetComponent<CapsuleCollider>();
            float radius=capsule ? capsule.radius*Mathf.Max(Mathf.Abs(capsule.transform.lossyScale.x),Mathf.Abs(capsule.transform.lossyScale.z)) : Mathf.Max(.3f,body.radius);
            float height=capsule ? Mathf.Max(radius*2,capsule.height*Mathf.Abs(capsule.transform.lossyScale.y)) : radius*2+1f;
            Vector3 center=capsule ? capsule.transform.TransformPoint(capsule.center) : body.corePosition;
            Vector3 footOffset=center-Vector3.up*(height*.5f)-body.transform.position;
            Vector3 feet;
            if(!SkipPlacementGeometry.Find(body.transform.position+footOffset,aim,Range,radius,height,LayerIndex.world.mask,LayerIndex.entityPrecise.mask,out feet))return false;
            destination=feet-footOffset;return true;
        }
    }
}
