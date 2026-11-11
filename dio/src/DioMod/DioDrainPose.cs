using UnityEngine;
using RoR2;
namespace EyesOfHeavenDio
{
    [DefaultExecutionOrder(50)]
    public sealed class DioDrainPose : MonoBehaviour
    {
        private CharacterBody victim;
        private DioGrabHold hold;
        private Transform space, hand, root;
        private Animation animation;
        private AnimationClip contact;
        private Quaternion rotation, localRotation;
        private bool wasEnabled;
        public void Begin(CharacterBody target)
        {
            victim=target;hold=target.GetComponent<DioGrabHold>();
            root=GetComponent<DioVisualDriver>().dioRoot.transform;
            rotation=root.rotation;localRotation=root.localRotation;
            animation=root.GetComponentInChildren<Animation>(true);
            if(animation)
            {
                wasEnabled=animation.enabled;
                foreach(AnimationState state in animation)if(state.name.EndsWith("_atk_l2")){contact=state.clip;break;}
                if(contact)animation.enabled=false;
            }
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if(skin.name.EndsWith(" body")){space=skin.transform;break;}
            foreach(var bone in root.GetComponentsInChildren<Transform>(true))
                if(bone.name.EndsWith(" l hand")){hand=bone;break;}
            LateUpdate();
        }
        private void LateUpdate()
        {
            if(!victim||!victim.healthComponent.alive||!hand||!contact||!animation)return;
            // This is the middle of the punch's measured contact window, not its recovery tail.
            contact.SampleAnimation(animation.gameObject,contact.length*.25f);
            root.rotation=rotation;
            if(hold)hold.SetContact(DioPoseSolver.VisualPosition(space,hand));
        }
        private void OnDisable()
        {
            if(animation)animation.enabled=wasEnabled;
            if(root)root.localRotation=localRotation;
            GetComponent<DioVisualDriver>()?.Action("_bas_nut",.15f,false);
        }
    }
}
