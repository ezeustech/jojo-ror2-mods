using UnityEngine;

namespace Diavolo
{
    // Shared by runtime placement and the synthetic floor/wall/cliff editor checks.
    public static class FatePlacementGeometry
    {
        public static bool Ground(Vector3 nearFeet, float above, float below, int mask, out Vector3 foot)
        {
            foot = nearFeet;
            RaycastHit hit;
            if (!Physics.Raycast(nearFeet + Vector3.up * above, Vector3.down, out hit, above + below, mask, QueryTriggerInteraction.Ignore) || hit.normal.y < .72f) return false;
            foot = hit.point + Vector3.up * .04f;
            return true;
        }

        public static bool Fits(Vector3 foot, float radius, float height, int mask)
        {
            radius = Mathf.Max(.2f, radius); height = Mathf.Max(radius * 2, height);
            if (Physics.CheckCapsule(foot + Vector3.up * radius, foot + Vector3.up * (height - radius), radius * .92f, mask, QueryTriggerInteraction.Ignore)) return false;
            // A center ray alone permits big enemies to hang over cliffs.
            for (int i = 0; i < 8; i++) {
                float angle = i * Mathf.PI / 4;
                Vector3 edge = foot + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius * .9f;
                Vector3 support;
                if (!Ground(edge, .65f, .8f, mask, out support) || Mathf.Abs(support.y - foot.y) > .65f) return false;
            }
            return true;
        }

        public static bool Connected(Vector3 from, Vector3 to, float radius, float height, int mask)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to)));
            Vector3 previous = from;
            for (int step = 1; step <= steps; step++) {
                Vector3 next;
                if (!Ground(Vector3.Lerp(from, to, (float)step / steps), 1.25f, 1.25f, mask, out next) || Mathf.Abs(next.y - previous.y) > 1f) return false;
                if (!Fits(next, radius, height, mask)) return false;
                Vector3 delta = next - previous;
                // Sweep the full body, including thin walls that fall between floor probes.
                if (delta.sqrMagnitude > .0001f && Physics.CapsuleCast(previous + Vector3.up * (radius + .15f), previous + Vector3.up * (Mathf.Max(height, radius * 2) - radius + .15f), radius * .9f, delta.normalized, delta.magnitude, mask, QueryTriggerInteraction.Ignore)) return false;
                previous = next;
            }
            return Vector3.Distance(previous, to) < .2f;
        }
    }
}
