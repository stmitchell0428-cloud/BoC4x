using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    readonly HashSet<HexCoordinates> explored = new();
    readonly HashSet<HexCoordinates> visible = new();

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsExplored(HexCoordinates coords) => explored.Contains(coords);

    public bool IsVisible(HexCoordinates coords) => visible.Contains(coords);

    public FogVisibility GetVisibility(HexCoordinates coords)
    {
        if (visible.Contains(coords)) return FogVisibility.Visible;
        if (explored.Contains(coords)) return FogVisibility.Explored;
        return FogVisibility.Unexplored;
    }

    public void Refresh()
    {
        if (HexGridMap.Instance == null || TurnManager.Instance == null)
            return;

        visible.Clear();

        int exploredBefore = explored.Count;

        foreach (var unit in TurnManager.Instance.GetUnits(FactionId.LutheranSynod).Where(u => u.IsAlive && u.SynodPlayer == SynodPlayerId.Player1))
            RevealAround(unit.HexPosition, unit.SightRange);

        if (CityManager.Instance != null)
        {
            foreach (var city in CityManager.Instance.AllCities)
            {
                if (city.Faction == FactionId.LutheranSynod && city.SynodPlayer == SynodPlayerId.Player1)
                    RevealAround(city.HexPosition, GarrisonBonus.GetCitySightRange(city));
            }
        }

        foreach (var coords in visible)
            explored.Add(coords);

        if (explored.Count > exploredBefore)
            CityPlacementAdvisor.InvalidateCache();

        ApplyToMap();
    }

    void RevealAround(HexCoordinates center, int range)
    {
        if (HexGridMap.Instance == null || range < 0)
            return;

        center = HexGridMap.Instance.Wrap(center);
        var visited = new HashSet<HexCoordinates> { center };
        var queue = new Queue<(HexCoordinates coords, int distance)>();
        queue.Enqueue((center, 0));

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();
            visible.Add(current);
            if (distance >= range)
                continue;

            foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(current))
            {
                if (!visited.Add(neighbor))
                    continue;
                queue.Enqueue((neighbor, distance + 1));
            }
        }
    }

    void ApplyToMap()
    {
        if (HexGridMap.Instance == null) return;

        foreach (var tile in HexGridMap.Instance.AllTiles)
            tile.SetFogVisibility(GetVisibility(tile.Coordinates));

        UpdateEntityVisibility();
    }

    void UpdateEntityVisibility()
    {
        if (TurnManager.Instance == null) return;

        foreach (var faction in new[] { FactionId.LutheranSynod, FactionId.Schismatic })
        {
            foreach (var unit in TurnManager.Instance.GetUnits(faction).Where(u => u.IsAlive))
            {
                bool hide = unit.Faction == FactionId.Schismatic && !IsVisible(unit.HexPosition);
                hide |= unit.Faction == FactionId.LutheranSynod &&
                        unit.SynodPlayer != SynodPlayerId.Player1 &&
                        !IsVisible(unit.HexPosition);
                unit.SetFogHidden(hide);
            }
        }

        if (CityManager.Instance == null) return;

        foreach (var city in CityManager.Instance.AllCities)
        {
            bool hide = city.Faction == FactionId.Schismatic && !IsExplored(city.HexPosition);
            hide |= city.Faction == FactionId.LutheranSynod &&
                    city.SynodPlayer != SynodPlayerId.Player1 &&
                    !IsExplored(city.HexPosition);
            city.SetFogHidden(hide);
        }
    }
}
