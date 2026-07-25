using UnityEngine;

public static class CombatSystem
{
    public struct CombatResult
    {
        public int DamageDealt;
        public bool DefenderDestroyed;
        public bool AttackerDestroyed;
    }

    public static CombatResult Resolve(Unit attacker, Unit defender)
    {
        int defense = defender.Defense + GarrisonBonus.GetDefenseBonus(defender) + ChaplainSpecialty.GetDefenseBonus(defender);
        int raw = attacker.Attack + GarrisonBonus.GetAttackBonus(attacker) + ChaplainSpecialty.GetAttackBonus(attacker) - defense + Random.Range(-2, 3);
        int damage = Mathf.Max(1, raw);
        damage = ApplyFactionDamageMods(attacker, defender, damage);

        defender.TakeDamage(damage);
        var result = new CombatResult
        {
            DamageDealt = damage,
            DefenderDestroyed = !defender.IsAlive
        };

        if (defender.IsAlive && CombatSystem.AreAdjacent(attacker.HexPosition, defender.HexPosition))
        {
            int counter = defender.Attack / 2 + GarrisonBonus.GetAttackBonus(defender)
                - attacker.Defense - GarrisonBonus.GetDefenseBonus(attacker) + Random.Range(-1, 2);
            if (counter > 0)
            {
                counter = ApplyFactionDamageMods(defender, attacker, counter);
                attacker.TakeDamage(counter);
                result.AttackerDestroyed = !attacker.IsAlive;
            }
        }

        attacker.MarkAttacked();
        Debug.Log($"Combat: {attacker.Faction} {attacker.Type} hit {defender.Faction} {defender.Type} for {damage} (garrison atk +{GarrisonBonus.GetAttackBonus(attacker)}, def +{GarrisonBonus.GetDefenseBonus(defender)}).");
        MatchController.Instance?.EvaluateConditions();
        return result;
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
