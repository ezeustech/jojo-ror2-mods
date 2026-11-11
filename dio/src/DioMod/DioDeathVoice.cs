using RoR2;
using UnityEngine;
namespace EyesOfHeavenDio
{
    public sealed class DioDeathVoice : MonoBehaviour
    {
        private CharacterBody body;
        private bool played;
        private void Awake()=>body=GetComponent<CharacterBody>();
        private void OnEnable()=>GlobalEventManager.onCharacterDeathServer+=OnDeath;
        private void OnDisable()=>GlobalEventManager.onCharacterDeathServer-=OnDeath;
        private void OnDeath(DamageReport report)
        {
            if(report.victimBody==body)PlayOnce();
        }
        private void Update()
        {
            // Also covers deaths replicated to a locally controlled body.
            if(body&&body.healthComponent&&!body.healthComponent.alive)PlayOnce();
        }
        private void PlayOnce()
        {
            if(played||!body||!UnityEngine.Networking.NetworkServer.active)return;
            played=true;
            // The audio object is unparented, so body destruction does not cut it off.
            DioFeedback.ServerSound(gameObject,"death-voice",1f);
        }
    }
}
