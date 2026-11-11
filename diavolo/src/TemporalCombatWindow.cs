namespace Diavolo
{
    // Server-owned combat state, independent of frame rate and presentation.
    public sealed class TemporalCombatWindow
    {
        private bool ambush;
        private float dodgeUntil;
        public void BeginErase() { ambush = false; }
        public void EndErase(bool alive) { ambush = alive; }
        public void BeginEpitaph(float now, float duration) { dodgeUntil = now + duration; }
        public bool Dodging(float now, bool alive) { return alive && now < dodgeUntil; }
        public bool TakeAmbush(bool erasing, bool alive)
        {
            if (erasing || !alive || !ambush) return false;
            ambush = false; return true;
        }
        public void RestoreAmbush() { ambush = true; }
    }
}
