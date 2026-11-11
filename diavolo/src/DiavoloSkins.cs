using UnityEngine;
using RoR2;
using R2API;
namespace Diavolo
{
    public static class DiavoloSkins
    {
        public static void Register(GameObject body){
            ModelLocator locator=body.GetComponent<ModelLocator>();if(!locator || !locator.modelTransform)return;
            GameObject model=locator.modelTransform.gameObject;var controller=model.GetComponent<ModelSkinController>();if(!controller)controller=model.AddComponent<ModelSkinController>();
            var character=model.GetComponent<CharacterModel>();if(!character)return;
            controller.skins=new[]{Make(model,character,"DIAVOLO_SKIN_DEFAULT","Diavolo",false),Make(model,character,"DIAVOLO_SKIN_BOSS","The Boss",true)};
        }
        private static SkinDef Make(GameObject root,CharacterModel model,string token,string title,bool boss){
            var def=ScriptableObject.CreateInstance<SkinDef>();def.name=token;def.nameToken=token;def.rootObject=root;def.baseSkins=new SkinDef[0];
            def.unlockableDef=null;
            def.icon=DiavoloPresentation.Asset<Sprite>("Assets/Generated/Icons/"+(boss?"COUNTER":"EPITAPH")+".png");
            var parameters=ScriptableObject.CreateInstance<SkinDefParams>();parameters.name=token+"Params";
            parameters.rendererInfos=model.baseRendererInfos;parameters.gameObjectActivations=new SkinDefParams.GameObjectActivation[0];parameters.meshReplacements=new SkinDefParams.MeshReplacement[0];parameters.projectileGhostReplacements=new SkinDefParams.ProjectileGhostReplacement[0];parameters.minionSkinReplacements=new SkinDefParams.MinionSkinReplacement[0];parameters.lightReplacements=new CharacterModel.LightInfo[0];
            def.skinDefParams=parameters;LanguageAPI.Add(token,title);return def;
        }
    }
}
