using UnityEngine;

public static class CombatSystem
{
    public struct CombatResult
    {
        public int DamageDealt;
        public int CounterDamage;
        public bool DefenderDestroyed;
        public bool AttackerDestroyed;
    }

    public static CombatResult Resolve(Unit attacker, Unit defender)
    {
        int defense = defender.Defense + GarrisonBonus.GetDefenseBonus(defender) + ChaplainSpecialty.GetDefenseBonus(defender);
        int raw = attacker.Attack + GarrisonBonus.GetAttackBonus(attacker) + ChaplainSpecialty.GetAttackBonus(attacker) - defense + Random.Range(-2, 3);
        int damage = Mathf.Max(1, raw);
        damage = ApplyFactionDamageMods(attacker, defender, damage);

        int healthBefore = defender.Health;
        defender.TakeDamage(damage);
        var result = new CombatResult
        {
            DamageDealt = damage,
            DefenderDestroyed = !defender.IsAlive
        };

        if (AreAdjacent(attacker.HexPosition, defender.HexPosition))
            result.CounterDamage = ApplyMeleeCounterDamage(attacker, defender, damage, healthBefore);

        if (result.CounterDamage > 0)
            result.AttackerDestroyed = !attacker.IsAlive;

        attacker.MarkAttacked();
        MatchHistory.Instance?.RegisterPlayerCombat(attacker, defender);
        string counterNote = result.CounterDamage > 0 ? $"; attacker -{result.CounterDamage}" : "";
        Debug.Log(
            $"Combat: {attacker.Faction} {attacker.Type} hit {defender.Faction} {defender.Type} for {damage} " +
            $"(garrison atk +{GarrisonBonus.GetAttackBonus(attacker)}, def +{GarrisonBonus.GetDefenseBonus(defender)}){counterNote}.");
        MatchController.Instance?.EvaluateConditions();
        return result;
    }

    /// <summary>Retaliation scales with defender strength and how contested the exchange was.</summary>
    static int ApplyMeleeCounterDamage(Unit attacker, Unit defender, int damageDealt, int defenderHealthBefore)
    {
        int counterBase = defender.Attack / 2 + GarrisonBonus.GetAttackBonus(defender)
            - attacker.Defense - GarrisonBonus.GetDefenseBonus(attacker) + Random.Range(-1, 2);

        if (counterBase <= 0)
            return 0;

        float fightWeight = defender.IsAlive
            ? defender.Health / (float)Mathf.Max(1, defender.MaxHealth)
            : RetaliationWeightForKill(defenderHealthBefore, defender.MaxHealth, damageDealt);

        int counter = Mathf.RoundToInt(counterBase * fightWeight);
        if (counter <= 0)
            return 0;

        counter = ApplyFactionDamageMods(defender, attacker, counter);
        attacker.TakeDamage(counter);
        return counter;
    }

    /// <summary>One-shot kills only retaliate in proportion to HP absorbed — not full overkill.</summary>
    public static float RetaliationWeightForKill(int healthBefore, int maxHealth, int damageDealt)
    {
        if (healthBefore <= 0 || damageDealt <= 0)
            return 0f;

        float hpShare = healthBefore / (float)Mathf.Max(1, maxHealth);
        float absorbedShare = Mathf.Min(healthBefore, damageDealt) / (float)damageDealt;
        return Mathf.Min(hpShare, absorbedShare);
    }

    static int ApplyFactionDamageMods(Unit attacker, Unit defender, int damage)
    {
        if (ConfessionResearchManager.Instance == null) return damage;
        if (defender.Faction != FactionId.LutheranSynod || attacker.Faction != FactionId.Schismatic)
            return damage;

        float mult = ConfessionResearchManager.Instance.GetEffectiveModifiers().SchismaticDamageTakenMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(damage * mult));
    }

    public static bool AreAdjacent(HexCoordinates a, HexCoordinates b) =>
        HexGridMap.Instance != null
            ? HexGridMap.Instance.AreWrappedAdjacent(a, b)
            : a.DistanceTo(b) == 1;

    public static bool AreInAttackRange(HexCoordinates from, HexCoordinates to, int range = 0)
    {
        if (HexGridMap.Instance == null) return from.DistanceTo(to) == 1;
        int dist = HexGridMap.Instance.WrappedDistance(from, to);
        if (range > 0) return dist >= 1 && dist <= range;
        return dist == 1;
    }

    public static bool AreInAttackRange(HexCoordinates from, HexCoordinates to, Unit attacker)
    {
        if (attacker == null) return AreInAttackRange(from, to, 1);
        int dist = HexGridMap.Instance != null
            ? HexGridMap.Instance.WrappedDistance(from, to)
            : from.DistanceTo(to);
        return dist >= 1 && dist <= attacker.AttackRange;
    }
}
