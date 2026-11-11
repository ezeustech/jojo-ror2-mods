using RoR2;
using UnityEngine;
namespace EyesOfHeavenDio
{
    internal static class DioInput
    {
        internal static bool IsLocal(CharacterBody body)
        { if(!body)return false;foreach(var user in LocalUserManager.readOnlyLocalUsersList)if(user.cachedBody==body)return true;return false; }
        internal static bool Allowed(CharacterBody body)
        {
            if(!body||!Run.instance||!Application.isFocused||body.allSkillsDisabled||Time.timeScale==0)return false;
            foreach(var user in LocalUserManager.readOnlyLocalUsersList)
                if(user.cachedBody==body)return !user.pauseScreenController&&(!user.eventSystem||!user.eventSystem.isCursorVisible);
            return false;
        }
        internal static bool Pressed(KeyCode key,int move)=>ExtraGamepad.KeyDown(key)||ExtraGamepad.Pressed(DioPlugin.GamepadMoves[move].Value);
        internal static bool Held(KeyCode key,int move)=>ExtraGamepad.KeyHeld(key)||ExtraGamepad.Held(DioPlugin.GamepadMoves[move].Value);
    }
}
