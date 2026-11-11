using UnityEngine;
using RoR2;
namespace Diavolo
{
    public static class UltimatePlacement
    {
        public static float Height(CharacterBody body)
        {
            Collider collider = body.GetComponent<Collider>();
            return collider ? Mathf.Max(1f,collider.bounds.size.y) : Mathf.Max(1f,body.radius*2f);
        }
        public static bool Ground(CharacterBody body, Vector3 probe, out Vector3 root, out Vector3 foot)
        {
            root=probe; foot=probe;
            Collider collider=body.GetComponent<Collider>();
            float offset=FateGrounding.RootOffset(body);
            RaycastHit hit;
            if (!Physics.Raycast(probe+Vector3.up*8f,Vector3.down,out hit,1000f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore) || hit.normal.y<.7f) return false;
            foot=hit.point+Vector3.up*.03f; root=foot+Vector3.up*offset;
            float radius=collider ? Mathf.Max(.2f,Mathf.Min(collider.bounds.extents.x,collider.bounds.extents.z)) : Mathf.Max(.3f,body.radius);
            float height=Mathf.Max(radius*2,Height(body));
            return !Physics.CheckCapsule(foot+Vector3.up*radius,foot+Vector3.up*(height-radius),radius*.94f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore);
        }
        public static bool Find(CharacterBody owner, CharacterBody target, Vector3 forward, out Vector3 ownerRoot, out Vector3 victimRoot, out Vector3[] spikes)
        {
            ownerRoot=owner.transform.position; victimRoot=target.transform.position; spikes=null;
            float width=Mathf.Max(.8f,target.radius*2f);
            float separation=owner.radius+target.radius+2.5f;
            for(int attempt=0;attempt<17;attempt++) {
                Vector3 offset=attempt==0 ? Vector3.zero : Quaternion.Euler(0,(attempt-1)*45f,0)*Vector3.forward*(attempt<=8 ? 4f : 8f);
                Vector3 a,b,af,bf;
                if (!Ground(owner,owner.transform.position+offset,out a,out af)) continue;
                if (!Ground(target,af-forward*separation,out b,out bf) || Mathf.Abs(af.y-bf.y)>2f) continue;
                if (Physics.Linecast(af+Vector3.up,bf+Vector3.up,LayerIndex.world.mask,QueryTriggerInteraction.Ignore)) continue;
                Vector3[] points=new Vector3[3]; bool valid=true;
                for(int i=0;i<3;i++) {
                    Vector3 p=bf+forward*((i-1)*width*.28f); RaycastHit floor;
                    if(!Physics.Raycast(p+Vector3.up*2f,Vector3.down,out floor,4f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore) || floor.normal.y<.7f || Mathf.Abs(floor.point.y-bf.y)>.6f) {valid=false;break;}
                    points[i]=floor.point;
                }
                if(!valid)continue;
                ownerRoot=a;victimRoot=b;spikes=points;return true;
            }
            return false;
        }
    }
}
