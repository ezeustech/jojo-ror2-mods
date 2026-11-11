using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EyesOfHeavenDio
{
    // Owns only the components it suspends; cleanup also handles interruption/death.
    [DefaultExecutionOrder(32001)]
    public sealed class DioGrabHold : MonoBehaviour
    {
        public CharacterBody Victim { get; private set; }
        private CharacterBody captor;
        private Vector3 direction;
        private float expires;
        private bool released, stationary;
        private Vector3 anchor;
        private bool hasContact;
        private Vector3 contact;
        public void SetContact(Vector3 point){hasContact=true;contact=point;}
        private readonly HashSet<Behaviour> paused = new HashSet<Behaviour>();
        private readonly List<Action> restore = new List<Action>();
        public void Begin(CharacterBody owner, CharacterBody victim, Vector3 aim, bool stayInPlace = false, float duration = 2f)
        {
            captor = owner; Victim = victim; direction = aim.normalized; expires = Time.time + duration; stationary = stayInPlace; anchor = victim.transform.position;
            Suspend();
        }
        private void Pause(Behaviour component)
        {
            if (!component || !component.enabled) return;
            component.enabled = false;
            if (paused.Add(component)) restore.Add(() => { if (component) component.enabled = true; });
        }
        private void Suspend()
        {
            foreach (var machine in Victim.GetComponents<EntityStateMachine>()) Pause(machine);
            {
                var root=Victim.modelLocator && Victim.modelLocator.modelTransform ? Victim.modelLocator.modelTransform : Victim.transform;
                foreach(var animation in root.GetComponentsInChildren<Animation>(true))Pause(animation);
                foreach(var animator in root.GetComponentsInChildren<Animator>(true))Pause(animator);
            }
            Pause(Victim.characterMotor);
            Pause(Victim.GetComponent<CharacterDirection>());
            Pause(Victim.GetComponent<RigidbodyMotor>());
            if (Victim.master) foreach (var ai in Victim.master.GetComponents<RoR2.CharacterAI.BaseAI>()) Pause(ai);
        }
        private void LateUpdate()
        {
            if (released) return;
            if (!captor || !Victim || !captor.healthComponent.alive || !Victim.healthComponent.alive || Time.time >= expires)
            { Release(); Destroy(this); return; }
            Suspend(); // A time stop ending during the grab may re-enable components.
            Vector3 origin = captor.corePosition;
            float radius = Mathf.Max(.4f, Victim.radius);
            float reach = Mathf.Max(radius+.75f, captor.radius + radius + .1f);
            if (Physics.SphereCast(origin, radius, direction, out var hit, reach, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                reach = Mathf.Max(0, hit.distance - .1f);
            Vector3 position = origin + direction * reach + (Victim.transform.position - Victim.corePosition);
            if(hasContact&&!stationary)position=contact+(Victim.transform.position-DioSpecialGeometry.TargetBounds(Victim).center);
            Vector3 displacement = position - Victim.transform.position;
            if (displacement.sqrMagnitude > .0001f && Physics.SphereCast(Victim.corePosition, radius,
                displacement.normalized, out var obstacle, displacement.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                position = Victim.transform.position + displacement.normalized * Mathf.Max(0, obstacle.distance - .1f);
            if (stationary) position = anchor;
            if (Victim.characterMotor)
            {
                Victim.characterMotor.velocity = Vector3.zero;
                Victim.characterMotor.Motor.SetPosition(position);
            }
            var rb = Victim.GetComponent<Rigidbody>();
            if (rb)
            {
                if (!rb.isKinematic) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                rb.position = position;
            }
            Victim.transform.position = position;
            DioTimeWorld.UpdateHeldPosition(Victim.transform);
        }
        public void Release()
        {
            if (released) return;
            released = true;
            var actions = restore.ToArray();
            Action cleanup = () => { foreach (var action in actions) action(); };
            // Keep a released enemy suspended if the world is still stopped.
            if (!DioTimeWorld.RestoreAfterStop(gameObject, cleanup)) cleanup();
            restore.Clear(); paused.Clear();
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
