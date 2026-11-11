namespace EyesOfHeavenDio
{
    internal static class DioGuardMath
    {
        internal const float DamageMultiplier = .1f;
        internal static bool InFront(float forwardX, float forwardZ, float sourceX, float sourceZ)
        {
            float length = (float)System.Math.Sqrt((forwardX*forwardX+forwardZ*forwardZ)*(sourceX*sourceX+sourceZ*sourceZ));
            return length > .0001f && (forwardX*sourceX+forwardZ*sourceZ)/length >= .5f;
        }
    }
}
