using UnityEngine;
namespace EyesOfHeavenDio
{
    [DefaultExecutionOrder(25)]
    public sealed class DioRollerPose : MonoBehaviour
    {
        public int Mode;
        public float PunchPhase=-1;
        public bool Finisher;
        private DioVisualDriver driver;
        private Transform visualSpace;
        private Vector3 position;
        private Quaternion rotation;
        private GameObject arms, companion;
        private bool savedSuppress, restored;
        private readonly Transform[] upper=new Transform[2], lower=new Transform[2], hands=new Transform[2];
        private readonly Transform[] thighs=new Transform[2], calves=new Transform[2], feet=new Transform[2];
        private Transform pelvis, spine;
        private Animation poseAnimation;
        private AnimationClip idle;
        private float punchStarted=-1;
        private readonly System.Collections.Generic.Dictionary<Transform,Quaternion> fists=new System.Collections.Generic.Dictionary<Transform,Quaternion>();
        public void Begin()
        {
            driver=GetComponent<DioVisualDriver>();position=driver.dioRoot.transform.localPosition;rotation=driver.dioRoot.transform.localRotation;
            foreach(var skin in driver.dioRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.name.EndsWith(" body")){visualSpace=skin.transform;break;}
            savedSuppress=driver.SuppressStand;driver.SuppressStand=true;
            poseAnimation=driver.dioRoot.GetComponentInChildren<Animation>(true);
            if(poseAnimation)foreach(AnimationState state in poseAnimation)if(state.name.EndsWith("_bas_nut")){idle=state.clip;break;}
            if(poseAnimation)foreach(AnimationState state in poseAnimation)if(state.name.EndsWith("_spa_01_lp"))
            {
                state.clip.SampleAnimation(poseAnimation.gameObject,.2f);
                foreach(var bone in driver.dioRoot.GetComponentsInChildren<Transform>(true))if(bone.name.Contains(" finger"))fists[bone]=bone.localRotation;
                if(idle)idle.SampleAnimation(poseAnimation.gameObject,.1f);
                break;
            }
            foreach(var bone in driver.dioRoot.GetComponentsInChildren<Transform>(true))
            {
                if(bone.name.EndsWith(" pelvis"))pelvis=bone;
                if(bone.name.EndsWith(" spine"))spine=bone;
                for(int i=0;i<2;i++)
                {
                    string side=i==0?" l ":" r ";
                    if(bone.name.EndsWith(side+"upperarm"))upper[i]=bone;
                    if(bone.name.EndsWith(side+"forearm"))lower[i]=bone;
                    if(bone.name.EndsWith(side+"hand"))hands[i]=bone;
                    if(bone.name.EndsWith(side+"thigh"))thighs[i]=bone;
                    if(bone.name.EndsWith(side+"calf"))calves[i]=bone;
                    if(bone.name.EndsWith(side+"foot"))feet[i]=bone;
                }
            }
        }
        private GameObject MakeStand(bool handsOnly)
        {
            var stand=Instantiate(driver.StandPrefab,driver.dioRoot.transform.parent);stand.name=handsOnly?"RoadRollerStandHands":"RoadRollerEscapeStand";
            driver.SummonFeedback(transform.position+Vector3.up);
            foreach(var skin in stand.GetComponentsInChildren<DioCpuSkinning>(true))skin.armsOnly=handsOnly;
            stand.AddComponent<DioStandLegPose>().Bind(driver.dioRoot.transform);
            return stand;
        }
        private void LateUpdate()
        {
            if(!driver||restored)return;
            var root=driver.dioRoot.transform;
            root.localPosition=position+(Mode==0?Vector3.up*2.3f:Vector3.zero);
            root.localRotation=rotation*Quaternion.Euler(Mode==0?180:0,0,0);
            if(Mode==0)DioPoseSolver.Handstand(root,rotation,upper,lower,hands,transform.position,transform.right,transform.forward,visualSpace);
            if(Mode==1)
            {
                root.rotation=Quaternion.LookRotation(transform.forward,Vector3.up);
                if(punchStarted<0)punchStarted=Time.time;
                if(idle)idle.SampleAnimation(poseAnimation.gameObject,.1f);
                foreach(var finger in fists)if(finger.Key)finger.Key.localRotation=finger.Value;
                DioPoseSolver.RollerPunch(root,pelvis,spine,thighs,calves,feet,upper,lower,hands,transform.position,transform.right,transform.forward,PunchPhase>=0?PunchPhase:Time.time-punchStarted,visualSpace,Finisher);
            }
            driver.worldRoot.SetActive(false);
            if(Mode==1&&!arms)arms=MakeStand(true);
            if(arms)arms.SetActive(Mode==1);
            if(Mode==2&&!companion)companion=MakeStand(false);
            if(companion)
            {
                companion.SetActive(Mode==2);
                companion.GetComponent<DioStandLegPose>().Offset=transform.right*.9f-transform.forward*.4f;
            }
        }
        private void PlaceHand(int i,Vector3 target,Vector3 bend)
        {
            DioPoseSolver.Arm(upper[i],lower[i],hands[i],target,bend,transform.forward,visualSpace);
        }

        public void Restore()
        {
            if(restored)return;restored=true;
            if(driver){driver.dioRoot.transform.localPosition=position;driver.dioRoot.transform.localRotation=rotation;driver.SuppressStand=savedSuppress;}
            if(arms)Destroy(arms);if(companion)Destroy(companion);
        }
        private void OnDisable()=>Restore();
        private void OnDestroy()=>Restore();
    }
}
