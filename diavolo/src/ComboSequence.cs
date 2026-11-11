namespace Diavolo
{
    public sealed class ComboSequence
    {
        private int next;
        private float expires;
        public int Take(float now) { if (now > expires) next = 0; int step = next; next = (next + 1) % 5; expires = now + 10f; return step; }
        public void Finish(float now, bool landedSwing) { if (!landedSwing) next = 0; expires = now + .85f; }
    }
}
