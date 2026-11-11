namespace Diavolo
{
    public enum TemporalAttack { Ordinary, Blood, Impale }
    public enum AttackMoment { Start, PreContact, Contact }
    public static class CombatRules
    {
        public static bool EndsErasure(TemporalAttack attack, AttackMoment moment)
        {
            if (attack == TemporalAttack.Blood) return false;
            if (attack == TemporalAttack.Impale) return moment == AttackMoment.PreContact || moment == AttackMoment.Contact;
            return true;
        }
        public static bool FullyCharged(float seconds) { return seconds >= 1.8f; }
        public static bool CounterUsesChop(float ownerHeight, float targetHeight) { return targetHeight > ownerHeight * 1.5f; }
    }
}
