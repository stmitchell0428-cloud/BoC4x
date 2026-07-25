using UnityEngine;

/// <summary>Coastal and inland-water movement for patrol and galley units.</summary>
public static class NavalMovementRules
{
    public static bool IsNavalUnit(UnitType type) =>
        type is UnitType.CoastalPatrol or UnitType.CoastalGalley;

    public static bool IsHybridNaval(UnitType type) => type == UnitType.CoastalPatrol;

    public static bool IsPureNaval(UnitType type) => type == UnitType.CoastalGalley;

    public static bool CanEnterTile(UnitType unitType, HexTile tile)
    {
        if (tile == null)
            return false;

        if (!IsNavalUnit(unitType))
            return TerrainRules.IsPassable(tile.Terrain);

        if (TerrainRules.IsWater(tile.Terrain))
            return tile.IsNavigableWater;

        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;

        if (IsPureNaval(unitType))
            return tile.Terrain == TerrainType.Shore || tile.IsNavalCoast;

        return true;
    }

    public static int StepCost(UnitType unitType, HexTile tile)
    {
        if (tile == null || !CanEnterTile(unitType, tile))
            return int.MaxValue / 4;

        if (TerrainRules.IsWater(tile.Terrain))
            return 1;

        if (IsPureNaval(unitType))
            return 1;

        return 1 + HexGridMap.TerrainMovePenalty(tile.Terrain);
    }

    public static bool GetsCoastalMoveBonus(UnitType unitType, HexTile tile) =>
        unitType == UnitType.CoastalPatrol &&
        tile != null &&
        (tile.IsNavalCoast ||
         tile.Terrain == TerrainType.Shore ||
         (TerrainRules.IsWater(tile.Terrain) && tile.IsNavigableWater));

    public static string FormatTileNavalHint(HexTile tile)
    {
        if (tile == null)
            return "";

        if (tile.IsNavigableWater)
            return "  |  <color=#88CCFF>Navigable water</color>";
        if (tile.IsNavalCoast)
            return "  |  <color=#88CCFF>Naval coast</color>";
        return "";
    }

    public static string FormatUnitNavalHint(Unit unit)
    {
        if (unit == null || !IsNavalUnit(unit.Type))
            return "";

        if (IsPureNaval(unit.Type))
            return "  |  <color=#88CCFF>Shore + navigable water only</color>";

        return "  |  <color=#88CCFF>Land + navigable water</color>";
    }
}
