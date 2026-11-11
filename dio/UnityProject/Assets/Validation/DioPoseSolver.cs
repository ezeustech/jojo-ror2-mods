using UnityEngine;
namespace EyesOfHeavenDio
{
    public static class DioPoseSolver
    {
        public static void AimStand(Transform root,Transform space,Transform leftShoulder,Transform rightShoulder,Vector3 origin,Vector3 direction,float reach=1.5f)
        {
            if(direction.sqrMagnitude<.0001f)return;
            direction.Normalize();
            Vector3 right=Vector3.Cross(Vector3.up,direction);
            if(right.sqrMagnitude<.000001f)right=Vector3.ProjectOnPlane(root.right,direction);
            if(right.sqrMagnitude<.000001f)right=Vector3.right;
            root.rotation=Quaternion.LookRotation(direction,Vector3.Cross(direction,right.normalized));
            // Anchor the shoulders, preserving the animation's actual fist travel.
            // Correct imported rig offsets in the same rendered space as skinning.
            if(leftShoulder&&rightShoulder)
            {
                var shoulders=(VisualPosition(space,leftShoulder)+VisualPosition(space,rightShoulder))*.5f;
                root.position+=origin+direction*reach-shoulders;
            }
            else root.position=origin+direction*reach;
        }
        public static void LimitStandTurn(Transform root,Transform pelvis,Quaternion rest)
        {
            if(!pelvis)return;
            Quaternion reference=root.rotation*rest;
            pelvis.rotation=Quaternion.RotateTowards(reference,pelvis.rotation,30f);
        }
        public static void AimPunchBody(Transform root,Transform space,Transform left,Transform right,Vector3 origin,Vector3 direction)
        {
            // Keep the body upright even when the crosshair passes through vertical.
            Vector3 horizontal=Vector3.ProjectOnPlane(direction,Vector3.up);
            if(horizontal.sqrMagnitude<.0025f)horizontal=Vector3.ProjectOnPlane(root.forward,Vector3.up);
            if(horizontal.sqrMagnitude<.0001f)horizontal=Vector3.forward;
            AimStand(root,space,left,right,origin,horizontal.normalized,2f);
            if(direction.sqrMagnitude>.0001f)root.position+=(direction.normalized-horizontal.normalized)*2f;
        }
        public static void AlignPunch(Transform root,Transform space,Transform left,Transform right,Vector3 origin,Vector3 direction,float blend)
        {
            if(!left||!right||direction.sqrMagnitude<.0001f)return;
            direction.Normalize();
            Vector3 fist=VisualPosition(space,right); // Fixed striking hand: no frame-to-frame anchor swapping.
            // Drive the arm from a stable idle rig instead of translating the whole
            // body to an animated fist, which fed animated rotations into placement.
            var lower=right.parent;var upper=lower?lower.parent:null;
            if(!upper||!lower)return;
            Vector3 target=Vector3.Lerp(fist,origin+direction*2.8f,Mathf.Clamp01(blend));
            Arm(upper,lower,right,target,root.right+Vector3.up*.2f,direction,space);
        }
        public static void RollerPunch(Transform root, Transform pelvis, Transform spine, Transform[] thighs, Transform[] calves, Transform[] feet,
            Transform[] upper, Transform[] lower, Transform[] hands, Vector3 surface, Vector3 right, Vector3 forward, float phase, Transform space, bool finisher=false)
        {
            if(!pelvis||!spine)return;
            float crouch=Mathf.SmoothStep(0,1,Mathf.Clamp01(phase/.25f));
            float beat=(phase-.25f)*Mathf.PI/.15f;
            float drive=Mathf.Sin(beat)*crouch;
            Vector3 hips=VisualPosition(space,pelvis);
            root.position+=surface-forward*.18f+right*(drive*.065f)+Vector3.up*(Mathf.Lerp(1.12f,.68f,crouch)-Mathf.Abs(drive)*.035f)-hips;
            spine.rotation=Quaternion.AngleAxis(drive*13f,Vector3.up)*Quaternion.AngleAxis((76f+Mathf.Abs(drive)*7f)*crouch,right)*spine.rotation;
            if(crouch>.99f)
            {
                float reach=0;Vector3 shoulders=Vector3.zero;
                for(int i=0;i<2;i++)
                {shoulders+=VisualPosition(space,upper[i])*.5f;reach+=(Vector3.Distance(VisualPosition(space,upper[i]),VisualPosition(space,lower[i]))+Vector3.Distance(VisualPosition(space,lower[i]),VisualPosition(space,hands[i])))*.5f;}
                root.position-=Vector3.up*Mathf.Clamp(shoulders.y-surface.y-.13f-reach*.88f,0,.12f);
            }
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                Quaternion footRotation=feet[i]?feet[i].rotation:Quaternion.identity;
                Arm(thighs[i],calves[i],feet[i],surface+right*side*.38f-forward*.25f+Vector3.up*.24f,forward, right,space);
                if(feet[i])feet[i].rotation=footRotation;
                float lift=finisher?0:phase<.25f?.45f:Mathf.Max(0,Mathf.Sin((phase-.25f)*Mathf.PI/.15f+i*Mathf.PI))*.42f;
                Vector3 contact=surface+right*side*.28f+forward*.55f+Vector3.up*(.13f+lift);
                Arm(upper[i],lower[i],hands[i],contact,right*side-forward*.2f,forward,space);
            }
        }
        public static Vector3 Handstand(Transform root, Quaternion rotation, Transform[] upper, Transform[] lower, Transform[] hands, Vector3 surface, Vector3 right, Vector3 forward, Transform visualSpace=null)
        {
            root.localRotation=rotation*Quaternion.Euler(180,0,0);
            if(!upper[0]||!upper[1]||!lower[0]||!lower[1]||!hands[0]||!hands[1])return surface;
            Vector3 shoulders=(VisualPosition(visualSpace,upper[0])+VisualPosition(visualSpace,upper[1]))*.5f;
            float reach=0;
            for(int i=0;i<2;i++)reach+=Vector3.Distance(VisualPosition(visualSpace,upper[i]),VisualPosition(visualSpace,lower[i]))+Vector3.Distance(VisualPosition(visualSpace,lower[i]),VisualPosition(visualSpace,hands[i]));
            reach*=.5f;
            // Position from the actual shoulder/arm geometry instead of a fixed
            // local-space height, which broke when the model was scaled/offset.
            root.position+=surface+Vector3.up*(reach*.9f+.08f)-shoulders;
            float spacing=Mathf.Clamp(Vector3.Distance(VisualPosition(visualSpace,upper[0]),VisualPosition(visualSpace,upper[1]))*.55f,.18f,.6f);
            Vector3 target=surface;
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;
                target=surface+right*side*spacing+forward*.04f+Vector3.up*.08f;
                Arm(upper[i],lower[i],hands[i],target,right*side,forward,visualSpace);
            }
            return target;
        }
        public static Vector3 VisualPosition(Transform visualSpace,Transform bone)
            => visualSpace ? visualSpace.position+(bone.position-visualSpace.position)*Mathf.Abs(visualSpace.lossyScale.x) : bone.position;
        public static void Arm(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 bend,Vector3 forward,Transform visualSpace=null)
        {
            if(!upper||!lower||!hand)return;
            if(visualSpace)target=visualSpace.position+(target-visualSpace.position)/Mathf.Max(.0001f,Mathf.Abs(visualSpace.lossyScale.x));
            Vector3 start=upper.position,delta=target-start;
            float a=Vector3.Distance(start,lower.position),b=Vector3.Distance(lower.position,hand.position);
            if(a<.000001f||b<.000001f||delta.sqrMagnitude<.0000000001f)return;
            float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.000001f,a+b-.000001f);
            Vector3 axis=delta.normalized;
            float along=(a*a-b*b+d*d)/(2*d);
            Vector3 perpendicular=Vector3.ProjectOnPlane(bend,axis).normalized;
            if(perpendicular.sqrMagnitude<.01f)perpendicular=Vector3.Cross(axis,forward).normalized;
            Vector3 elbow=start+axis*along+perpendicular*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-start,elbow-start)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,start+axis*d-lower.position)*lower.rotation;
        }
    }
}
