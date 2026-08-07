using UnityEngine;

/// <summary>Coastal and inland-water movement for explorer, galley, and deep-sea units.</summary>
public static class NavalMovementRules
{
    public static bool IsNavalUnit(UnitType type) =>
        type is UnitType.CoastalExplorer or UnitType.CoastalGalley or UnitType.DeepSeaShip;

    public static bool IsCoastalNaval(UnitType type) =>
        type is UnitType.CoastalExplorer or UnitType.CoastalGalley;

    public static bool IsDeepSeaUnit(UnitType type) => type == UnitType.DeepSeaShip;

    public static bool IsGalleyUnit(UnitType type) => type == UnitType.CoastalGalley;

    public static bool CanEnterTile(UnitType unitType, HexTile tile)
    {
        if (tile == null)
            return false;

        if (!IsNavalUnit(unitType))
            return TerrainRules.IsPassable(tile.Terrain);

        bool isWater = TerrainRules.IsWater(tile.Terrain);

        if (unitType == UnitType.CoastalExplorer)
        {
            // Rivers/lakes always; coastal sea (navigable ocean band) allowed; deep ocean blocked.
            if (isWater)
                return tile.IsNavigableWater;
            if (!TerrainRules.IsPassable(tile.Terrain))
                return false;
            return tile.Terrain == TerrainType.Shore || tile.IsNavalCoast;
        }

        if (unitType == UnitType.CoastalGalley)
            return isWater && tile.IsNavigableWater;

        if (unitType == UnitType.DeepSeaShip)
            return isWater;

        return false;
    }

    /// <summary>Explorer land-to-land steps must hug the same navigable water — blocks peninsula portage.</summary>
    public static bool CanTraverse(HexTile from, HexTile to, UnitType unitType)
    {
        if (from == null || to == null || !IsNavalUnit(unitType))
            return true;

        if (unitType is UnitType.CoastalGalley or UnitType.DeepSeaShip)
            return true;

        bool fromLand = !TerrainRules.IsWater(from.Terrain);
        bool toLand = !TerrainRules.IsWater(to.Terrain);
        if (!fromLand || !toLand)
            return true;

        return SharesNavigableWaterNeighbor(from.Coordinates, to.Coordinates);
    }

    static bool SharesNavigableWaterNeighbor(HexCoordinates a, HexCoordinates b)
    {
        var map = HexGridMap.Instance;
        if (map == null)
            return false;

        foreach (var waterCoords in map.GetWrappedNeighbors(a))
        {
            if (!map.TryGetTile(waterCoords, out var waterTile))
                continue;
            if (!TerrainRules.IsWater(waterTile.Terrain) || !waterTile.IsNavigableWater)
                continue;

            foreach (var touch in map.GetWrappedNeighbors(b))
            {
                if (touch == waterCoords)
                    return true;
            }
        }

        return false;
    }

    public static int StepCost(UnitType unitType, HexTile tile)
    {
        if (tile == null || !CanEnterTile(unitType, tile))
            return int.MaxValue / 4;

        if (TerrainRules.IsWater(tile.Terrain))
            return IsDeepSeaUnit(unitType) && !tile.IsNavigableWater ? 2 : 1;

        return 1 + HexGridMap.TerrainMovePenalty(tile.Terrain);
    }

    public static bool RequiresWharf(CityBuildId id) =>
        id is CityBuildId.TrainCoastalExplorer
            or CityBuildId.BuildFishingPost or CityBuildId.BuildDock
            or CityBuildId.TrainCoastalGalley or CityBuildId.TrainDeepSeaShip;

    public static bool RequiresDock(CityBuildId id) =>
        id is CityBuildId.TrainCoastalGalley or CityBuildId.TrainDeepSeaShip;

    public static string FormatTileNavalHint(HexTile tile)
    {
        if (tile == null)
            return "";
        return FormatTerrainNavalHint(tile.Terrain, tile.IsNavalCoast, tile.IsNavigableWater);
    }

    public static string FormatTerrainNavalHint(TerrainType terrain, bool isNavalCoast, bool isNavigableWater)
    {
        if (terrain == TerrainType.River || terrain == TerrainType.Lake)
        {
            return isNavigableWater
                ? "  |  <color=#66DDCC>River/lake — explorer+</color>"
                : "  |  <color=#6688AA>Water (impassable)</color>";
        }

        if (terrain == TerrainType.Ocean)
        {
            if (isNavigableWater)
                return "  |  <color=#88AAFF>Coastal sea — explorer + galley</color>";
            return "  |  <color=#6688AA>Open ocean — deep-sea ship only</color>";
        }

        if (isNavalCoast)
            return "  |  <color=#88CCFF>Naval coast (explorer shore)</color>";
        return "";
    }

    public static string FormatUnitNavalHint(Unit unit)
    {
        if (unit == null || !IsNavalUnit(unit.Type))
            return "";

        return unit.Type switch
        {
            UnitType.CoastalExplorer =>
                "  |  <color=#88CCFF>Rivers, lakes, coastal sea — not deep ocean</color>",
            UnitType.CoastalGalley =>
                "  |  <color=#88CCFF>Water only — land troops via cargo disembark</color>",
            UnitType.DeepSeaShip =>
                "  |  <color=#88CCFF>All ocean — water only; cargo lands on shore</color>",
            _ => ""
        };
    }
}
