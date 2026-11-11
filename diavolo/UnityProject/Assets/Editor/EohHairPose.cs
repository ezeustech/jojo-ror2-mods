using System.Collections.Generic;
using UnityEngine;

namespace Diavolo
{
    // EOH simulates these long strands outside its skeletal clips. Reconstruct a
    // gravity-rest pose after sampling, before the CPU skin is baked.
    public sealed class EohHairPose
    {
        private readonly List<Transform> strands = new List<Transform>();
        public EohHairPose(GameObject actor)
        {
            foreach (Transform bone in actor.GetComponentsInChildren<Transform>())
                if ((bone.name.Contains("hair02") || bone.name.Contains(" hair b ") || bone.name.Contains(" hair c ") || bone.name.Contains(" hair d ") || bone.name.Contains(" hair e ") || bone.name.Contains(" hair f ")) && !bone.name.EndsWith(" d") && bone.childCount > 0) strands.Add(bone);
        }
        public void Apply(Vector3 backward)
        {
            foreach (Transform bone in strands) {
                Transform child = bone.GetChild(0);
                Vector3 segment = child.position - bone.position;
                float length = segment.magnitude;
                if (length < 1e-10f) continue;
                Vector3 direction = segment / length;
                // Keep side separation and curls, but stop long strands projecting
                // horizontally when Diavolo leans or turns his head.
                Vector3 resting = (Vector3.down + backward * .16f + Vector3.ProjectOnPlane(direction, Vector3.up) * .18f).normalized;
                bone.rotation = Quaternion.FromToRotation(direction, Vector3.Slerp(direction, resting, .82f)) * bone.rotation;
            }
        }
    }
}
