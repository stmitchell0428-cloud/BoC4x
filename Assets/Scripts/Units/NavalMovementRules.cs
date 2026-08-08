using System.Collections.Generic;
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

    public static bool CanEnterTile(UnitType unitType, HexTile tile) =>
        CanEnterTile(unitType, tile, movingFaction: null, movingSynod: SynodPlayerId.None);

    public static bool CanEnterTile(
        UnitType unitType,
        HexTile tile,
        FactionId? movingFaction,
        SynodPlayerId movingSynod)
    {
        if (tile == null)
            return false;

        if (!IsNavalUnit(unitType))
            return TerrainRules.IsPassable(tile.Terrain);

        bool isWater = TerrainRules.IsWater(tile.Terrain);

        if (unitType == UnitType.CoastalExplorer)
        {
            if (isWater)
                return tile.IsNavigableWater;
            if (!TerrainRules.IsPassable(tile.Terrain))
                return false;
            return tile.Terrain == TerrainType.Shore || tile.IsNavalCoast;
        }

        if (unitType == UnitType.CoastalGalley)
        {
            if (isWater)
                return tile.IsNavigableWater;
            // Friendly city hex: portage between waters that both touch the city.
            return IsFriendlyPortHex(tile, movingFaction, movingSynod);
        }

        if (unitType == UnitType.DeepSeaShip)
        {
            if (isWater)
                return true;
            return IsFriendlyPortHex(tile, movingFaction, movingSynod);
        }

        return false;
    }

    static bool IsFriendlyPortHex(HexTile tile, FactionId? movingFaction, SynodPlayerId movingSynod)
    {
        if (tile?.Settlement == null || !movingFaction.HasValue)
            return false;
        var city = tile.Settlement;
        if (city.Faction != movingFaction.Value)
            return false;
        return true;
    }

    /// <summary>
    /// Explorer: land↔land must share navigable water; land→water blocked if the land
    /// also touches a disconnected water body (no free ocean↔lake portage).
    /// </summary>
    public static bool CanTraverse(HexTile from, HexTile to, UnitType unitType)
    {
        if (from == null || to == null || !IsNavalUnit(unitType))
            return true;

        if (unitType is UnitType.CoastalGalley or UnitType.DeepSeaShip)
            return true;

        bool fromLand = !TerrainRules.IsWater(from.Terrain);
        bool toLand = !TerrainRules.IsWater(to.Terrain);

        if (!fromLand && !toLand)
            return true;

        if (fromLand && toLand)
            return SharesNavigableWaterNeighbor(from.Coordinates, to.Coordinates);

        // Water → land: always OK if CanEnter allows the land.
        if (!fromLand && toLand)
            return true;

        // Land → water: land's navigable water neighbors must be one connected body with `to`.
        return LandConnectsOnlyToWaterBody(from.Coordinates, to.Coordinates);
    }

    static bool LandConnectsOnlyToWaterBody(HexCoordinates land, HexCoordinates waterDest)
    {
        var map = HexGridMap.Instance;
        if (map == null)
            return false;

        var waterNeighbors = new List<HexCoordinates>();
        foreach (var n in map.GetWrappedNeighbors(land))
        {
            if (!map.TryGetTile(n, out var tile))
                continue;
            if (!TerrainRules.IsWater(tile.Terrain) || !tile.IsNavigableWater)
                continue;
            waterNeighbors.Add(n);
        }

        if (waterNeighbors.Count == 0)
            return false;

        foreach (var wn in waterNeighbors)
        {
            if (!SameNavigableWaterComponent(wn, waterDest))
                return false;
        }

        return true;
    }

    static bool SameNavigableWaterComponent(HexCoordinates a, HexCoordinates b)
    {
        var map = HexGridMap.Instance;
        if (map == null)
            return false;
        a = map.Wrap(a);
        b = map.Wrap(b);
        if (a == b)
            return true;

        var visited = new HashSet<HexCoordinates>();
        var queue = new Queue<HexCoordinates>();
        queue.Enqueue(a);
        visited.Add(a);
        int guard = 0;
        while (queue.Count > 0 && guard++ < 400)
        {
            var cur = queue.Dequeue();
            if (cur == b)
                return true;
            foreach (var n in map.GetWrappedNeighbors(cur))
            {
                if (!visited.Add(n))
                    continue;
                if (!map.TryGetTile(n, out var tile))
                    continue;
                if (!TerrainRules.IsWater(tile.Terrain) || !tile.IsNavigableWater)
                    continue;
                queue.Enqueue(n);
            }
        }

        return false;
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
        if (tile == null)
            return int.MaxValue / 4;

        if (TerrainRules.IsWater(tile.Terrain))
            return IsDeepSeaUnit(unitType) && !tile.IsNavigableWater ? 2 : 1;

        if (IsNavalUnit(unitType) && tile.Settlement != null)
            return 1;

        if (!IsNavalUnit(unitType) && !TerrainRules.IsPassable(tile.Terrain))
            return int.MaxValue / 4;

        return 1 + HexGridMap.TerrainMovePenalty(tile.Terrain);
    }

    public static bool RequiresWharf(CityBuildId id) =>
        id is CityBuildId.TrainCoastalExplorer
            or CityBuildId.BuildFishingPost or CityBuildId.BuildDock
            or CityBuildId.TrainCoastalGalley or CityBuildId.TrainDeepSeaShip;

    public static bool RequiresDock(CityBuildId id) =>
        id is CityBuildId.TrainCoastalGalley or CityBuildId.TrainDeepSeaShip;

    public static bool RequiresOceanAccess(CityBuildId id) =>
        id is CityBuildId.BuildWharf or CityBuildId.BuildDock
            or CityBuildId.TrainCoastalGalley or CityBuildId.TrainDeepSeaShip;

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
                "  |  <color=#88CCFF>Rivers, lakes, coastal sea — no ocean↔lake portage</color>",
            UnitType.CoastalGalley =>
                "  |  <color=#88CCFF>Water only — may pass friendly city hex; cargo disembark</color>",
            UnitType.DeepSeaShip =>
                "  |  <color=#88CCFF>All ocean — may pass friendly city; cargo lands on shore</color>",
            _ => ""
        };
    }
}
