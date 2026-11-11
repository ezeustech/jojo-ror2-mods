using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace Diavolo
{
    // A forced control lock, including enemies immune to RoR2's ordinary stun.
    public sealed class BloodBlind : MonoBehaviour
    {
        private CharacterBody body;
        private float until;
        private Vector3 position;
        private Quaternion rotation;
        private bool oldSkills;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private Rigidbody rigid;
        private bool wasKinematic;
        private bool initialized;
        private bool restored;

        public static void Lock(CharacterBody body, float duration, Vector3 position, Quaternion rotation)
        {
            if (!NetworkServer.active || !body || body.isPlayerControlled) return;
            BloodBlind blind = body.GetComponent<BloodBlind>();
            if (!blind) blind = body.gameObject.AddComponent<BloodBlind>();
            blind.body = body; blind.position = position; blind.rotation = rotation;
            blind.until = Mathf.Max(blind.until, Time.time + duration);
            if (!blind.initialized) blind.Suspend();
        }
        private void Pause(Behaviour component) { if (component && component.enabled) { suspended.Add(component); component.enabled = false; } }
        private void Suspend()
        {
            initialized = true; oldSkills = body.allSkillsDisabled; body.allSkillsDisabled = true;
            if (body.master) foreach (BaseAI ai in body.master.GetComponents<BaseAI>()) { ai.currentEnemy.Reset(); ai.customTarget.Reset(); Pause(ai); }
            foreach (EntityStateMachine machine in body.GetComponents<EntityStateMachine>()) machine.SetInterruptState(new Idle(), InterruptPriority.Death);
            if (body.characterMotor) { body.characterMotor.velocity = Vector3.zero; Pause(body.characterMotor); }
            foreach (Behaviour component in body.GetComponents<Behaviour>()) if (component is RigidbodyMotor || component.GetType().Namespace == "KinematicCharacterController") Pause(component);
            Pause(body.characterDirection);
            rigid = body.GetComponent<Rigidbody>();
            if (rigid) { wasKinematic = rigid.isKinematic; rigid.isKinematic = true; }
            // Preserve unrelated disable states; restore only components we suspended.
        }
        private void FixedUpdate()
        {
            if (!initialized) return;
            if (!body || !body.healthComponent || !body.healthComponent.alive || Time.time >= until) { Destroy(this); return; }
            body.transform.SetPositionAndRotation(position, rotation);
            if(body.characterMotor && body.characterMotor.Motor)body.characterMotor.Motor.SetPositionAndRotation(position,rotation);
            if (body.characterMotor) body.characterMotor.velocity = Vector3.zero;
            if (body.inputBank) { body.inputBank.moveVector = Vector3.zero; body.inputBank.skill1.PushState(false); body.inputBank.skill2.PushState(false); body.inputBank.skill3.PushState(false); body.inputBank.skill4.PushState(false); }
        }
        public void Release(Vector3 launchVelocity)
        {
            Restore();
            if (body && body.characterMotor) body.characterMotor.velocity = launchVelocity;
            else if (rigid && !rigid.isKinematic) rigid.velocity = launchVelocity;
            Destroy(this);
        }
        private void OnDestroy() { Restore(); }
        private void Restore()
        {
            if (!initialized || restored) return;
            restored = true;
            if(body && body.characterMotor && body.characterMotor.Motor)body.characterMotor.Motor.SetPositionAndRotation(position,rotation);
            foreach (Behaviour component in suspended) if (component) component.enabled = true;
            if (rigid) rigid.isKinematic = wasKinematic;
            if (body) {
                body.allSkillsDisabled = oldSkills;
                if (body.healthComponent && body.healthComponent.alive) foreach (EntityStateMachine machine in body.GetComponents<EntityStateMachine>()) machine.SetNextStateToMain();
            }
        }
    }
}
