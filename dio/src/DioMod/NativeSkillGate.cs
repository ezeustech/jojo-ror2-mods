using System;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RoR2;
namespace EyesOfHeavenDio
{
    internal static class NativeSkillGate
    {
        private static Action<PlayerCharacterMasterController> suppress;
        internal static void Install() {
            suppress=BuildSuppressor<PlayerCharacterMasterController>();
            var harmony=new Harmony("local.eyesofheaven.dio.controller");
            harmony.Patch(AccessTools.Method(typeof(PlayerCharacterMasterController),"PollInputToButtonBuffers"),postfix:new HarmonyMethod(typeof(NativeSkillGate),"Filter"));
            harmony.Patch(AccessTools.Method(typeof(PlayerCharacterMasterController),"PollButtonInput"),prefix:new HarmonyMethod(typeof(NativeSkillGate),"Filter"));
        }
        // Use the game's own until-release latch. Clearing InputBank afterward would
        // leave buffered presses, causing an unwanted attack when R3 was released.
        internal static Action<T> BuildSuppressor<T>() {
            var buffers=typeof(T).GetField("buttonBuffers",BindingFlags.Instance|BindingFlags.NonPublic);
            if(buffers==null)throw new MissingFieldException(typeof(T).FullName,"buttonBuffers");
            var method=new DynamicMethod("DiavoloSuppressNativeSkills",typeof(void),new[]{typeof(T)},typeof(NativeSkillGate),true);
            var il=method.GetILGenerator();
            foreach(string name in new[]{"skill1","skill2","skill3","skill4","jump","activateEquipment","ping","sprint"}) {
                var field=buffers.FieldType.GetField(name);
                if(field==null)throw new MissingFieldException(buffers.FieldType.FullName,name);
                var stop=field.FieldType.GetMethod("SuppressUntilReleased",BindingFlags.Public|BindingFlags.Instance);
                if(stop==null)throw new MissingMethodException(field.FieldType.FullName,"SuppressUntilReleased");
                il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldflda,buffers);il.Emit(OpCodes.Ldflda,field);il.Emit(OpCodes.Call,stop);
            }
            il.Emit(OpCodes.Ret);
            return (Action<T>)method.CreateDelegate(typeof(Action<T>));
        }
        private static void Filter(PlayerCharacterMasterController __instance,CharacterBody ___body) {
            if(!___body || !___body.hasEffectiveAuthority || !___body.GetComponent<DioExtraMoves>() || !ExtraGamepad.ModifierHeld)return;
            foreach(LocalUser user in LocalUserManager.readOnlyLocalUsersList)
                if(user.cachedBody==___body){suppress(__instance);return;}
        }
    }
}
