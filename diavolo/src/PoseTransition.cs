using UnityEngine;
namespace Diavolo
{
    // Blend only the entrance to a newly sampled pose. Authored timing stays untouched.
    public sealed class PoseTransition
    {
        private readonly Transform[] bones;
        private readonly Vector3[] positions;
        private readonly Quaternion[] rotations;
        private string state; private float began,duration,sequence; private bool initialized;
        public PoseTransition(GameObject root){bones=root.GetComponentsInChildren<Transform>(true);positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];}
        public void BeforeSample(string next,float nextSequence,float now,float seconds){
            if(!initialized){state=next;sequence=nextSequence;initialized=true;duration=0;return;}
            if(state==next && sequence==nextSequence)return;
            state=next;sequence=nextSequence;began=now;duration=Mathf.Max(.001f,seconds);
            // Root placement belongs to gameplay presentation, never pose blending.
            for(int i=1;i<bones.Length;i++)if(bones[i]){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
        }
        public void AfterSample(float now){
            if(duration<=0 || now-began>=duration)return;
            float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((now-began)/duration));
            for(int i=1;i<bones.Length;i++)if(bones[i]){bones[i].localPosition=Vector3.Lerp(positions[i],bones[i].localPosition,weight);bones[i].localRotation=Quaternion.Slerp(rotations[i],bones[i].localRotation,weight);}
        }
    }
}
