using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>City borders, tile ownership, and auto-assigned worked tiles.</summary>
public class TerritoryManager : MonoBehaviour
{
    public static TerritoryManager Instance { get; private set; }

    readonly Dictionary<HexCoordinates, City> hexOwner = new();
    readonly Dictionary<City, HashSet<HexCoordinates>> cityTerritory = new();
    readonly Dictionary<City, HashSet<HexCoordinates>> cityWorked = new();

    void Awake() => Instance = this;

    void Start()
    {
        // Catch cities registered before this component existed (bootstrap order / hot add).
        if (CityManager.Instance == null)
            return;

        foreach (var city in CityManager.Instance.AllCities)
        {
            if (city == null)
                continue;
            cityTerritory.TryAdd(city, new HashSet<HexCoordinates>());
            cityWorked.TryAdd(city, new HashSet<HexCoordinates>());
        }

        RefreshAll();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnCityRegistered(City city)
    {
        if (city == null) return;
        cityTerritory.TryAdd(city, new HashSet<HexCoordinates>());
        cityWorked.TryAdd(city, new HashSet<HexCoordinates>());
        RefreshAll();
    }

    public void OnCityUnregistered(City city)
    {
        if (city == null) return;
        cityTerritory.Remove(city);
        cityWorked.Remove(city);
        RefreshAll();
    }

    public void RefreshAll()
    {
        if (HexGridMap.Instance == null || CityManager.Instance == null)
            return;

        hexOwner.Clear();
        foreach (var set in cityTerritory.Values)
            set.Clear();
        foreach (var set in cityWorked.Values)
            set.Clear();

        var cities = CityManager.Instance.AllCities
            .Where(c => c != null)
            .OrderByDescending(c => c.IsCapital)
            .ThenByDescending(c => c.Population)
            .ToList();

        foreach (var city in cities.Where(c => c.IsIndependentCity))
            ExpandTerritory(city);

        IntegrateHamletDistricts(cities);

        foreach (var city in cities.Where(c => c.IsIndependentCity))
            AssignWorkedTiles(city);

        ApplyTileVisuals();
    }

    public City GetOwner(HexCoordinates hex)
    {
        hex = HexGridMap.Instance != null ? HexGridMap.Instance.Wrap(hex) : hex;
        return hexOwner.TryGetValue(hex, out var city) ? city : null;
    }

    public bool IsWorkedBy(HexCoordinates hex, City city)
    {
        return city != null &&
               cityWorked.TryGetValue(city, out var worked) &&
               worked.Contains(HexGridMap.Instance.Wrap(hex));
    }

    public IReadOnlyCollection<HexCoordinates> GetTerritory(City city) =>
        city != null && cityTerritory.TryGetValue(city, out var set) ? set : System.Array.Empty<HexCoordinates>();

    public IReadOnlyCollection<HexCoordinates> GetWorkedTiles(City city) =>
        city != null && cityWorked.TryGetValue(city, out var set) ? set : System.Array.Empty<HexCoordinates>();

    public TileYield GetWorkedYieldTotal(City city)
    {
        var total = default(TileYield);
        if (city == null || !cityWorked.TryGetValue(city, out var worked))
            return total;

        foreach (var hex in worked)
        {
            if (HexGridMap.Instance != null &&
                HexGridMap.Instance.TryGetTile(hex, out var tile))
            {
                total += TileYieldDatabase.GetTileYield(tile);
            }
        }

        return total;
    }

    public int GetTerritoryTileCount(City city) =>
        city != null && cityTerritory.TryGetValue(city, out var set) ? set.Count : 0;

    public int GetTerritoryCap(City city)
    {
        if (city == null || city.IsHamlet) return 0;
        int fromCulture = 6 + Mathf.FloorToInt(city.CulturePoints / 8f);
        return Mathf.Max(7, fromCulture);
    }

    void IntegrateHamletDistricts(System.Collections.Generic.List<City> cities)
    {
        foreach (var hamlet in cities.Where(c => c.IsHamlet))
        {
            var parent = hamlet.ControllingCity;
            if (parent == null || parent == hamlet)
                continue;
            if (!cityTerritory.TryGetValue(parent, out var owned))
                continue;

            var hex = HexGridMap.Instance.Wrap(hamlet.HexPosition);
            if (HexGridMap.Instance.WrappedDistance(hex, parent.HexPosition) > CityManager.MaxTerritoryRadius)
                continue;
            if (!CanClaim(parent, hex))
                continue;

            owned.Add(hex);
            hexOwner[hex] = parent;
        }
    }

    public string FormatManuscriptWorkedTiles(City city)
    {
        if (city == null || !cityWorked.TryGetValue(city, out var worked))
            return null;

        var parts = new System.Collections.Generic.List<string>();
        foreach (var hex in worked)
        {
            if (HexGridMap.Instance == null || !HexGridMap.Instance.TryGetTile(hex, out var tile))
                continue;

            var yield = TileYieldDatabase.GetTileYield(tile);
            if (yield.Manuscripts <= 0)
                continue;

            string label = tile.Resource != MapResourceType.None
                ? MapResourceDatabase.DisplayNameForPlayer(tile.Resource)
                : HexGridMap.TerrainDisplayName(tile.Terrain);
            parts.Add($"{label} +{yield.Manuscripts}/turn");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public string FormatManuscriptTilesInTerritory(City city)
    {
        if (city == null || !cityTerritory.TryGetValue(city, out var territory))
            return null;

        var parts = new System.Collections.Generic.List<string>();
        foreach (var hex in territory)
        {
            if (HexGridMap.Instance == null || !HexGridMap.Instance.TryGetTile(hex, out var tile))
                continue;

            var yield = TileYieldDatabase.GetTileYield(tile);
            if (yield.Manuscripts <= 0)
                continue;

            bool worked = cityWorked.TryGetValue(city, out var workedSet) && workedSet.Contains(hex);
            string label = tile.Resource != MapResourceType.None
                ? MapResourceDatabase.DisplayNameForPlayer(tile.Resource)
                : HexGridMap.TerrainDisplayName(tile.Terrain);
            parts.Add(worked
                ? $"{label} +{yield.Manuscripts}/turn"
                : $"{label} (+{yield.Manuscripts} unworked)");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public int GetWorkedTileCap(City city)
    {
        if (city == null) return 0;
        return Mathf.Max(1, CityGrowthSystem.GetTotalWorkers(city));
    }

    void ExpandTerritory(City city)
    {
        if (!cityTerritory.TryGetValue(city, out var owned))
        {
            owned = new HashSet<HexCoordinates>();
            cityTerritory[city] = owned;
        }

        int cap = GetTerritoryCap(city);
        var start = HexGridMap.Instance.Wrap(city.HexPosition);
        var visited = new HashSet<HexCoordinates>();
        var queue = new Queue<HexCoordinates>();
        queue.Enqueue(start);

        while (queue.Count > 0 && owned.Count < cap)
        {
            var hex = queue.Dequeue();
            if (!visited.Add(hex))
                continue;

            if (!CanClaim(city, hex))
                continue;

            owned.Add(hex);
            hexOwner[hex] = city;

            foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(hex))
            {
                if (!visited.Contains(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
    }

    bool CanClaim(City city, HexCoordinates hex)
    {
        if (HexGridMap.Instance.WrappedDistance(hex, city.HexPosition) > CityManager.MaxTerritoryRadius)
            return false;

        if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
            return false;
        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;

        if (tile.Settlement != null && tile.Settlement != city)
        {
            if (tile.Settlement.Faction != city.Faction)
                return false;
            if (tile.Settlement.IsHamlet && tile.Settlement.ControllingCity == city)
                return true;
            return false;
        }

        if (hexOwner.TryGetValue(hex, out var existing))
        {
            if (existing.Faction != city.Faction)
                return false;
            if (existing != city)
                return false;
        }

        return true;
    }

    void AssignWorkedTiles(City city)
    {
        if (!cityTerritory.TryGetValue(city, out var territory) ||
            !cityWorked.TryGetValue(city, out var worked))
        {
            return;
        }

        worked.Clear();
        int cap = GetWorkedTileCap(city);
        var candidates = new List<(HexCoordinates hex, int food, int prod, int mss)>();

        foreach (var hex in territory)
        {
            if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
                continue;

            var yield = TileYieldDatabase.GetTileYield(tile);
            candidates.Add((hex, yield.Food, yield.Production, yield.Manuscripts));
        }

        foreach (var entry in candidates
                     .OrderByDescending(c => c.food)
                     .ThenByDescending(c => c.prod)
                     .ThenByDescending(c => c.mss)
                     .ThenByDescending(c => c.hex == city.HexPosition)
                     .Take(cap))
            worked.Add(entry.hex);
    }

    public void RefreshTileVisuals() => ApplyTileVisuals();

    void ApplyTileVisuals()
    {
        if (HexGridMap.Instance == null) return;

        foreach (var tile in HexGridMap.Instance.AllTiles)
        {
            var owner = GetOwner(tile.Coordinates);
            bool worked = owner != null && IsWorkedBy(tile.Coordinates, owner);
            tile.SetTerritoryVisual(owner, worked);
        }
    }
}
