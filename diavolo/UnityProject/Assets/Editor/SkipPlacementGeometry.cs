using UnityEngine;
namespace Diavolo
{
    // Feet coordinates keep landing clearance independent of the model's root/pivot.
    public static class SkipPlacementGeometry
    {
        public static bool Find(Vector3 feet, Ray aim, float range, float radius, float height, int world, int targets, out Vector3 destination)
        {
            destination=feet;
            aim.direction=aim.direction.normalized;
            RaycastHit hit;
            bool struck=Physics.Raycast(aim,out hit,range,world|targets,QueryTriggerInteraction.Ignore);
            Vector3 point=struck ? hit.point : aim.GetPoint(range);
            Vector3 forward=Vector3.ProjectOnPlane(aim.direction,Vector3.up).normalized;
            bool uphill=aim.direction.y>.08f;
            // Looking up enables a search beyond the mountain face for a walkable top.
            // Low ceilings or a blocked route above the ledge still reject the landing.
            for(int i=0;i<(uphill ? 7 : 1);i++) {
                float step=i==0 ? 0 : radius+1f+(i-1)*3f;
                Vector3 probe=point+forward*step;
                probe.y=Mathf.Min(feet.y+range,Mathf.Max(feet.y,point.y)+(uphill ? 32f : 3f));
                RaycastHit floor;
                float down=probe.y-Mathf.Max(feet.y-24f,point.y-(uphill ? 48f : 24f));
                if(down<=0 || !Physics.Raycast(probe,Vector3.down,out floor,down,world,QueryTriggerInteraction.Ignore) || floor.normal.y<.6f)continue;
                Vector3 landing=floor.point+Vector3.up*.08f;
                if((landing-feet).sqrMagnitude>range*range || (landing-feet).sqrMagnitude<.25f)continue;
                if(!Supported(landing,radius,world) || !Free(landing,radius,height,world))continue;
                if(Clear(feet,landing,radius,height,world) || (uphill && landing.y>feet.y+.5f && OverTop(feet,landing,radius,height,world))) {
                    destination=landing;return true;
                }
            }
            // No safe ledge: preserve an airborne skip, stopping before solid geometry.
            Vector3 desired=point-Vector3.up*(height*.5f)+(struck ? hit.normal*(radius+.2f) : Vector3.zero);
            Vector3 delta=Vector3.ClampMagnitude(desired-feet,range);
            if(delta.sqrMagnitude<.01f)return false;
            Vector3 bottom,top;Capsule(feet,radius,height,out bottom,out top);
            RaycastHit obstacle;
            if(Physics.CapsuleCast(bottom,top,radius*.97f,delta.normalized,out obstacle,delta.magnitude,world,QueryTriggerInteraction.Ignore))
                delta=delta.normalized*Mathf.Max(0,obstacle.distance-.15f);
            desired=feet+delta;
            for(int i=0;i<20;i++) {
                Vector3 candidate=Vector3.Lerp(desired,feet,i/20f);
                if((candidate-feet).sqrMagnitude<.25f)return false;
                if(Free(candidate,radius,height,world)){destination=candidate;return true;}
            }
            return false;
        }
        private static bool Supported(Vector3 feet,float radius,int world) {
            for(int i=0;i<5;i++) {
                Vector3 offset=i==0 ? Vector3.zero : new Vector3(Mathf.Cos((i-1)*Mathf.PI*.5f),0,Mathf.Sin((i-1)*Mathf.PI*.5f))*radius*.85f;
                RaycastHit floor;
                if(!Physics.Raycast(feet+offset+Vector3.up*.4f,Vector3.down,out floor,1.2f,world,QueryTriggerInteraction.Ignore) || floor.normal.y<.6f)return false;
            }
            return true;
        }
        private static void Capsule(Vector3 feet,float radius,float height,out Vector3 bottom,out Vector3 top) {
            bottom=feet+Vector3.up*radius;top=feet+Vector3.up*(height-radius);
        }
        private static bool Free(Vector3 feet,float radius,float height,int world) {
            Vector3 bottom,top;Capsule(feet,radius,height,out bottom,out top);
            return !Physics.CheckCapsule(bottom,top,radius*.97f,world,QueryTriggerInteraction.Ignore);
        }
        private static bool Clear(Vector3 from,Vector3 to,float radius,float height,int world) {
            Vector3 delta=to-from;if(delta.sqrMagnitude<.0001f)return true;
            Vector3 bottom,top;Capsule(from,radius,height,out bottom,out top);
            return !Physics.CapsuleCast(bottom,top,radius*.97f,delta.normalized,delta.magnitude,world,QueryTriggerInteraction.Ignore);
        }
        private static bool OverTop(Vector3 from,Vector3 to,float radius,float height,int world) {
            float travelY=Mathf.Max(from.y,to.y)+radius+1f;
            Vector3 aboveStart=new Vector3(from.x,travelY,from.z),aboveEnd=new Vector3(to.x,travelY,to.z);
            return Clear(from,aboveStart,radius,height,world) && Clear(aboveStart,aboveEnd,radius,height,world) && Clear(aboveEnd,to,radius,height,world);
        }
    }
}
