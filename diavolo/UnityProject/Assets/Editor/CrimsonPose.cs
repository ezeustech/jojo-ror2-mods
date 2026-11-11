using UnityEngine;

namespace Diavolo
{
    public static class CrimsonPose
    {
        public static Vector3 VisualPosition(Transform space, Transform bone)
        {
            return space ? space.position + (bone.position - space.position) * Mathf.Abs(space.lossyScale.x) : bone.position;
        }
        public static void Arm(Transform hand, Transform space, Vector3 target, Vector3 bend)
        {
            if (!hand || !hand.parent || !hand.parent.parent) return;
            Transform lower = hand.parent, upper = lower.parent;
            if (space) target = space.position + (target - space.position) / Mathf.Max(.0001f, Mathf.Abs(space.lossyScale.x));
            Vector3 origin = upper.position, delta = target - origin;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, hand.position);
            if (a < .00001f || b < .00001f || delta.sqrMagnitude < .00001f) return;
            float d = Mathf.Clamp(delta.magnitude, Mathf.Abs(a-b)+.00001f, a+b-.00001f);
            Vector3 axis = delta.normalized;
            float along = (a*a-b*b+d*d)/(2*d);
            Vector3 perpendicular = Vector3.ProjectOnPlane(bend,axis).normalized;
            if (perpendicular.sqrMagnitude < .01f) perpendicular = Vector3.Cross(axis, Vector3.up).normalized;
            Vector3 elbow = origin + axis * along + perpendicular * Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation = Quaternion.FromToRotation(lower.position-origin, elbow-origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position-lower.position, origin+axis*d-lower.position) * lower.rotation;
        }
    }
}
