using UnityEngine;

/// <summary>Martial bonuses for units stationed on friendly city hexes with garrison buildings.</summary>
public static class GarrisonBonus
{
    public const int FortificationDefenseBonus = 2;
    public const int WatchtowerDefenseBonus = 2;
    public const int WatchtowerAttackBonus = 1;
    public const int WatchtowerCitySightBonus = 1;

    public static bool IsMartialUnit(Unit unit) =>
        unit != null &&
        unit.Type is UnitType.Soldier or UnitType.Defender or UnitType.Slinger or UnitType.Archer
            or UnitType.Horseman or UnitType.SiegeEngine or UnitType.CoastalGalley;

    public static bool TryGetFriendlyCityAt(Unit unit, out City city)
    {
        city = null;
        if (unit == null || HexGridMap.Instance == null)
            return false;

        if (!HexGridMap.Instance.TryGetTile(unit.HexPosition, out var tile) || tile.Settlement == null)
            return false;

        if (tile.Settlement.Faction != unit.Faction)
            return false;
        if (tile.Settlement.Faction == FactionId.LutheranSynod &&
            tile.Settlement.SynodPlayer != unit.SynodPlayer)
            return false;

        city = tile.Settlement;
        return true;
    }

    public static bool IsOnWatchtowerCityTile(Unit unit) =>
        TryGetFriendlyCityAt(unit, out var city) &&
        city.Production?.HasBuilding(CityBuildId.BuildWatchtower) == true;

    public static int GetDefenseBonus(Unit defender)
    {
        if (defender == null || CityManager.Instance == null)
            return 0;

        int bonus = 0;
        if (CityManager.Instance.IsOnFortifiedCityTile(defender))
            bonus += FortificationDefenseBonus;

        if (IsMartialUnit(defender) && IsOnWatchtowerCityTile(defender))
            bonus += WatchtowerDefenseBonus;

        return bonus;
    }

    public static int GetAttackBonus(Unit attacker)
    {
        if (!IsMartialUnit(attacker) || !IsOnWatchtowerCityTile(attacker))
            return 0;

        return WatchtowerAttackBonus;
    }

    public static int GetCitySightRange(City city)
    {
        const int baseRange = 2;
        if (city?.Production == null)
            return baseRange;

        return city.Production.HasBuilding(CityBuildId.BuildWatchtower)
            ? baseRange + WatchtowerCitySightBonus
            : baseRange;
    }

    public static string FormatRoleSuffix(Unit unit)
    {
        if (unit == null || !IsMartialUnit(unit))
            return "";

        if (CityManager.Instance?.IsOnFortifiedCityTile(unit) == true &&
            IsOnWatchtowerCityTile(unit))
            return " | fortified watchtower garrison";

        if (CityManager.Instance?.IsOnFortifiedCityTile(unit) == true)
            return " | fortified garrison";

        if (IsOnWatchtowerCityTile(unit))
            return " | watchtower garrison (+1 atk, +2 def)";

        return "";
    }

    public static string FormatCityGarrisonHint(City city)
    {
        if (city == null || city.Production == null)
            return "";

        bool fort = city.Production.HasBuilding(CityBuildId.BuildFortification);
        bool tower = city.Production.HasBuilding(CityBuildId.BuildWatchtower);
        if (!fort && !tower)
            return "";

        if (fort && tower)
            return "\n<size=11><color=#AABBCC>Martial garrison: +2 def (walls) + +1 atk/+2 def (tower)</color></size>";

        if (fort)
            return "\n<size=11><color=#AABBCC>Martial garrison: +2 def on city hex (walls)</color></size>";

        return "\n<size=11><color=#AABBCC>Martial garrison: +1 atk, +2 def on city hex (watchtower)</color></size>";
    }
}
