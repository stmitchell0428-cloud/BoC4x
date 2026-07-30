using UnityEngine;

/// <summary>
/// Walls / fortification rules: loyalty still captures, but fortified cities
/// block hostile entry onto the city hex until the parish yields.
/// </summary>
public static class CityDefenses
{
    public static bool HasWalls(City city) =>
        city != null &&
        !city.IsHamlet &&
        city.Production != null &&
        city.Production.HasBuilding(CityBuildId.BuildFortification);

    public static bool BlocksHostileEntry(City city, Unit unit) =>
        city != null &&
        unit != null &&
        HasWalls(city) &&
        FactionRelations.IsHostileToCity(unit, city) &&
        city.Loyalty > 0f;

    public static bool BlocksHostileEntryAt(HexCoordinates hex, Unit unit)
    {
        if (unit == null || HexGridMap.Instance == null)
            return false;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile) || tile.Settlement == null)
            return false;
        return BlocksHostileEntry(tile.Settlement, unit);
    }

    /// <summary>Siege/preach from the city hex, or from adjacent tiles when walls hold.</summary>
    public static bool CanPressCityFrom(Unit unit, City city)
    {
        if (unit == null || city == null || !FactionRelations.IsHostileToCity(unit, city))
            return false;

        if (unit.HexPosition == city.HexPosition)
            return !HasWalls(city) || city.Loyalty <= 0f;

        if (!HasWalls(city))
            return false;

        return HexGridMap.Instance != null &&
               HexGridMap.Instance.AreWrappedAdjacent(unit.HexPosition, city.HexPosition);
    }
}
