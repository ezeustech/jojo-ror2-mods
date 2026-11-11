using UnityEngine;
using RoR2;
using R2API;
namespace Diavolo
{
    public sealed class DiavoloSelection : MonoBehaviour
    {
        private GameObject actor,stand; private Animation actorAnimation,standAnimation;
        private EohHairPose hair; private EohMaterials materials=new EohMaterials(), actorMaterials=new EohMaterials();
        private CharacterSelectSurvivorPreviewDisplayController preview;private Loadout loadout;private uint skin;private float nextSkinCheck;
        public static GameObject Create(GameObject body)
        {
            GameObject display=PrefabAPI.InstantiateClone(LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterDisplays/MercDisplay"),"DiavoloEOHDisplay",false);
            var preview=display.GetComponent<CharacterSelectSurvivorPreviewDisplayController>();if(!preview)preview=display.AddComponent<CharacterSelectSurvivorPreviewDisplayController>();
            preview.bodyPrefab=body;preview.skillChangeResponses=new CharacterSelectSurvivorPreviewDisplayController.SkillChangeResponse[0];preview.skinChangeResponses=new CharacterSelectSurvivorPreviewDisplayController.SkinChangeResponse[0];
            display.AddComponent<DiavoloSelection>();return display;
        }
        private void Start()
        {
            foreach(Renderer r in GetComponentsInChildren<Renderer>(true))r.forceRenderingOff=true;
            foreach(Animator a in GetComponentsInChildren<Animator>(true))a.enabled=false;
            preview=GetComponent<CharacterSelectSurvivorPreviewDisplayController>();loadout=Loadout.RequestInstance();
            actor=CreateModel("5dvl01",new Vector3(-.55f,0,.35f),1.2f);
            stand=CreateModel("5kcr01",new Vector3(1.2f,.1f,-1.55f),1.05f);
            if(actor){actorAnimation=actor.GetComponentInChildren<Animation>();hair=new EohHairPose(actor);}
            if(stand)standAnimation=stand.GetComponentInChildren<Animation>();
        }
        private GameObject CreateModel(string code,Vector3 position,float scale)
        {
            GameObject prefab=DiavoloPresentation.Asset<GameObject>("Assets/Generated/"+code+"Visual.prefab");if(!prefab)return null;
            GameObject result=Instantiate(prefab,transform,false);result.transform.localPosition=position;result.transform.localScale*=scale;
            (code=="5kcr01" ? materials : actorMaterials).Apply(result,DiavoloPresentation.Asset<Shader>("Assets/CrimsonModel.shader"));
            foreach(SkinnedMeshRenderer skin in result.GetComponentsInChildren<SkinnedMeshRenderer>())skin.gameObject.AddComponent<EohSkinning>();
            Animation anim=result.GetComponentInChildren<Animation>();if(anim){anim.Stop();anim.enabled=false;}
            return result;
        }
        private void LateUpdate()
        {
            if(Time.unscaledTime>=nextSkinCheck && preview && preview.networkUser){nextSkinCheck=Time.unscaledTime+.15f;
                preview.networkUser.networkLoadout.CopyLoadout(loadout);uint chosen=loadout.bodyLoadoutManager.GetSkinIndex(BodyCatalog.FindBodyIndex(preview.bodyPrefab));
                if(chosen!=skin){skin=chosen;if(actor){actor.SetActive(false);Destroy(actor);}actorMaterials.Dispose();
                    actor=CreateModel(skin==1 ? "5dvl11" : "5dvl01",new Vector3(-.55f,0,.35f),1.2f);actorAnimation=actor ? actor.GetComponentInChildren<Animation>() : null;hair=actor && skin!=1 ? new EohHairPose(actor) : null;
                }
            }
            Pose(actorAnimation,"_bas_jojo_02_lp");Pose(standAnimation,"_bas_s_nut");
            if(hair!=null)hair.Apply(-transform.forward);
        }
        private static void Pose(Animation anim,string suffix)
        {
            if(!anim)return;
            foreach(AnimationState state in anim)if(state.name.EndsWith(suffix)){state.clip.SampleAnimation(anim.gameObject,Mathf.Repeat(Time.time,state.length));break;}
        }
        private void OnDestroy(){materials.Dispose();actorMaterials.Dispose();if(loadout!=null)loadout=Loadout.ReturnInstance(loadout);}
    }
}
