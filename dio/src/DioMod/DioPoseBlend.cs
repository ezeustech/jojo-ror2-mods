using UnityEngine;
namespace EyesOfHeavenDio
{
    // Legacy Animation cannot crossfade a clip back into itself. Capture the
    // displayed pose so repeated punches and interrupted moves blend correctly.
    internal sealed class DioPoseBlend
    {
        private readonly Transform[] bones;
        private readonly Vector3[] positions;
        private readonly Quaternion[] rotations;
        private float began, duration;
        private int capturedFrame=-1;
        internal DioPoseBlend(GameObject root)
        {
            bones=root.GetComponentsInChildren<Transform>(true);
            positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];
        }
        internal void Begin(float seconds)
        {
            if(capturedFrame!=Time.frameCount) {
                for(int i=1;i<bones.Length;i++)if(bones[i]){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
                capturedFrame=Time.frameCount;began=Time.time;
            }
            duration=Mathf.Max(.001f,seconds);
        }
        internal void Apply()
        {
            if(capturedFrame<0 || duration<=0 || Time.time-began>=duration)return;
            float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/duration));
            for(int i=1;i<bones.Length;i++)if(bones[i]) {
                bones[i].localPosition=Vector3.Lerp(positions[i],bones[i].localPosition,weight);
                bones[i].localRotation=Quaternion.Slerp(rotations[i],bones[i].localRotation,weight);
            }
        }
    }
}
