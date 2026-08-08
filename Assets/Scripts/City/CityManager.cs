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
    City playerCapital;

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
            if (city.Faction != FactionId.LutheranSynod || city.SynodPlayer != SynodPlayerId.Player1)
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

    public System.Collections.Generic.List<City> GetPlayerCities() =>
        GetSynodPlayerCities(SynodPlayerId.Player1);

    public System.Collections.Generic.List<City> GetSynodPlayerCities(SynodPlayerId playerId)
    {
        var list = new System.Collections.Generic.List<City>();
        foreach (var city in cities)
        {
            if (city.Faction == FactionId.LutheranSynod && city.SynodPlayer == playerId)
                list.Add(city);
        }
        return list;
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
            tile.Settlement.Faction == FactionId.LutheranSynod &&
            tile.Settlement.SynodPlayer == unit.SynodPlayer)
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

        string trade = SynodTradeSystem.FormatNetworkSummary(SynodPlayerId.Player1);
        if (!string.IsNullOrEmpty(trade))
            summaries.Add(trade);

        return summaries.Count > 0 ? string.Join("\n", summaries) : "City production:  - ";
    }

    public bool HasPlayerCityProduction()
    {
        foreach (var city in cities)
        {
            if (city.Faction == FactionId.LutheranSynod)
                return true;
        }

        return false;
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

    public string FormatProminentBuildQueueBlock()
    {
        var lines = new List<string>();
        foreach (var city in cities)
        {
            if (city.Faction != FactionId.LutheranSynod)
                continue;
            if (city.Production == null || !city.Production.IsProducing)
                continue;

            lines.Add(
                $"  <color=#FFE8AA>{city.CityName}</color>  " +
                $"<color=#FFFFFF>{city.Production.ActiveBuildHudLabel()}</color>");
        }

        if (lines.Count == 0)
        {
            return "<size=21><color=#FFAA66><b>BUILD</b></color></size>  " +
                   "<color=#FF9988>none queued</color>  <size=15><color=#99AABB>(C — city screen)</color></size>";
        }

        return "<size=21><color=#FFCC55><b>BUILD</b></color></size>\n" + string.Join("\n", lines);
    }

    public string FormatCompactBuildSummary()
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

        return parts.Count > 0
            ? string.Join("  ·  ", parts)
            : "<color=#FFAA88>build idle</color>";
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

    public void AdvanceSynodPlayerCities(SynodPlayerId playerId)
    {
        foreach (var city in GetSynodPlayerCities(playerId))
            city.Production?.AdvanceTurn();
    }

    public City GetSynodPlayerCapital(SynodPlayerId playerId)
    {
        if (playerId == SynodPlayerId.Player1 && playerCapital != null)
            return playerCapital;

        foreach (var city in GetSynodPlayerCities(playerId))
        {
            if (city.IsCapital)
                return city;
        }

        foreach (var city in GetSynodPlayerCities(playerId))
        {
            if (!city.IsHamlet)
                return city;
        }

        return null;
    }

    /// <summary>Human player's founded capital (not hardcoded to Wittenberg).</summary>
    public City PlayerCapital => playerCapital != null ? playerCapital : GetSynodPlayerCapital(SynodPlayerId.Player1);

    public void RegisterPlayerCapital(City city)
    {
        if (city == null || city.SynodPlayer != SynodPlayerId.Player1 || !city.IsCapital)
            return;

        playerCapital = city;
    }

    public void AdvancePlayerCities() => AdvanceSynodPlayerCities(SynodPlayerId.Player1);

    public void TryCaptureCityAt(Unit unit, HexCoordinates hex)
    {
        if (unit == null || (unit.Type != UnitType.Soldier && unit.Type != UnitType.Defender &&
            unit.Type != UnitType.Archer && unit.Type != UnitType.Horseman && unit.Type != UnitType.Slinger) ||
            !unit.IsAlive) return;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile)) return;

        City city = tile.Settlement;
        if (city == null && HexGridMap.Instance != null)
        {
            // Adjacent siege against walled cities.
            foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(hex))
            {
                if (!HexGridMap.Instance.TryGetTile(neighbor, out var nTile) || nTile.Settlement == null)
                    continue;
                if (CityDefenses.CanPressCityFrom(unit, nTile.Settlement))
                {
                    city = nTile.Settlement;
                    break;
                }
            }
        }

        if (city == null) return;
        if (!CityDefenses.CanPressCityFrom(unit, city) && unit.HexPosition != city.HexPosition)
            return;
        if (city.Faction == unit.Faction &&
            (unit.Faction != FactionId.LutheranSynod || city.SynodPlayer == unit.SynodPlayer))
            return;
        if (HexGridMap.Instance.TryGetTile(city.HexPosition, out var cityTile) &&
            cityTile.Occupant != null && cityTile.Occupant != unit &&
            !CityDefenses.HasWalls(city))
            return;

        CityLoyaltySystem.TryApplyPressure(unit, city, isPreach: false);
    }

    public void TryPreachCityAt(Unit unit, HexCoordinates hex)
    {
        if (unit == null || !unit.CanPreachOrHymn || !unit.IsAlive) return;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile)) return;

        City city = tile.Settlement;
        if (city == null && HexGridMap.Instance != null)
        {
            foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(hex))
            {
                if (!HexGridMap.Instance.TryGetTile(neighbor, out var nTile) || nTile.Settlement == null)
                    continue;
                if (CityDefenses.CanPressCityFrom(unit, nTile.Settlement))
                {
                    city = nTile.Settlement;
                    break;
                }
            }
        }

        if (city == null) return;
        if (!CityDefenses.CanPressCityFrom(unit, city) && unit.HexPosition != city.HexPosition)
            return;
        if (city.Faction == unit.Faction &&
            (unit.Faction != FactionId.LutheranSynod || city.SynodPlayer == unit.SynodPlayer))
            return;
        if (HexGridMap.Instance.TryGetTile(city.HexPosition, out var cityTile) &&
            cityTile.Occupant != null && cityTile.Occupant != unit &&
            !CityDefenses.HasWalls(city))
            return;

        CityLoyaltySystem.TryApplyPressure(unit, city, isPreach: true);
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
        unit.Initialize(city.Faction, type, spawnHex.Value, synodPlayer: city.SynodPlayer);
        if (city.Faction == FactionId.Schismatic && city.SchismaticBloc != SchismaticBlocId.None)
            unit.SetSchismaticBloc(city.SchismaticBloc);
        TurnManager.Instance.RegisterUnit(unit);

        if (ClergyRoster.IsClergyUnit(type))
            ClergyRoster.RegisterUnit(unit, city);

        if (city.Faction == FactionId.LutheranSynod &&
            city.SynodPlayer == SynodPlayerId.Player1 &&
            type == UnitType.Missionary)
            FirstSteps.Instance?.BindPlayerUnit(unit);

        ConfessionResearchManager.Instance?.ApplyBonusesToAllPlayerUnits();
        FogOfWarManager.Instance?.Refresh();
        return true;
    }

    HexCoordinates? FindUnitSpawnHex(City city, UnitType type)
    {
        if (HexGridMap.Instance == null || city == null)
            return null;

        HexCoordinates? bestNaval = null;
        int bestNavalScore = int.MinValue;
        HexCoordinates? fallback = null;

        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(city.HexPosition))
        {
            if (!HexGridMap.Instance.TryGetTile(neighbor, out var tile))
                continue;
            if (tile.Occupant != null || tile.Settlement != null)
                continue;

            if (NavalMovementRules.IsNavalUnit(type))
            {
                if (!NavalMovementRules.CanEnterTile(type, tile, city.Faction, city.SynodPlayer))
                    continue;

                int score = NavalSpawnScore(tile, type);
                if (score > bestNavalScore)
                {
                    bestNavalScore = score;
                    bestNaval = neighbor;
                }

                fallback ??= neighbor;
                continue;
            }

            if (!TerrainRules.IsPassable(tile.Terrain))
                continue;

            return neighbor;
        }

        return bestNaval ?? fallback;
    }

    static int NavalSpawnScore(HexTile tile, UnitType type)
    {
        // Prefer ocean (especially navigable coastal sea) over inland lakes.
        if (tile.Terrain == TerrainType.Ocean)
            return tile.IsNavigableWater || type == UnitType.DeepSeaShip ? 100 : 80;
        if (tile.Terrain == TerrainType.River)
            return 40;
        if (tile.Terrain == TerrainType.Lake)
            return 20;
        if (tile.Terrain == TerrainType.Shore || tile.IsNavalCoast)
            return 10;
        return 0;
    }

    /// <summary>Ocean shore / ocean-adjacent naval coast — not lakes alone.</summary>
    public bool CityTouchesOceanCoast(City city)
    {
        if (city == null || HexGridMap.Instance == null)
            return false;

        if (HexGridMap.Instance.TryGetTile(city.HexPosition, out var center) &&
            TileTouchesOcean(center))
            return true;

        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(city.HexPosition))
        {
            if (HexGridMap.Instance.TryGetTile(neighbor, out var tile) &&
                TileTouchesOcean(tile))
                return true;
        }

        return false;
    }

    static bool TileTouchesOcean(HexTile tile)
    {
        if (tile == null)
            return false;
        if (tile.Terrain == TerrainType.Ocean)
            return true;
        if (tile.Terrain == TerrainType.Shore)
        {
            // Shore next to ocean (not lake-only).
            foreach (var n in HexGridMap.Instance.GetWrappedNeighbors(tile.Coordinates))
            {
                if (HexGridMap.Instance.TryGetTile(n, out var nt) && nt.Terrain == TerrainType.Ocean)
                    return true;
            }
        }

        if (tile.IsNavalCoast)
        {
            foreach (var n in HexGridMap.Instance.GetWrappedNeighbors(tile.Coordinates))
            {
                if (HexGridMap.Instance.TryGetTile(n, out var nt) && nt.Terrain == TerrainType.Ocean)
                    return true;
            }
        }

        return false;
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
        var city = go.AddComponent<City>();
        city.Initialize(
            settler.Faction,
            hex,
            cityName,
            isCapital: true,
            startingPopulation: CityGrowthSystem.FoundingCapitalPopulation,
            synodPlayer: settler.SynodPlayer);

        settler.ConvertToMissionaryAfterFounding();

        if (settler.SynodPlayer == SynodPlayerId.Player1)
        {
            RegisterPlayerCapital(city);
            FirstSteps.Instance?.AddFame(10);
            IdentityPickerPanel.Instance?.Show();
        }
        PopulationSync.SyncPlayerFactionFromCities();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        FogOfWarManager.Instance?.Refresh();
        CityGrowthSystem.ProjectCapitalFoundingFood(
            hex,
            out int foodProduced,
            out int foodConsumed,
            out int foodSurplus);
        string foodNote = foodSurplus >= 0
            ? $"+{foodSurplus} food surplus"
            : $"{foodSurplus} food (deficit grace {CityGrowthSystem.CapitalDeficitGraceTurns} turns)";
        Debug.Log(
            $"Founded {cityName} (pop {CityGrowthSystem.FoundingCapitalPopulation}). " +
            $"Turn-1 food: {foodProduced}/{foodConsumed} ({foodNote}). The nomadic settler is now a missionary.");
        FirstSteps.Instance?.RefreshDashboard();
        GameHUD.Instance?.Relayout();
        TurnPhaseBanner.Instance?.Refresh();
        return true;
    }

    public bool TrySpawnFrontierSettler(City city)
    {
        if (city == null || !MissionHouseChain.CanTrainFrontierSettler(city))
            return false;

        HexCoordinates? spawnHex = FindUnitSpawnHex(city, UnitType.Settler);
        if (!spawnHex.HasValue || TurnManager.Instance == null)
            return false;

        var go = new GameObject($"{city.Faction}_FrontierSettler");
        go.transform.SetParent(transform);
        var unit = go.AddComponent<Unit>();
        unit.Initialize(city.Faction, UnitType.Settler, spawnHex.Value, synodPlayer: city.SynodPlayer, isFrontierSettler: true);
        TurnManager.Instance.RegisterUnit(unit);
        FogOfWarManager.Instance?.Refresh();
        return true;
    }

    public bool TryFoundCityFromFrontierSettler(Unit settler, string cityName = null)
    {
        if (settler == null || !settler.CanFoundFrontierCity)
            return false;

        var hex = settler.HexPosition;
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
            return false;
        if (!TerrainRules.IsPassable(tile.Terrain) || tile.Settlement != null || tile.Occupant != settler)
            return false;
        if (IsTooCloseToIndependentCity(hex))
        {
            Debug.LogWarning($"Cannot found second city: must be at least {MinCitySeparation} hexes from any other city.");
            return false;
        }

        cityName ??= PickFrontierCityName();
        var go = new GameObject($"City_{cityName}");
        go.transform.SetParent(transform);
        go.AddComponent<City>().Initialize(
            settler.Faction, hex, cityName, isCapital: false, synodPlayer: settler.SynodPlayer);

        settler.ConvertToMissionaryAfterFounding();

        if (settler.SynodPlayer == SynodPlayerId.Player1)
            FirstSteps.Instance?.AddFame(6);

        PopulationSync.SyncPlayerFactionFromCities();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        FogOfWarManager.Instance?.Refresh();
        Debug.Log($"Founded second city {cityName}. The frontier settler is now a missionary.");
        return true;
    }

    static int frontierCityCounter;

    static string PickFrontierCityName()
    {
        frontierCityCounter++;
        return frontierCityCounter switch
        {
            1 => "Leipzig",
            2 => "Erfurt",
            3 => "Halle",
            _ => $"Synod City {frontierCityCounter + 1}"
        };
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

        hamletCounter++;
        var go = new GameObject($"City_Hamlet_{hamletCounter}");
        go.transform.SetParent(transform);
        var city = go.AddComponent<City>();
        city.Initialize(parent.Faction, hex, $"District {hamletCounter}", startingPopulation: 10, parentCity: parent);

        FirstSteps.Instance?.AddFame(3);
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
        // Enemy units block founding; friendly / same-synod units do not.
        if (tile.Occupant != null && FactionRelations.IsHostileToCity(tile.Occupant, parentCity))
            return false;
        if (HexGridMap.Instance.WrappedDistance(hex, parentCity.HexPosition) > 3)
            return false;
        // Parent is ignored — districts sit near the capital; MinCitySeparation
        // only blocks other independent cities (≤3 and ≥6 from parent is impossible).
        return !IsTooCloseToIndependentCity(hex, ignore: parentCity);
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
        if (tile.Settlement.Faction == FactionId.LutheranSynod &&
            tile.Settlement.SynodPlayer != unit.SynodPlayer)
            return false;
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

    public bool ClusterHasBuilding(City city, CityBuildId id)
    {
        if (city == null)
            return false;

        var root = ClergyRoster.GetControllingRoot(city);
        foreach (var member in GetSynodPlayerCities(city.SynodPlayer))
        {
            if (ClergyRoster.GetControllingRoot(member) != root)
                continue;
            if (member.Production != null && member.Production.HasBuilding(id))
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
        foreach (var city in GetPlayerCities())
        {
            if (!city.IsHamlet) continue;
            int tribute = city.GetProductionPerTurn();
            totalMss += Mathf.Max(1, tribute / 2);
        }

        // Manuscripts only — district tribute fame was racing the fame victory ahead of research.
        if (totalMss > 0)
        {
            faction.ScriptureManuscripts += totalMss;
            Debug.Log($"Hamlet tribute: +{totalMss} manuscripts.");
        }
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
