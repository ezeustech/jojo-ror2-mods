using UnityEngine;
namespace EyesOfHeavenDio
{
    public static class DioDashCollision
    {
        public static Vector3 Move(Vector3 from,Vector3 desired,Vector3 center,float radius,float half,float feetOffset,int mask)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,desired)/.3f));
            Vector3 safe=from;
            float castRadius=Mathf.Max(.1f,radius-.03f);
            Vector3 extent=Vector3.up*(half-radius);
            for(int i=1;i<=steps;i++)
            {
                Vector3 next=Vector3.Lerp(from,desired,(float)i/steps);
                Vector3 probe=new Vector3(next.x,Mathf.Max(next.y,safe.y)+1.5f,next.z);
                if(Physics.Raycast(probe,Vector3.down,out var floor,4f,mask,QueryTriggerInteraction.Ignore)&&floor.normal.y>.5f)
                    next.y=Mathf.Max(next.y,floor.point.y+feetOffset+.08f);
                Vector3 delta=next-safe;
                if(delta.sqrMagnitude>.000001f&&Physics.CapsuleCast(safe+center+extent,safe+center-extent,castRadius,delta.normalized,out var hit,delta.magnitude,mask,QueryTriggerInteraction.Ignore))break;
                if(Physics.CheckCapsule(next+center+extent,next+center-extent,castRadius,mask,QueryTriggerInteraction.Ignore))break;
                safe=next;
            }
            return safe;
        }
    }
}
