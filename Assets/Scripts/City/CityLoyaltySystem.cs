using System.Text;
using UnityEngine;

/// <summary>Siege and loyalty  -  cities flip when loyalty reaches 0, not on first contact.</summary>
public static class CityLoyaltySystem
{
    public const float CapitalLoyalty = 100f;
    public const float CityLoyalty = 85f;
    public const float HamletLoyalty = 55f;
    public const float SchismaticCapitalLoyalty = 80f;

    /// <summary>Per-turn restore when the city is not under enemy siege/occupation.</summary>
    public const float BaseLoyaltyRecovery = 5f;

    public static float GetStartingLoyalty(City city)
    {
        if (city == null) return CityLoyalty;
        if (city.IsHamlet) return HamletLoyalty;
        if (city.IsCapital) return city.Faction == FactionId.Schismatic ? SchismaticCapitalLoyalty : CapitalLoyalty;
        return city.Faction == FactionId.Schismatic ? 70f : CityLoyalty;
    }

    public static float GetFortificationModifier(City city)
    {
        if (city?.Production == null) return 1f;
        if (city.Production.HasBuilding(CityBuildId.BuildFortification)) return 0.5f;
        if (city.Production.HasBuilding(CityBuildId.BuildWatchtower)) return 0.75f;
        return 1f;
    }

    public static int GetSiegePressure(Unit unit)
    {
        if (unit == null) return 0;

        int basePressure = unit.Type switch
        {
            UnitType.SiegeEngine => 18,
            UnitType.Horseman => 12,
            UnitType.Soldier => 10,
            UnitType.Defender => 8,
            UnitType.Slinger => 7,
            UnitType.Archer => 7,
            _ => 0
        };
        if (basePressure <= 0)
            return 0;

        if (unit.Faction == FactionId.LutheranSynod && ConfessionResearchManager.Instance != null)
            basePressure += ConfessionResearchManager.Instance.GetEffectiveModifiers().SiegePressureBonus;

        if (unit.Type == UnitType.SiegeEngine &&
            unit.Faction == FactionId.LutheranSynod &&
            CityManager.Instance?.HasAnyPlayerBuilding(CityBuildId.BuildArmory) == true)
            basePressure += 2;

        return basePressure;
    }

    public static bool IsCityUnderEnemyOccupation(City city)
    {
        if (city == null || HexGridMap.Instance == null)
            return false;

        if (!HexGridMap.Instance.TryGetTile(city.HexPosition, out var tile))
            return false;

        var occupier = tile.Occupant;
        if (occupier != null && occupier.IsAlive && FactionRelations.IsHostileToCity(occupier, city))
            return true;

        // Walled cities are besieged from adjacent hexes.
        if (CityDefenses.HasWalls(city))
        {
            foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(city.HexPosition))
            {
                if (!HexGridMap.Instance.TryGetTile(neighbor, out var nTile))
                    continue;
                var foe = nTile.Occupant;
                if (foe != null && foe.IsAlive && FactionRelations.IsHostileToCity(foe, city) &&
                    IsMartialOccupier(foe))
                    return true;
            }
        }

        return false;
    }

    public static void ProcessEndTurnOccupation(FactionId occupierFaction)
    {
        if (HexGridMap.Instance == null || CityManager.Instance == null || TurnManager.Instance == null)
            return;

        foreach (var unit in TurnManager.Instance.GetUnits(occupierFaction))
        {
            if (!unit.IsAlive || !IsMartialOccupier(unit))
                continue;

            if (!HexGridMap.Instance.TryGetTile(unit.HexPosition, out var tile))
                continue;

            if (tile.Settlement != null &&
                tile.Settlement.Faction != occupierFaction &&
                tile.Occupant == unit &&
                CityDefenses.CanPressCityFrom(unit, tile.Settlement))
            {
                TryApplyPressure(unit, tile.Settlement, isPreach: false);
                continue;
            }

            // Adjacent pressure vs walled cities.
            foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(unit.HexPosition))
            {
                if (!HexGridMap.Instance.TryGetTile(neighbor, out var nTile) || nTile.Settlement == null)
                    continue;
                var city = nTile.Settlement;
                if (city.Faction == occupierFaction)
                    continue;
                if (!CityDefenses.CanPressCityFrom(unit, city))
                    continue;

                TryApplyPressure(unit, city, isPreach: false);
                break;
            }
        }
    }

    /// <summary>
    /// After siege ends, loyalty climbs back toward starting hold.
    /// Friendly clergy on the city hex restore faster.
    /// </summary>
    public static void ProcessEndTurnRecovery(
        FactionId ownerFaction,
        SynodPlayerId synodFilter = SynodPlayerId.None,
        SchismaticBlocId blocFilter = SchismaticBlocId.None)
    {
        if (CityManager.Instance == null)
            return;

        foreach (var city in CityManager.Instance.AllCities)
        {
            if (city == null || city.Faction != ownerFaction)
                continue;
            if (ownerFaction == FactionId.LutheranSynod &&
                synodFilter != SynodPlayerId.None &&
                city.SynodPlayer != synodFilter)
                continue;
            if (ownerFaction == FactionId.Schismatic &&
                blocFilter != SchismaticBlocId.None &&
                city.SchismaticBloc != blocFilter)
                continue;

            TryRecoverLoyalty(city);
        }
    }

    public static bool TryRecoverLoyalty(City city)
    {
        if (city == null || IsCityUnderEnemyOccupation(city))
            return false;

        float ceiling = GetStartingLoyalty(city);
        if (city.Loyalty >= ceiling)
            return false;

        float amount = GetLoyaltyRecoveryAmount(city);
        if (amount <= 0f)
            return false;

        float before = city.Loyalty;
        float room = ceiling - before;
        city.AdjustLoyalty(Mathf.Min(amount, room));
        if (city.Loyalty > before)
        {
            Debug.Log(
                $"{city.CityName} loyalty recovered +{city.Loyalty - before:F0} -> {city.Loyalty:F0}% " +
                $"(ceiling {ceiling:F0}%)");
            return true;
        }

        return false;
    }

    public static float GetLoyaltyRecoveryAmount(City city)
    {
        if (city == null)
            return 0f;

        float amount = BaseLoyaltyRecovery;
        var clergy = GetFriendlyClergyOnCity(city);
        if (clergy != null)
            amount = Mathf.Max(amount, GetClergyLoyaltyRecovery(clergy));
        return amount;
    }

    public static float GetClergyLoyaltyRecovery(Unit unit)
    {
        if (unit == null || !unit.IsAlive || !unit.CanPreachOrHymn)
            return 0f;
        return GetClergyLoyaltyRecovery(unit.Type);
    }

    public static float GetClergyLoyaltyRecovery(UnitType type) => type switch
    {
        UnitType.Archbishop => 16f,
        UnitType.Bishop => 14f,
        UnitType.Pastor => 12f,
        UnitType.Chaplain => 10f,
        UnitType.Deaconess => 8f,
        UnitType.Missionary => 7f,
        UnitType.Cantor => 6f,
        _ => 0f
    };

    static Unit GetFriendlyClergyOnCity(City city)
    {
        if (city == null || HexGridMap.Instance == null)
            return null;
        if (!HexGridMap.Instance.TryGetTile(city.HexPosition, out var tile))
            return null;

        var unit = tile.Occupant;
        if (unit == null || !unit.IsAlive || !unit.CanPreachOrHymn)
            return null;
        if (unit.Faction != city.Faction)
            return null;
        if (city.Faction == FactionId.LutheranSynod && unit.SynodPlayer != city.SynodPlayer)
            return null;
        if (city.Faction == FactionId.Schismatic &&
            unit.SchismaticBloc != SchismaticBlocId.None &&
            city.SchismaticBloc != SchismaticBlocId.None &&
            unit.SchismaticBloc != city.SchismaticBloc)
            return null;

        return GetClergyLoyaltyRecovery(unit) > 0f ? unit : null;
    }

    public static float DisplayLoyalty(City city)
    {
        if (city == null) return 0f;
        if (city.Faction == FactionId.LutheranSynod && !IsCityUnderEnemyOccupation(city))
            return city.Loyalty;
        return city.Loyalty;
    }

    public static Color LoyaltyBarColor(float loyalty)
    {
        if (loyalty > 60f) return new Color(0.35f, 0.72f, 0.42f);
        if (loyalty > 30f) return new Color(0.85f, 0.65f, 0.25f);
        return new Color(0.82f, 0.35f, 0.28f);
    }

    public static string CityScreenLoyaltyLabel(City city)
    {
        if (city == null) return "Loyalty";

        if (city.Faction == FactionId.LutheranSynod)
        {
            if (IsCityUnderEnemyOccupation(city))
                return $"Under siege  -  {city.Loyalty:F0}% synod hold";
            if (city.Loyalty < GetStartingLoyalty(city))
            {
                float recover = GetLoyaltyRecoveryAmount(city);
                return $"Synod loyalty  -  {city.Loyalty:F0}% (+{recover:F0}/turn recovering)";
            }
            return $"Synod loyalty  -  {city.Loyalty:F0}%";
        }

        return $"Occupation target  -  {city.Loyalty:F0}%";
    }

    public static int GetPreachPressure(Unit unit)
    {
        if (unit == null || !unit.CanPreachOrHymn) return 0;
        if (unit.Type == UnitType.Chaplain)
            return ChaplainSpecialty.GetPreachPressure(unit) + EpiscopalOversight.GetPassivePreachPressureBonus(unit);

        int pressure = unit.Type switch
        {
            UnitType.Pastor => 14,
            UnitType.Bishop => 17,
            UnitType.Archbishop => 22,
            UnitType.Deaconess => 8,
            UnitType.Missionary => 6,
            UnitType.Cantor => 5,
            _ => 4
        };
        return pressure + EpiscopalOversight.GetPassivePreachPressureBonus(unit);
    }

    public static bool IsMartialOccupier(Unit unit) => GetSiegePressure(unit) > 0;

    /// <summary>Apply siege or preach pressure; returns true if city captured.</summary>
    public static bool TryApplyPressure(Unit unit, City city, bool isPreach)
    {
        if (unit == null || city == null || !FactionRelations.IsHostileToCity(unit, city))
            return false;

        int basePressure = isPreach ? GetPreachPressure(unit) : GetSiegePressure(unit);
        if (basePressure <= 0)
            return false;

        float mod = GetFortificationModifier(city);
        if (!isPreach && unit.Type == UnitType.SiegeEngine)
            mod = Mathf.Min(1f, mod + 0.25f);
        if (city.IsCapital && !isPreach)
            mod *= 0.85f;

        int pressure = Mathf.Max(1, Mathf.RoundToInt(basePressure * mod));
        city.AdjustLoyalty(-pressure);

        string action = isPreach ? "Preaching eroded" : "Siege reduced";
        Debug.Log($"{action} {city.CityName} loyalty by {pressure} -> {city.Loyalty:F0}%");

        if (city.Loyalty <= 0f)
        {
            city.Capture(unit.Faction, unit.SynodPlayer);
            return true;
        }

        return false;
    }

    public static string FormatLoyaltyBar(float loyalty, int width = 10)
    {
        loyalty = Mathf.Clamp(loyalty, 0f, 100f);
        int filled = Mathf.RoundToInt(loyalty / 100f * width);
        filled = Mathf.Clamp(filled, 0, width);
        string bar = new string('#', filled) + new string('-', width - filled);
        string color = loyalty > 60f ? "#88CC88" : loyalty > 30f ? "#FFCC66" : "#FF8866";
        return $"<color={color}>[{bar}] {loyalty:F0}%</color>";
    }

    public static string FormatHoverLoyaltyBlock(City city, Unit selectedUnit = null)
    {
        if (city == null)
            return "";

        var sb = new StringBuilder();
        sb.Append(FormatLoyaltyBar(city.Loyalty));

        if (city.Faction != FactionId.LutheranSynod)
        {
            sb.Append("  -  siege or preach to capture");
            if (CityDefenses.HasWalls(city))
                sb.Append("\n<size=11><color=#AABBCC>Walls hold — besiege from adjacent hexes until loyalty falls.</color></size>");
            float fortMod = GetFortificationModifier(city);
            if (fortMod < 1f)
            {
                int reduction = Mathf.RoundToInt((1f - fortMod) * 100f);
                sb.Append($"\n<size=11><color=#AABBCC>Fortified (-{reduction}% siege pressure)</color></size>");
            }
        }
        else if (IsCityUnderEnemyOccupation(city))
            sb.Append("  -  <color=#FFCC66>under siege</color>");
        else
        {
            if (city.Loyalty < GetStartingLoyalty(city))
            {
                float recover = GetLoyaltyRecoveryAmount(city);
                var clergy = GetFriendlyClergyOnCity(city);
                string who = clergy != null ? $" with {Unit.TypeDisplayName(clergy.Type)}" : "";
                sb.Append($"\n<size=11><color=#88CC88>Recovering +{recover:F0}/turn{who} (siege clear)</color></size>");
            }
            if (CityDefenses.HasWalls(city))
                sb.Append("\n<size=11><color=#AABBCC>Walls hold — hostiles cannot enter until loyalty falls.</color></size>");
        }

        if (selectedUnit != null && selectedUnit.Faction == FactionId.LutheranSynod && city.Faction != selectedUnit.Faction)
        {
            int siege = GetSiegePressure(selectedUnit);
            int preach = GetPreachPressure(selectedUnit);
            if (siege > 0)
                sb.Append($"\n<size=11>Selected unit siege: {siege}/turn on occupation</size>");
            if (preach > 0)
                sb.Append($"\n<size=11>Selected unit preach: {preach}/turn on occupation</size>");
        }

        return sb.ToString();
    }

    public static string FormatLoyaltyLine(City city)
    {
        if (city == null || city.Faction == FactionId.LutheranSynod)
            return city != null ? FormatLoyaltyBar(city.Loyalty) : "";

        return $"{FormatLoyaltyBar(city.Loyalty)}  -  siege or preach to capture";
    }
}
