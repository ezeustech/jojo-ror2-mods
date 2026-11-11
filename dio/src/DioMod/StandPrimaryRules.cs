using System;

namespace LocalStandCombat
{
    internal static class StandPrimaryRules
    {
        internal const float Range = 25f;
        internal const float AimAngle = 32f;
        internal const float Standoff = 1.3f;
        internal const float MaxTravelSpeed = 34f;
        internal const float Acceleration = 100f;
        internal const float ComboGrace = .18f;

        internal static bool InRange(float distanceSquared)
        {
            return distanceSquared >= 0f && distanceSquared <= Range * Range;
        }

        // A fast attack can cross its whole contact window in one physics tick.
        internal static bool CrossesContact(float previous, float current, float duration, float start, float end)
        {
            return duration > 0f && current >= previous && current >= duration * start && previous < duration * end;
        }

        internal static float TravelSpeed(float distance)
        {
            // Ease into the stop. Attack speed must never turn travel into a teleport.
            return Math.Min(MaxTravelSpeed, Math.Max(3f, distance * 12f));
        }

        internal static float AnimationSpeed(float attackSpeed)
        { return Math.Max(.4f, Math.Min(2f, (float)Math.Sqrt(Math.Max(.1f, attackSpeed)))); }
    }

    // The same clock drives contact and the rendered pose. Long-range windup
    // waits at an anticipation pose; it never consumes the hit while travelling.
    internal sealed class StandAttackClock
    {
        internal float Age { get; private set; }
        internal float Previous { get; private set; }
        internal float Waited { get; private set; }
        internal bool TimedOut { get { return Waited >= 1.5f; } }
        internal float Advance(float delta, float contact, bool tracking, bool arrived)
        {
            Previous = Age;
            float next = Age + Math.Max(0f, delta);
            if (tracking && !arrived && Age < contact) {
                float gate = Math.Max(Age, contact * .55f);
                float held = Math.Min(next, gate);
                Waited += next - held;
                Age = held;
            } else Age = next;
            return Age;
        }
    }
}
