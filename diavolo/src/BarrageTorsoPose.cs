using System.Collections.Generic;
using UnityEngine;
namespace Diavolo {
    // Borrowed arm motion should not carry The World's different torso rest axes.
    public sealed class BarrageTorsoPose {
        readonly Transform[] bones; readonly Quaternion[] neutral;
        public BarrageTorsoPose(GameObject model){
            var selected=new List<Transform>();
            foreach(var bone in model.GetComponentsInChildren<Transform>())
                if(bone.name.EndsWith(" body") || bone.name.EndsWith(" spine") || bone.name.EndsWith(" spine1") || bone.name.EndsWith(" spine2") || bone.name.EndsWith(" neck") || bone.name.EndsWith(" head"))selected.Add(bone);
            bones=selected.ToArray();neutral=new Quaternion[bones.Length];
        }
        public void Capture(){for(int i=0;i<bones.Length;i++)neutral[i]=bones[i].localRotation;}
        public void Apply(){for(int i=0;i<bones.Length;i++)bones[i].localRotation=Quaternion.Slerp(neutral[i],bones[i].localRotation,.15f);}
    }
}
