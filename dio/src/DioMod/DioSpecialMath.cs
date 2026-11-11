using System;
namespace EyesOfHeavenDio
{
    // Intersect a ray with the enclosing rectangle, after expanding both half-extents for knife clearance.
    // A simple ellipse with the same half-extents cuts through the corners.
    internal static class DioSpecialMath
    {
        internal const float RollerLeap = .3f;
        internal const float RollerVoicePunchStart = 6.07f; // 1:39.62 - 1:33.55
        internal const float RollerVoiceFinisher = 12.11f; // 1:45.66 - 1:33.55
        internal const float RollerHidden = RollerVoicePunchStart - RollerLeap - RollerDrop - .25f;
        internal static float RollerDropHeight(float originY, float groundY) => Math.Max(24f, originY - groundY + 24f);
        internal const float RollerPunch = RollerVoiceFinisher + .25f - RollerDrop;
        internal const float RollerEscape = .65f;
        internal const float ExDash = .6f;
        internal const int ExKnifeCount = 30;
        internal const float ExThrowInterval = .045f;
        internal const float ExReleaseInterval = .025f;
        internal const float ExRecovery = 2.65f;
        internal const float RollerDrop = .5f;
        internal const float ExFreeze = 1.5f;
        internal static (float x, float z) KnifeOffset(float angle, float halfWidth, float halfDepth)
        {
            double x = Math.Cos(angle), z = Math.Sin(angle);
            double edge = 1 / Math.Max(Math.Abs(x) / (Math.Max(.1, halfWidth) + 2), Math.Abs(z) / (Math.Max(.1, halfDepth) + 2));
            return ((float)(x * (edge + .5)), (float)(z * (edge + .5)));
        }
    }
}
