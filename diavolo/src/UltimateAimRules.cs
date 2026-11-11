using System;
namespace Diavolo
{
    public static class UltimateAimRules
    {
        public const float Range = 40f;
        public static double Score(float angleDegrees, float distance, float radius)
        {
            if (distance <= 0 || distance - radius > Range || angleDegrees >= 90f) return double.PositiveInfinity;
            double silhouette = Math.Asin(Math.Min(1, Math.Max(0, radius) / distance)) * 180 / Math.PI;
            double miss = Math.Max(0, angleDegrees - silhouette);
            if (miss > 30) return double.PositiveInfinity;
            return miss * 100 + distance;
        }
    }
}
