using System;
using System.Collections.Generic;
using UnityEngine;

namespace EyesOfHeavenDio
{
    internal static class DioUi
    {
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        internal static void Load(AssetBundle bundle)
        {
            foreach (var name in new[] { "DioPortrait", "DioCard", "Combo", "Knives", "TimeSkip", "TimeStop", "Barrage", "BloodDrain", "StandJump", "RoadRoller", "ExKnives", "StandBlock" })
            {
                var texture = bundle.LoadAsset<Texture2D>("Assets/UI/" + name + ".png");
                if (!texture) throw new InvalidOperationException("Missing DIO UI asset: " + name);
                textures[name] = texture;
                sprites[name] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
                sprites[name].name = "Dio" + name + "Icon";
            }
            DioPlugin.Log.LogInfo("DIO UI loaded: 2 portraits and 10 custom skill icons.");
        }
        internal static Texture2D Texture(string name) => textures.TryGetValue(name, out var texture) ? texture : null;
        internal static Sprite Icon(string name) => sprites.TryGetValue(name, out var sprite) ? sprite : null;
        internal static Sprite SkillIcon(Type state)
        {
            if (state == typeof(DioPunch)) return Icon("Combo");
            if (state == typeof(DioKnives)) return Icon("Knives");
            if (state == typeof(DioDash)) return Icon("TimeSkip");
            if (state == typeof(DioTimeStop)) return Icon("TimeStop");
            throw new ArgumentException("No DIO icon registered for " + state.Name);
        }
    }
}
