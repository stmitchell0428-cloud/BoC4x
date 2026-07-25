using System.Collections.Generic;
using UnityEngine;

public class CityManager : MonoBehaviour
{
    public static CityManager Instance { get; private set; }

    /// <summary>Land borders cannot extend farther than this many hexes from the city center.</summary>
    public const int MaxTerritoryRadius = 4;

    /// <summary>New cities must be at least this many hexes from every existing city.</summary>
    public const int MinCitySeparation = 6;

    readonly List<City> cities = new();
    int hamletCounter;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Register(City city)
    {
        if (!cities.Contains(city))
        {
            cities.Add(city);
            TerritoryManager.Instance?.OnCityRegistered(city);
        }
    }

    public void Unregister(City city)
    {
        cities.Remove(city);
        TerritoryManager.Instance?.OnCityUnregistered(city);
    }

    public IReadOnlyList<City> AllCities => cities;

    public City GetCityByName(string name)
    {
        foreach (var city in cities)
            if (city.CityName == name)
                return city;
        return null;
    }

    public City GetPrimaryPlayerCity()
    {
        City capital = null;
        City firstIndependent = null;
        foreach (var city in cities)
        {
            if (city.Faction != FactionId.LutheranSynod)
                continue;
            if (city.IsCapital)
                return city;
            if (!city.IsHamlet && firstIndependent == null)
                firstIndependent = city;
            if (capital == null)
                capital = city;
        }
        return firstIndependent ?? capital;
    }

    public System.Collections.Generic.List<City> GetPlayerCities()
    {
        return GetCitiesForFaction(FactionId.LutheranSynod);
    }

    public System.Collections.Generic.List<City> GetCitiesForFaction(FactionId faction)
    {
        var list = new System.Collections.Generic.List<City>();
        foreach (var city in cities)
        {
            if (city.Faction == faction)
                list.Add(city);
        }
        return list;
    }

    public City GetCityForUnit(Unit unit)
    {
        if (unit == null || HexGridMap.Instance == null)
            return null;

        if (HexGridMap.Instance.TryGetTile(unit.HexPosition, out var tile) &&
            tile.Settlement != null &&
            tile.Settlement.Faction == FactionId.LutheranSynod)
        {
            return tile.Settlement;
        }

        City nearest = null;
        int best = int.MaxValue;
        foreach (var city in GetPlayerCities())
        {
            int d = HexGridMap.Instance.WrappedDistance(unit.HexPosition, city.HexPosition);
            if (d < best)
            {
                best = d;
                nearest = city;
            }
        }
        return nearest;
    }

    public void GrowPlayerCities()
    {
        CityGrowthManager.Instance?.ProcessPlayerEndTurn();
        CityGrowthManager.Instance?.TickCooldowns();
    }

    public void AdvanceCityCulture()
    {
        var faction = FirstSteps.Instance;
        float adherence = faction != null ? faction.ConfessionalAdherence : 50f;

        foreach (var city in cities)
        {
            if (city == null) continue;
            if (city.Faction == FactionId.LutheranSynod)
                city.AdvanceCulture(adherence);
            else if (city.Faction == FactionId.Schismatic)
                city.AdvanceCulture(40f);
        }

        TerritoryManager.Instance?.RefreshAll();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    public void CollectWorkedTileManuscripts()
    {
        var faction = FirstSteps.Instance;
        if (faction == null) return;

        int total = 0;
        foreach (var city in GetPlayerCities())
        {
            var breakdown = city.GetProductionBreakdown();
            if (breakdown.FromManuscripts > 0)
                total += breakdown.FromManuscripts;
        }

        if (total > 0)
        {
            faction.ScriptureManuscripts += total;
            Debug.Log($"Worked tiles yielded +{total} manuscripts.");
        }
    }

    public string FormatPlayerCityYieldLine()
    {
        var summaries = new List<string>();
        foreach (var city in cities)
        {
            if (city.Faction != FactionId.LutheranSynod)
                continue;

            var line = city.ProductionBreakdownLabel();
            if (!city.IsHamlet)
            {
                string growth = city.GrowthSummaryLabel();
                if (!string.IsNullOrEmpty(growth))
                    line += "\n" + growth;
            }

            summaries.Add(line);
        }
        return summaries.Count > 0 ? string.Join("\n", summaries) : "City production:  - ";
    }

    public string FormatPlayerProductionQueueLine()
    {
        var parts = new List<string>();
        foreach (var city in cities)
        {
            if (city.Faction != FactionId.LutheranSynod)
                continue;
            if (city.Production == null || !city.Production.IsProducing)
                continue;
            parts.Add($"{city.CityName}: {city.Production.ActiveBuildHudLabel()}");
        }

        return parts.Count > 0 ? string.Join("  |  ", parts) : "idle";
    }

    public string FormatPlayerCityStatusLine()
    {
        string queue = FormatPlayerProductionQueueLine();
        string yield = FormatPlayerCityYieldLine();
        if (queue == "City queue: idle")
            return yield;
        return $"{queue}\n{yield}";
    }

    public void AdvanceFactionCities(FactionId faction)
    {
        // Snapshot  -  Found Hamlet (and other builds) can Register new cities during AdvanceTurn.
        var snapshot = new List<City>(cities);
        foreach (var city in snapshot)
        {
            if (city != null && city.Faction == faction)
                city.Production?.AdvanceTurn();
        }
    }

    public void AdvanceBlocCity(SchismaticBlocId blocId)
    {
        var city = GetAiCity(blocId);
        city?.Production?.AdvanceTurn();
    }

    public void AdvancePlayerCities() => AdvanceFactionCities(FactionId.LutheranSynod);

    public void TryCaptureCityAt(Unit unit, HexCoordinates hex)
    {
        if (unit == null || (unit.Type != UnitType.Soldier && unit.Type != UnitType.Defender &&
            unit.Type != UnitType.Archer && unit.Type != UnitType.Horseman && unit.Type != UnitType.Slinger) ||
            !unit.IsAlive) return;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile) || tile.Settlement == null) return;
        if (tile.Settlement.Faction == unit.Faction) return;
        if (tile.Occupant != null && tile.Occupant != unit) return;

        CityLoyaltySystem.TryApplyPressure(unit, tile.Settlement, isPreach: false);
    }

    public void TryPreachCityAt(Unit unit, HexCoordinates hex)
    {
        if (unit == null || !unit.CanPreachOrHymn || !unit.IsAlive) return;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile) || tile.Settlement == null) return;
        if (tile.Settlement.Faction == unit.Faction) return;
        if (tile.Occupant != null && tile.Occupant != unit) return;

        CityLoyaltySystem.TryApplyPressure(unit, tile.Settlement, isPreach: true);
    }

    public bool TrySpawnUnit(City city, UnitType type)
    {
        if (HexGridMap.Instance == null || TurnManager.Instance == null)
            return false;

        HexCoordinates? spawnHex = FindUnitSpawnHex(city, type);
        if (!spawnHex.HasValue)
            return false;

        var go = new GameObject($"{city.Faction}_{type}");
        go.transform.SetParent(transform);
        var unit = go.AddComponent<Unit>();
        unit.Initialize(city.Faction, type, spawnHex.Value);
        if (city.Faction == FactionId.Schismatic && city.SchismaticBloc != SchismaticBlocId.None)
            unit.SetSchismaticBloc(city.SchismaticBloc);
        TurnManager.Instance.RegisterUnit(unit);

        if (ClergyRoster.IsClergyUnit(type))
            ClergyRoster.RegisterUnit(unit, city);

        if (city.Faction == FactionId.LutheranSynod && type == UnitType.Missionary)
            FirstSteps.Instance?.BindPlayerUnit(unit);

        ConfessionResearchManager.Instance?.ApplyBonusesToAllPlayerUnits();
        FogOfWarManager.Instance?.Refresh();
        return true;
    }

    HexCoordinates? FindUnitSpawnHex(City city, UnitType type)
    {
        if (HexGridMap.Instance == null || city == null)
            return null;

        HexCoordinates? fallback = null;
        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(city.HexPosition))
        {
            if (!HexGridMap.Instance.TryGetTile(neighbor, out var tile))
                continue;
            if (tile.Occupant != null || tile.Settlement != null)
                continue;

            if (NavalMovementRules.IsNavalUnit(type))
            {
                if (!NavalMovementRules.CanEnterTile(type, tile))
                    continue;

                if (TerrainRules.IsWater(tile.Terrain) ||
                    tile.Terrain == TerrainType.Shore ||
                    tile.IsNavalCoast)
                    return neighbor;

                fallback ??= neighbor;
                continue;
            }

            if (!TerrainRules.IsPassable(tile.Terrain))
                continue;

            return neighbor;
        }

        return fallback;
    }

    public bool TryFoundCityFromNomadicSettler(Unit settler, string cityName = "Wittenberg")
    {
        if (settler == null || !settler.CanFoundNomadicCapital)
            return false;

        var hex = settler.HexPosition;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
            return false;
        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;
        if (tile.Settlement != null)
            return false;
        if (tile.Occupant != settler)
            return false;
        if (IsTooCloseToIndependentCity(hex))
        {
            Debug.LogWarning($"Cannot found {cityName}: must be at least {MinCitySeparation} hexes from any other city.");
            return false;
        }

        var go = new GameObject($"City_{cityName}");
        go.transform.SetParent(transform);
        go.AddComponent<City>().Initialize(settler.Faction, hex, cityName, isCapital: true);

        settler.ConvertToMissionaryAfterFounding();

        FirstSteps.Instance?.AddFame(15);
        IdentityPickerPanel.Instance?.Show();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        FogOfWarManager.Instance?.Refresh();
        Debug.Log($"Founded {cityName}. The nomadic settler is now a missionary.");
        return true;
    }

    public bool TryFoundHamletFromColonist(Unit colonist)
    {
        if (colonist == null || !colonist.CanFoundHamlet)
            return false;

        var hex = colonist.HexPosition;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
            return false;
        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;
        if (tile.Settlement != null)
            return false;
        if (tile.Occupant != colonist)
            return false;

        var parent = GetNearestPlayerCity(hex);
        if (parent == null || !IsValidHamletDistrictSite(hex, parent))
        {
            Debug.LogWarning(
                $"Cannot found district: need open land within 3 hexes of your city and {MinCitySeparation}+ hexes from other cities.");
            return false;
        }

        hamletCounter++;
        var go = new GameObject($"City_Hamlet_{hamletCounter}");
        go.transform.SetParent(transform);
        var city = go.AddComponent<City>();
        city.Initialize(colonist.Faction, hex, $"District {hamletCounter}", startingPopulation: 10, parentCity: parent);

        colonist.ClearTileForFounding();
        TurnManager.Instance?.UnregisterUnit(colonist);
        Destroy(colonist.gameObject);

        FirstSteps.Instance?.AddFame(5);
        FirstSteps.Instance?.RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        FogOfWarManager.Instance?.Refresh();
        DistrictSpecialtyPickerPanel.Instance?.Show(city);
        Debug.Log($"Founded {city.SettlementDisplayName()} (district of {parent.CityName}).");
        return true;
    }

    public bool TryFoundOrganicDistrict(City parent, HexCoordinates hex, HamletSpecialty suggestedSpecialty)
    {
        if (parent == null || parent.IsHamlet || HexGridMap.Instance == null)
            return false;

        hex = HexGridMap.Instance.Wrap(hex);
        if (!IsValidHamletDistrictSite(hex, parent))
        {
            Debug.LogWarning("Organic district site no longer valid.");
            return false;
        }

        if (HexGridMap.Instance.TryGetTile(hex, out var tile) && tile.Occupant != null)
        {
            Debug.LogWarning("Organic district site is occupied.");
            return false;
        }

        hamletCounter++;
        var go = new GameObject($"City_Hamlet_{hamletCounter}");
        go.transform.SetParent(transform);
        var city = go.AddComponent<City>();
        city.Initialize(parent.Faction, hex, $"District {hamletCounter}", startingPopulation: 10, parentCity: parent);

        FirstSteps.Instance?.AddFame(5);
        FirstSteps.Instance?.RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        FogOfWarManager.Instance?.Refresh();
        DistrictSpecialtyPickerPanel.Instance?.Show(city, suggestedSpecialty);
        Debug.Log($"Organic district at {hex} for {parent.CityName} (suggested {suggestedSpecialty}).");
        return true;
    }

    public bool IsTooCloseToIndependentCity(HexCoordinates hex, City ignore = null)
    {
        if (HexGridMap.Instance == null)
            return false;

        hex = HexGridMap.Instance.Wrap(hex);
        foreach (var city in cities)
        {
            if (city == null || city == ignore || city.IsHamlet)
                continue;
            if (HexGridMap.Instance.WrappedDistance(hex, city.HexPosition) < MinCitySeparation)
                return true;
        }
        return false;
    }

    public bool IsValidHamletDistrictSite(HexCoordinates hex, City parentCity)
    {
        if (parentCity == null || HexGridMap.Instance == null)
            return false;

        parentCity = parentCity.ControllingCity;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
            return false;
        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;
        if (tile.Settlement != null)
            return false;
        if (HexGridMap.Instance.WrappedDistance(hex, parentCity.HexPosition) > 3)
            return false;
        return !IsTooCloseToIndependentCity(hex);
    }

    public City GetNearestPlayerCity(HexCoordinates hex, bool independentOnly = false)
    {
        if (HexGridMap.Instance == null)
            return null;

        City nearest = null;
        int best = int.MaxValue;
        foreach (var city in GetPlayerCities())
        {
            if (independentOnly && city.IsHamlet)
                continue;

            int d = HexGridMap.Instance.WrappedDistance(hex, city.ControllingCity.HexPosition);
            if (d < best)
            {
                best = d;
                nearest = city;
            }
        }
        return nearest?.ControllingCity;
    }

    public bool IsTooCloseToExistingCity(HexCoordinates hex, City ignore = null) =>
        IsTooCloseToIndependentCity(hex, ignore);

    public bool IsValidNewCitySite(HexCoordinates hex)
    {
        if (HexGridMap.Instance == null)
            return false;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
            return false;
        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;
        if (tile.Settlement != null)
            return false;
        return !IsTooCloseToExistingCity(hex);
    }

    public bool IsNearPlayerCity(HexCoordinates hex, int maxDistance)
    {
        if (HexGridMap.Instance == null) return false;
        foreach (var city in GetPlayerCities())
        {
            if (HexGridMap.Instance.WrappedDistance(hex, city.ControllingCity.HexPosition) <= maxDistance)
                return true;
        }
        return false;
    }

    public bool IsUnitOnCity(Unit unit, City city)
    {
        if (unit == null || city == null || HexGridMap.Instance == null) return false;
        return unit.HexPosition == city.HexPosition;
    }

    public bool IsOnFortifiedCityTile(Unit unit)
    {
        if (unit == null || HexGridMap.Instance == null) return false;
        if (!HexGridMap.Instance.TryGetTile(unit.HexPosition, out var tile) || tile.Settlement == null)
            return false;
        if (tile.Settlement.Faction != unit.Faction) return false;
        return tile.Settlement.Production != null &&
               tile.Settlement.Production.HasBuilding(CityBuildId.BuildFortification);
    }

    public bool HasAnyPlayerBuilding(CityBuildId id)
    {
        foreach (var city in GetPlayerCities())
        {
            if (city.Production != null && city.Production.HasBuilding(id))
                return true;
        }
        return false;
    }

    public bool CityTouchesNavalCoast(City city)
    {
        if (city == null || HexGridMap.Instance == null)
            return false;

        if (HexGridMap.Instance.TryGetTile(city.HexPosition, out var center) &&
            (center.IsNavalCoast || center.Terrain == TerrainType.Shore))
            return true;

        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(city.HexPosition))
        {
            if (HexGridMap.Instance.TryGetTile(neighbor, out var tile) &&
                (tile.IsNavalCoast || tile.Terrain == TerrainType.Shore))
                return true;
        }

        return false;
    }

    public int GetResearchAcceleration()
    {
        int bonus = 0;
        foreach (var city in GetPlayerCities())
        {
            if (city.Production == null) continue;
            if (city.Production.HasBuilding(CityBuildId.BuildSeminary)) bonus++;
            if (city.Production.HasBuilding(CityBuildId.BuildUniversity)) bonus++;
        }
        return Mathf.Min(bonus, 2);
    }

    public void CollectHamletTribute()
    {
        var faction = FirstSteps.Instance;
        if (faction == null) return;

        MissionHouseChain.ProcessEndTurnFame();

        int totalMss = 0;
        int totalFame = 0;
        foreach (var city in GetPlayerCities())
        {
            if (!city.IsHamlet) continue;
            int tribute = city.GetProductionPerTurn();
            int mss = Mathf.Max(1, tribute / 2);
            totalMss += mss;
            totalFame += tribute;
        }

        if (totalMss > 0)
            faction.ScriptureManuscripts += totalMss;
        if (totalFame > 0)
            faction.AddFame(totalFame / 2);

        if (totalMss > 0 || totalFame > 0)
            Debug.Log($"Hamlet tribute: +{totalMss} manuscripts, +{totalFame / 2} fame.");
    }

    public bool TryFoundHamlet(City origin)
    {
        if (origin == null || HexGridMap.Instance == null) return false;

        var parent = origin.ControllingCity;
        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(parent.HexPosition))
        {
            if (!IsValidHamletDistrictSite(neighbor, parent))
                continue;
            if (HexGridMap.Instance.TryGetTile(neighbor, out var tile) && tile.Occupant != null)
                continue;

            hamletCounter++;
            var go = new GameObject($"City_Hamlet_{hamletCounter}");
            go.transform.SetParent(transform);
            var city = go.AddComponent<City>();
            city.Initialize(parent.Faction, neighbor, $"District {hamletCounter}", startingPopulation: 10, parentCity: parent);
            Debug.Log($"Founded {city.SettlementDisplayName()} adjacent to {parent.CityName}.");
            FirstSteps.Instance?.RefreshDashboard();
            TerrainInfoPanel.Instance?.RefreshCityYield();
            FogOfWarManager.Instance?.Refresh();
            DistrictSpecialtyPickerPanel.Instance?.Show(city);
            return true;
        }

        Debug.LogWarning($"No open adjacent hex for a district near {parent.CityName}.");
        return false;
    }

    public City GetAiCity(SchismaticBlocId blocId = SchismaticBlocId.None)
    {
        foreach (var city in cities)
        {
            if (city.Faction != FactionId.Schismatic)
                continue;
            if (blocId == SchismaticBlocId.None || city.SchismaticBloc == blocId)
                return city;
        }
        return null;
    }
}
