using UnityEngine;

/// <summary>Coastal and inland-water movement for patrol, explorer, galley, and deep-sea units.</summary>
public static class NavalMovementRules
{
    public static bool IsNavalUnit(UnitType type) =>
        type is UnitType.CoastalPatrol or UnitType.CoastalExplorer or UnitType.CoastalGalley or UnitType.DeepSeaShip;

    public static bool IsHybridNaval(UnitType type) =>
        type is UnitType.CoastalPatrol or UnitType.CoastalExplorer;

    public static bool IsPureNaval(UnitType type) => type == UnitType.CoastalGalley;

    public static bool IsDeepSeaUnit(UnitType type) => type == UnitType.DeepSeaShip;

    public static bool CanEnterTile(UnitType unitType, HexTile tile)
    {
        if (tile == null)
            return false;

        if (!IsNavalUnit(unitType))
            return TerrainRules.IsPassable(tile.Terrain);

        if (IsDeepSeaUnit(unitType) && TerrainRules.IsWater(tile.Terrain))
            return true;

        if (TerrainRules.IsWater(tile.Terrain))
            return tile.IsNavigableWater;

        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;

        if (IsPureNaval(unitType) || IsDeepSeaUnit(unitType))
            return tile.Terrain == TerrainType.Shore || tile.IsNavalCoast;

        return true;
    }

    public static int StepCost(UnitType unitType, HexTile tile)
    {
        if (tile == null || !CanEnterTile(unitType, tile))
            return int.MaxValue / 4;

        if (TerrainRules.IsWater(tile.Terrain))
            return IsDeepSeaUnit(unitType) && !tile.IsNavigableWater ? 2 : 1;

        if (IsPureNaval(unitType) || IsDeepSeaUnit(unitType))
            return 1;

        return 1 + HexGridMap.TerrainMovePenalty(tile.Terrain);
    }

    public static bool GetsCoastalMoveBonus(UnitType unitType, HexTile tile) =>
        unitType == UnitType.CoastalPatrol &&
        tile != null &&
        (tile.IsNavalCoast ||
         tile.Terrain == TerrainType.Shore ||
         (TerrainRules.IsWater(tile.Terrain) && tile.IsNavigableWater));

    public static bool RequiresWharf(CityBuildId id) =>
        id is CityBuildId.TrainCoastalPatrol or CityBuildId.TrainCoastalExplorer
            or CityBuildId.BuildFishingPost or CityBuildId.BuildDock
            or CityBuildId.TrainCoastalGalley or CityBuildId.TrainDeepSeaShip;

    public static bool RequiresDock(CityBuildId id) =>
        id is CityBuildId.TrainCoastalGalley or CityBuildId.TrainDeepSeaShip;

    public static string FormatTileNavalHint(HexTile tile)
    {
        if (tile == null)
            return "";

        if (tile.IsNavigableWater)
            return "  |  <color=#88CCFF>Navigable water</color>";
        if (tile.Terrain == TerrainType.Ocean)
            return "  |  <color=#6688AA>Deep ocean (deep-sea ship only)</color>";
        if (tile.IsNavalCoast)
            return "  |  <color=#88CCFF>Naval coast</color>";
        return "";
    }

    public static string FormatUnitNavalHint(Unit unit)
    {
        if (unit == null || !IsNavalUnit(unit.Type))
            return "";

        if (IsDeepSeaUnit(unit.Type))
            return "  |  <color=#88CCFF>All ocean + shore + navigable water</color>";

        if (IsPureNaval(unit.Type))
            return "  |  <color=#88CCFF>Shore + navigable water only</color>";

        if (unit.Type == UnitType.CoastalExplorer)
            return "  |  <color=#88CCFF>Land + navigable water | wide sight</color>";

        return "  |  <color=#88CCFF>Land + navigable water</color>";
    }
}
