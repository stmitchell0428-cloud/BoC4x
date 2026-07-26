using System.Collections.Generic;
using UnityEngine;

/// <summary>Martial units embark on galleys and land on shore hexes adjacent to the ship.</summary>
public static class AmphibiousTransport
{
    public const int GalleyPassengerCapacity = 2;

    public static bool IsAmphibiousCargo(Unit unit) =>
        unit != null &&
        unit.IsAlive &&
        !unit.IsEmbarked &&
        unit.Type is UnitType.Soldier or UnitType.Defender or UnitType.Slinger or UnitType.Archer
            or UnitType.Horseman;

    public static bool IsGalleyTransporter(Unit unit) =>
        unit != null && unit.IsAlive && unit.Type == UnitType.CoastalGalley;

    public static bool CanEmbark(Unit passenger, Unit galley)
    {
        if (!IsAmphibiousCargo(passenger) || !IsGalleyTransporter(galley))
            return false;
        if (passenger.Faction != galley.Faction)
            return false;
        if (!galley.CanEmbarkMore)
            return false;
        if (passenger.MovementRemaining <= 0)
            return false;
        if (!HexGridMap.Instance.TryGetTile(passenger.HexPosition, out var passengerTile))
            return false;
        if (!IsBoardingShoreTile(passengerTile))
            return false;
        if (!HexGridMap.Instance.TryGetTile(galley.HexPosition, out var galleyTile))
            return false;
        if (!IsGalleyWaterTile(galleyTile))
            return false;
        if (HexGridMap.Instance.WrappedDistance(passenger.HexPosition, galley.HexPosition) != 1)
            return false;

        return true;
    }

    public static bool TryEmbark(Unit passenger, Unit galley)
    {
        if (!CanEmbark(passenger, galley))
            return false;

        passenger.SetEmbarkedOn(galley);
        galley.AddPassenger(passenger);
        Debug.Log($"{Unit.TypeDisplayName(passenger.Type)} boarded {Unit.TypeDisplayName(galley.Type)} ({galley.EmbarkedCount}/{GalleyPassengerCapacity}).");
        return true;
    }

    public static bool CanDisembark(Unit galley, HexCoordinates landHex, out Unit passenger)
    {
        passenger = null;
        if (!IsGalleyTransporter(galley) || galley.EmbarkedCount == 0)
            return false;
        if (galley.MovementRemaining <= 0)
            return false;
        if (!IsValidLandingHex(galley.HexPosition, landHex))
            return false;

        passenger = galley.GetFirstPassenger();
        return passenger != null;
    }

    public static bool TryDisembark(Unit galley, HexCoordinates landHex, Unit passenger = null)
    {
        if (passenger == null)
            passenger = galley.GetFirstPassenger();

        if (passenger == null || !ContainsPassenger(galley, passenger))
            return false;

        if (!CanDisembark(galley, landHex, out _))
            return false;

        galley.RemovePassenger(passenger);
        passenger.ClearEmbarkedState(landHex);
        galley.SpendMovement(galley.MovementRemaining);
        galley.ClearMoveOrder();

        CityManager.Instance?.TryCaptureCityAt(passenger, landHex);
        HexGridMap.Instance?.InvalidateMovementCostCache();
        FogOfWarManager.Instance?.Refresh();

        Debug.Log($"{Unit.TypeDisplayName(passenger.Type)} landed from galley at {landHex}.");
        return true;
    }

    public static bool TryDisembark(Unit galley, HexCoordinates landHex)
        => TryDisembark(galley, landHex, null);

    public static List<HexCoordinates> GetDisembarkHexes(Unit galley)
    {
        var list = new List<HexCoordinates>();
        if (!IsGalleyTransporter(galley) || galley.EmbarkedCount == 0 || galley.MovementRemaining <= 0)
            return list;
        if (HexGridMap.Instance == null)
            return list;

        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(galley.HexPosition))
        {
            if (IsValidLandingHex(galley.HexPosition, neighbor))
                list.Add(neighbor);
        }

        return list;
    }

    public static bool IsValidLandingHex(HexCoordinates fromWater, HexCoordinates landHex)
    {
        if (HexGridMap.Instance == null ||
            !HexGridMap.Instance.TryGetTile(landHex, out var landTile))
            return false;
        if (!TerrainRules.IsPassable(landTile.Terrain))
            return false;
        if (landTile.Occupant != null)
            return false;
        if (HexGridMap.Instance.WrappedDistance(fromWater, landHex) != 1)
            return false;
        if (!HexGridMap.Instance.TryGetTile(fromWater, out var waterTile))
            return false;
        if (!IsGalleyWaterTile(waterTile))
            return false;

        return IsLandingTile(landTile);
    }

    static bool IsGalleyWaterTile(HexTile tile) =>
        tile != null &&
        TerrainRules.IsWater(tile.Terrain) &&
        tile.IsNavigableWater;

    static bool IsBoardingShoreTile(HexTile tile) =>
        tile != null &&
        TerrainRules.IsPassable(tile.Terrain) &&
        (tile.Terrain == TerrainType.Shore || tile.IsNavalCoast);

    static bool IsLandingTile(HexTile tile) =>
        tile != null && TerrainRules.IsPassable(tile.Terrain);

    public static string FormatGalleyCargoHint(Unit galley)
    {
        if (!IsGalleyTransporter(galley))
            return "";

        if (galley.EmbarkedCount == 0)
            return "  |  <color=#88CCFF>O = board adjacent soldier</color>";

        int landings = GetDisembarkHexes(galley).Count;
        if (landings > 0 && galley.MovementRemaining > 0)
            return $"  |  <color=#FFDD66>L or click shore = land ({galley.EmbarkedCount} aboard)</color>";

        return $"  |  <color=#AABBCC>{galley.EmbarkedCount} aboard — sail adjacent to shore to land</color>";
    }

    public static string FormatEmbarkHint(Unit passenger)
    {
        if (!IsAmphibiousCargo(passenger) || passenger.MovementRemaining <= 0)
            return "";

        if (FindAdjacentGalley(passenger) != null)
            return "  |  <color=#88CCFF>O = board adjacent galley</color>";

        return "";
    }

    public static Unit FindAdjacentGalley(Unit passenger)
    {
        if (HexGridMap.Instance == null || passenger == null)
            return null;

        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(passenger.HexPosition))
        {
            if (!HexGridMap.Instance.TryGetTile(neighbor, out var tile) || tile.Occupant == null)
                continue;
            if (CanEmbark(passenger, tile.Occupant))
                return tile.Occupant;
        }

        return null;
    }

    static bool ContainsPassenger(Unit galley, Unit passenger)
    {
        foreach (var embarked in galley.EmbarkedPassengers)
        {
            if (embarked == passenger)
                return true;
        }
        return false;
    }
}
