using System;
using Diavolo;
public static class CombatRulesTests
{
    private static int checks;
    private static void Check(bool value, string scenario) { checks++; if (!value) throw new Exception(scenario); }
    public static void Main()
    {
        Check(UltimateAimRules.Score(0,35,1) < UltimateAimRules.Score(10,5,0.5f), "Crosshair target outranks a closer off-axis enemy");
        Check(!double.IsInfinity(UltimateAimRules.Score(29,35,1)), "Wide assist cone acquires near-miss targets");
        Check(double.IsInfinity(UltimateAimRules.Score(0,50,1)), "Acquisition range stays bounded");
        Check(double.IsInfinity(UltimateAimRules.Score(120,5,1)), "Cannot acquire targets behind the player");
        Check(UltimateAimRules.Score(10,30,8) < UltimateAimRules.Score(10,30,1), "Large enemy silhouettes count toward aim assistance");
        Check(CombatRules.EndsErasure(TemporalAttack.Impale, AttackMoment.PreContact), "Impale returns before damage so fate colliders settle");
        Check(!CombatRules.EndsErasure(TemporalAttack.Blood, AttackMoment.PreContact), "Blood must preserve erasure even before contact");
        Check(!CombatRules.EndsErasure(TemporalAttack.Blood, AttackMoment.Start), "Blood windup must preserve erasure");
        Check(!CombatRules.EndsErasure(TemporalAttack.Blood, AttackMoment.Contact), "Blood contact must preserve erasure");
        Check(!CombatRules.EndsErasure(TemporalAttack.Impale, AttackMoment.Start), "Impale charge/windup must preserve erasure");
        Check(CombatRules.EndsErasure(TemporalAttack.Impale, AttackMoment.Contact), "Impale must resolve fate at contact");
        Check(CombatRules.EndsErasure(TemporalAttack.Ordinary, AttackMoment.Start), "Chop/barrage/grab must resolve fate at attack start");
        Check(!CombatRules.FullyCharged(1.79f), "Early release must be uncharged");
        Check(CombatRules.FullyCharged(1.8f), "Full charge boundary");
        Check(!CombatRules.CounterUsesChop(2f, 2.9f), "Slightly larger enemies receive impale");
        Check(!CombatRules.CounterUsesChop(2f, 3f), "Threshold enemy receives impale");
        Check(CombatRules.CounterUsesChop(2f, 3.1f), "Large enemies receive chop");
        var window = new TemporalCombatWindow();
        Check(!window.TakeAmbush(false, true), "No bonus before erasure");
        window.BeginErase(); window.EndErase(true);
        Check(!window.TakeAmbush(true, true), "Bonus cannot apply inside erasure");
        Check(window.TakeAmbush(false, true), "First hit after erasure gets bonus");
        Check(!window.TakeAmbush(false, true), "Second hit must not get bonus");
        window.RestoreAmbush();
        Check(window.TakeAmbush(false, true), "Rejected hit preserves bonus for next hit");
        window.EndErase(true); window.BeginErase();
        Check(!window.TakeAmbush(false, true), "Repeated erase cannot bank old bonuses");
        window.EndErase(false);
        Check(!window.TakeAmbush(false, true), "Death cannot grant bonus");
        window.BeginEpitaph(10f, 6f);
        Check(window.Dodging(10f, true) && window.Dodging(15.99f, true), "Dodge covers entire active duration");
        Check(!window.Dodging(16f, true), "Dodge ends at six seconds");
        Check(!window.Dodging(12f, false), "Dead body cannot dodge");
        window.BeginEpitaph(15f, 6f);
        Check(window.Dodging(20f, true) && !window.Dodging(21f, true), "Reactivation refreshes dodge duration");
        Console.WriteLine("PASS: " + checks + " combat rule checks");
    }
}
