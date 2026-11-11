using RoR2;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.CharacterAI;
namespace Diavolo
{
    // Uses the actual survivor skills. The global damage hook rejects every hit
    // originating from this body; no copied inventory or placeholder Merc attacks.
    public sealed class TimeEraseDecoy : MonoBehaviour
    {
        private static GameObject masterPrefab;
        private CharacterBody body;
        private EntityStateMachine weapon;
        private GenericSkill[] skills;
        private float nextAttack;
        private int nextMove;
        private static readonly string[] moves={"CHAIN","CHAIN","BARRAGE","IMPALE","CHOP","SMASH","BLOOD","COUNTER","SKIP","EPITAPH","ULTIMATE"};
        internal static void Register(GameObject originalBody)
        {
            GameObject originalMaster=LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/MercMonsterMaster");
            if(!originalMaster){Debug.LogError("Diavolo decoy: MercMonsterMaster unavailable.");return;}
            GameObject decoy=PrefabAPI.InstantiateClone(originalBody,"DiavoloDecoyBody",true);
            ExtraInputs inputs=decoy.GetComponent<ExtraInputs>();if(inputs)Object.DestroyImmediate(inputs);
            CharacterBody cb=decoy.GetComponent<CharacterBody>();
            cb.baseNameToken="DIAVOLO_DECOY";cb.baseMaxHealth=1000f;cb.baseRegen=0;cb.baseDamage=0;cb.levelDamage=0;cb.baseMoveSpeed=9f;
            decoy.AddComponent<TimeEraseDecoy>();
            DeathRewards rewards=decoy.GetComponent<DeathRewards>();if(rewards){rewards.goldReward=0;rewards.expReward=0;}
            ContentAddition.AddBody(decoy);
            masterPrefab=PrefabAPI.InstantiateClone(originalMaster,"DiavoloDecoyMaster",true);
            foreach(AISkillDriver driver in masterPrefab.GetComponents<AISkillDriver>())Object.DestroyImmediate(driver);
            foreach(BaseAI ai in masterPrefab.GetComponents<BaseAI>())Object.DestroyImmediate(ai);
            masterPrefab.GetComponent<CharacterMaster>().bodyPrefab=decoy;
            ContentAddition.AddMaster(masterPrefab);LanguageAPI.Add("DIAVOLO_DECOY","Diavolo's Afterimage");
        }
        internal static CharacterMaster Spawn(CharacterBody owner)
        {
            if(!NetworkServer.active || !masterPrefab || !owner)return null;
            return new MasterSummon {masterPrefab=masterPrefab,position=owner.transform.position,rotation=owner.transform.rotation,summonerBodyObject=owner.gameObject,teamIndexOverride=owner.teamComponent.teamIndex,ignoreTeamMemberLimit=true}.Perform();
        }
        internal static void Remove(CharacterMaster master)
        {
            if(!NetworkServer.active || !master)return;
            CharacterBody decoy=master.GetBody();if(decoy)NetworkServer.Destroy(decoy.gameObject);NetworkServer.Destroy(master.gameObject);
        }
        private void Start()
        {
            body=GetComponent<CharacterBody>();skills=GetComponents<GenericSkill>();
            foreach(EntityStateMachine machine in GetComponents<EntityStateMachine>())if(machine.customName=="Weapon")weapon=machine;
            if(NetworkServer.active)body.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
        }
        private void FixedUpdate()
        {
            if(!NetworkServer.active || !body || !body.inputBank || !body.healthComponent.alive)return;
            CharacterBody target=null;float nearest=60f;
            foreach(CharacterBody enemy in CharacterBody.readOnlyInstancesList) {
                if(!TemporalController.IsEnemy(body,enemy))continue;
                float distance=Vector3.Distance(body.corePosition,enemy.corePosition);
                if(distance<nearest){nearest=distance;target=enemy;}
            }
            if(!target){body.inputBank.moveVector=Vector3.zero;return;}
            Vector3 delta=target.corePosition-body.corePosition;
            body.inputBank.aimDirection=delta.normalized;
            Vector3 heading=Vector3.ProjectOnPlane(delta,Vector3.up).normalized;
            body.inputBank.moveVector=nearest>5f ? heading : Vector3.zero;
            if(body.characterDirection)body.characterDirection.forward=heading;
            // Hold charge for the real Impale state; it auto-releases at two seconds.
            body.inputBank.skill2.PushState(weapon && weapon.state is Impale);
            if(Time.time<nextAttack || (weapon && weapon.state is EntityStates.BaseSkillState))return;
            if(Physics.Linecast(body.corePosition,target.corePosition,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))return;
            for(int attempt=0;attempt<moves.Length;attempt++) {
                string move=moves[nextMove++%moves.Length];
                if(nearest>10f && move!="SKIP" && move!="BLOOD" && move!="ULTIMATE")continue;
                foreach(GenericSkill skill in skills)if(skill.skillName==move && skill.IsReady()) {
                    skill.ExecuteIfReady();nextAttack=Time.time+.4f;return;
                }
            }
        }
    }
}
