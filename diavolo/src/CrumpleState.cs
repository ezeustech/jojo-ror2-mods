using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace Diavolo
{
    public sealed class CrumpleState : BaseState
    {
        public float Duration = 1f;
        private Transform model;
        private Quaternion originalRotation;
        private Vector3 originalPosition;
        private bool authoredAnimation;
        public override void OnEnter()
        {
            base.OnEnter(); model = GetModelTransform();
            if (model) { originalRotation = model.localRotation; originalPosition = model.localPosition; }
            Animator animator = GetModelAnimator();
            if (animator) foreach (string name in new string[] { "Hurt", "Stun", "Stunned" }) {
                int hash = Animator.StringToHash(name);
                if (animator.HasState(0, hash)) { animator.CrossFade(hash, .05f, 0); authoredAnimation = true; break; }
            }
        }
        public override void Update()
        {
            base.Update();
            if (model && !authoredAnimation) {
                float slump = Mathf.Min(Mathf.Clamp01(age/.2f), Mathf.Clamp01((Duration-age)/.2f));
                model.localRotation = originalRotation * Quaternion.Euler(55f*slump, 0, 0);
                model.localPosition = originalPosition - Vector3.up * .35f * slump;
            }
        }
        public override void FixedUpdate() { base.FixedUpdate(); if (isAuthority && fixedAge >= Duration) outer.SetNextStateToMain(); }
        public override void OnExit() { if (model && !authoredAnimation) { model.localRotation = originalRotation; model.localPosition = originalPosition; } base.OnExit(); }
        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Frozen; }
        public override void OnSerialize(NetworkWriter writer) { base.OnSerialize(writer); writer.Write(Duration); }
        public override void OnDeserialize(NetworkReader reader) { base.OnDeserialize(reader); Duration = reader.ReadSingle(); }
    }
}
