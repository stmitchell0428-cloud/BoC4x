using UnityEngine;

/// <summary>
/// Citizens take up arms: independent cities strike adjacent hostile units at end of turn
/// (militia sally when the parish is threatened).
/// </summary>
public static class CityMilitia
{
    public const int MinDamage = 2;
    public const int MaxDamage = 5;

    public static void ProcessEndTurn(FactionId faction, SchismaticBlocId blocFilter = SchismaticBlocId.None)
    {
        if (CityManager.Instance == null || HexGridMap.Instance == null || TurnManager.Instance == null)
            return;

        foreach (var city in CityManager.Instance.GetCitiesForFaction(faction))
        {
            if (city == null || city.IsHamlet)
                continue;
            if (blocFilter != SchismaticBlocId.None && city.SchismaticBloc != blocFilter)
                continue;

            StrikeAdjacentHostiles(city);
        }
    }

    public static void ProcessSynodPlayerEndTurn(SynodPlayerId playerId)
    {
        if (CityManager.Instance == null || HexGridMap.Instance == null || TurnManager.Instance == null)
            return;

        foreach (var city in CityManager.Instance.GetSynodPlayerCities(playerId))
        {
            if (city == null || city.IsHamlet)
                continue;

            StrikeAdjacentHostiles(city);
        }
    }

    static void StrikeAdjacentHostiles(City city)
    {
        int damage = DamageFor(city);
        if (damage <= 0)
            return;

        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(city.HexPosition))
        {
            if (!HexGridMap.Instance.TryGetTile(neighbor, out var tile))
                continue;

            var foe = tile.Occupant;
            if (foe == null || !foe.IsAlive || !FactionRelations.IsHostileToCity(foe, city))
                continue;

            foe.TakeDamage(damage);
            string foeLabel = $"{foe.FormatOwnerLabel()} {Unit.TypeDisplayName(foe.Type)}";
            if (!foe.IsAlive)
            {
                Debug.LogWarning(
                    $"{city.CityName} militia drove off {foeLabel} ({damage} damage) — citizens held the walls.");
                TurnPhaseBanner.Instance?.Refresh(
                    $"<color=#FFCC66><b>{city.CityName} militia</b></color> destroyed {foeLabel}!");
            }
            else
            {
                Debug.Log(
                    $"{city.CityName} militia struck {foeLabel} for {damage} HP " +
                    $"({foe.Health}/{foe.MaxHealth}) — citizens took up arms.");
                TurnPhaseBanner.Instance?.Refresh(
                    $"<color=#FFCC66><b>{city.CityName} militia</b></color> struck {foeLabel} for {damage} HP " +
                    $"({foe.Health}/{foe.MaxHealth})");
            }
        }
    }

    static int DamageFor(City city)
    {
        // Roughly 1 per 5 pop, clamped; capitals fight a bit harder.
        int damage = Mathf.Clamp(city.Population / 5, MinDamage, MaxDamage);
        if (city.IsCapital)
            damage = Mathf.Min(MaxDamage, damage + 1);
        return damage;
    }
}
