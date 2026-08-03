using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Schismatic factions emerge when confessional crisis splits the synod.
/// Supports repeatable multi-heresy schisms (up to 3 concurrent blocs).
/// </summary>
public class SchismManager : MonoBehaviour
{
    public static SchismManager Instance { get; private set; }

    readonly List<SchismRecord> schismHistory = new();

    public IReadOnlyList<SchismRecord> SchismHistory => schismHistory;
    public int SchismCount => schismHistory.Count;
    public bool HasSchismed => SchismaticBlocRegistry.Instance != null && SchismaticBlocRegistry.Instance.HasAnySchism;
    public string LastSchismReason { get; private set; } = "";
    public HexCoordinates DissentCapitalHex { get; private set; }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryTriggerSchism(HeresyType heresy, string reason, bool controlledSplit = false)
    {
        var registry = SchismaticBlocRegistry.Instance;
        if (registry == null)
            return false;

        var blocId = registry.AllocateBlocId();
        if (blocId == null)
        {
            Debug.LogWarning("Schism blocked: maximum concurrent dissent blocs (3) already active.");
            return false;
        }

        var anchorHex = FirstSteps.Instance?.SynodAnchorHex;
        if (anchorHex == null)
            return false;

        if (HexGridMap.Instance == null ||
            !HexGridMap.Instance.TryPickSchismSite(
                anchorHex.Value,
                out var schismCapital,
                out var soldierHex,
                out var missionaryHex,
                CollectPlayerSchismAvoidHexes()))
        {
            Debug.LogWarning("Schism blocked: no valid dissent site on the map.");
            return false;
        }

        var profile = HeresyDatabase.ProfileFor(heresy);
        var record = new SchismRecord(
            blocId.Value,
            heresy,
            reason,
            schismCapital,
            TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1);

        registry.TryRegisterBloc(record);
        schismHistory.Add(record);
        LastSchismReason = reason;
        DissentCapitalHex = schismCapital;

        SplitPopulation(controlledSplit);
        RebalanceLawGospelAfterSchism();
        var schismCity = SpawnSchismaticCity(record, schismCapital);
        SplitSchismaticForcesFromPlayer(record, profile, soldierHex, missionaryHex, schismCity);

        TurnManager.Instance?.ActivateSchismaticBloc(blocId.Value);
        FogOfWarManager.Instance?.Refresh();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh($"Schism! {profile.DisplayName} has broken from the synod.");
        SchismEventPanel.Instance?.Show(record, reason);

        Debug.LogWarning($"SCHISM ({record.BlocId}): {profile.DisplayName}  -  {reason} at {schismCapital}.");
        return true;
    }

    public bool TryTriggerSchism(string reason) =>
        TryTriggerSchism(HeresyType.DoctrinalDrift, reason);

    /// <summary>Pre-placed rival from lobby player count — no population split or crisis panel.</summary>
    public bool TrySpawnLobbyRival(
        SchismaticBlocId blocId,
        HeresyType heresy,
        HexCoordinates synodAnchor,
        IReadOnlyList<HexCoordinates> avoidCapitals)
    {
        var registry = SchismaticBlocRegistry.Instance;
        if (registry == null || registry.TryGetBloc(blocId, out _))
            return false;

        if (HexGridMap.Instance == null ||
            !HexGridMap.Instance.TryPickRivalSpawnSite(
                synodAnchor,
                avoidCapitals,
                out var schismCapital,
                out var soldierHex,
                out var missionaryHex))
        {
            Debug.LogWarning($"Lobby rival {blocId}: no valid spawn site.");
            return false;
        }

        var profile = HeresyDatabase.ProfileFor(heresy);
        var record = new SchismRecord(
            blocId,
            heresy,
            "Pre-existing dissent (lobby rival)",
            schismCapital,
            TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1);

        registry.TryRegisterBloc(record);
        schismHistory.Add(record);

        var schismCity = SpawnSchismaticCity(record, schismCapital);
        SpawnSchismaticUnits(record, profile, soldierHex, missionaryHex, schismCity);
        TurnManager.Instance?.ActivateSchismaticBloc(blocId);
        FogOfWarManager.Instance?.Refresh();
        FirstSteps.Instance?.RefreshDashboard();

        Debug.Log($"Lobby rival {blocId}: {profile.DisplayName} at {schismCapital}.");
        return true;
    }

    /// <summary>AI rival synod splinters a nearby schismatic bloc; parent synod survives weakened.</summary>
    public bool TryTriggerAiSchism(
        City sourceCity,
        SynodPlayerId playerId,
        CrisisType crisis,
        string tensionLabel)
    {
        if (sourceCity == null ||
            sourceCity.Faction != FactionId.LutheranSynod ||
            sourceCity.SynodPlayer != playerId ||
            playerId is SynodPlayerId.None or SynodPlayerId.Player1)
            return false;

        var registry = SchismaticBlocRegistry.Instance;
        if (registry == null)
            return false;

        var blocId = registry.AllocateBlocId();
        if (blocId == null)
        {
            Debug.LogWarning($"AI schism blocked for {SynodPlayerDatabase.DisplayName(playerId)}: max blocs active.");
            return false;
        }

        if (HexGridMap.Instance == null ||
            !HexGridMap.Instance.TryPickSchismSite(
                sourceCity.HexPosition,
                out var schismCapital,
                out var soldierHex,
                out var missionaryHex,
                CollectPlayerSchismAvoidHexes()))
        {
            Debug.LogWarning($"AI schism blocked for {sourceCity.CityName}: no valid dissent site.");
            return false;
        }

        var heresy = registry.PickHeresyForCrisis(crisis, registry.HasAnySchism);
        var profile = HeresyDatabase.ProfileFor(heresy);
        string synodName = SynodPlayerDatabase.DisplayName(playerId);
        string reason =
            $"{synodName} fractured under {tensionLabel} — {profile.DisplayName} broke away near {sourceCity.CityName}.";

        if (registry.PickBlocForHeresy(heresy) is SchismaticBlocId existingBloc)
        {
            WeakenAiSynod(playerId, sourceCity);
            ReinforceExistingBloc(existingBloc, reason, nearPlayer: false);
            TurnPhaseBanner.Instance?.Refresh(
                $"Schism pressure! Unrest joined existing {profile.DisplayName}.");
            Debug.LogWarning($"AI SCHISM REINFORCE ({existingBloc}): {reason}");
            return true;
        }

        var record = new SchismRecord(
            blocId.Value,
            heresy,
            reason,
            schismCapital,
            TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1);

        registry.TryRegisterBloc(record);
        schismHistory.Add(record);
        LastSchismReason = reason;
        DissentCapitalHex = schismCapital;

        WeakenAiSynod(playerId, sourceCity);
        PeelUnitToSchismaticBloc(playerId, blocId.Value, soldierHex);

        var schismCity = SpawnSchismaticCity(record, schismCapital);
        SpawnSchismaticUnits(record, profile, soldierHex, missionaryHex, schismCity);

        TurnManager.Instance?.ActivateSchismaticBloc(blocId.Value);
        FogOfWarManager.Instance?.Refresh();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh($"Schism! {profile.DisplayName} splintered from {synodName}.");
        SchismEventPanel.Instance?.Show(record, reason);

        Debug.LogWarning($"AI SCHISM ({record.BlocId}): {reason} at {schismCapital}.");
        return true;
    }

    static void WeakenAiSynod(SynodPlayerId playerId, City capital)
    {
        if (CityManager.Instance == null)
            return;

        capital.Population = Mathf.Max(8, Mathf.RoundToInt(capital.Population * 0.65f));
        capital.AdjustLoyalty(-8f);
        capital.RefreshAppearance();

        foreach (var city in CityManager.Instance.GetSynodPlayerCities(playerId))
        {
            if (city == capital)
                continue;

            city.Population = Mathf.Max(5, city.Population - Random.Range(2, 5));
            city.RefreshAppearance();
        }
    }

    static void PeelUnitToSchismaticBloc(SynodPlayerId playerId, SchismaticBlocId blocId, HexCoordinates rallyHex)
    {
        if (TryPeelSynodUnit(playerId, blocId, rallyHex, u =>
                u.Type is UnitType.Soldier or UnitType.Slinger or UnitType.Archer or UnitType.Horseman) != null)
            return;

        if (TryPeelSynodUnit(playerId, blocId, rallyHex, GarrisonBonus.IsMartialUnit) != null)
            return;

        TryPeelSynodUnit(playerId, blocId, rallyHex, _ => true);
    }

    void SplitPopulation(bool controlledSplit)
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        int divisor = controlledSplit ? 4 : 3;
        int splinterPop = Mathf.Max(6, PopulationSync.SumSynodPopulation() / divisor);
        PopulationSync.ApplyLossAcrossPlayerCities(splinterPop);
        float adherenceLoss = controlledSplit ? 5f : 8f;
        faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - adherenceLoss, 0f, 100f);
    }

    /// <summary>
    /// After schism, pull Law/Gospel meters out of the crisis skew so the same tension
    /// does not immediately re-fire on the next turn.
    /// </summary>
    static void RebalanceLawGospelAfterSchism()
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        faction.civicRestraint = Mathf.Clamp(
            Mathf.Lerp(faction.civicRestraint, 52f, 0.55f), 40f, 62f);
        faction.spiritualComfort = Mathf.Clamp(
            Mathf.Lerp(faction.spiritualComfort, 52f, 0.55f), 40f, 58f);
        Debug.Log(
            $"Schism Law/Gospel rebalance: restraint {faction.civicRestraint:F0}%, " +
            $"comfort {faction.spiritualComfort:F0}%.");
    }

    City SpawnSchismaticCity(SchismRecord record, HexCoordinates hex)
    {
        var profile = record.Profile;
        var go = new GameObject($"City_{profile.CapitalSuffix}");
        go.transform.SetParent(transform);
        var city = go.AddComponent<City>();
        city.Initialize(FactionId.Schismatic, hex, profile.CapitalSuffix, isCapital: true);
        city.SetSchismaticBloc(record.BlocId);
        return city;
    }

    void SplitSchismaticForcesFromPlayer(
        SchismRecord record,
        HeresyProfile profile,
        HexCoordinates martialHex,
        HexCoordinates clergyHex,
        City schismCity)
    {
        var onMap = GetSynodUnitsOnMap(SynodPlayerId.Player1);
        bool playerHasMartial = onMap.Any(GarrisonBonus.IsMartialUnit);
        bool playerHasClergy = onMap.Any(u => ClergyRoster.IsClergyUnit(u.Type));

        Unit martial = playerHasMartial
            ? TryPeelSynodUnit(SynodPlayerId.Player1, record.BlocId, martialHex, GarrisonBonus.IsMartialUnit)
            : null;
        Unit clergy = playerHasClergy
            ? TryPeelSynodUnit(
                SynodPlayerId.Player1,
                record.BlocId,
                clergyHex,
                u => ClergyRoster.IsClergyUnit(u.Type))
            : null;

        if (clergy != null)
            ClergyRoster.RegisterUnit(clergy, schismCity);

        if (martial == null && playerHasMartial)
            SpawnSchismaticUnit(record.BlocId, PickMirrorMartialType(onMap, profile), martialHex);

        if (clergy == null && playerHasClergy)
            SpawnSchismaticUnit(record.BlocId, PickMirrorClergyType(onMap, profile), clergyHex, schismCity);

        if (martial == null && clergy == null && !playerHasMartial && !playerHasClergy)
        {
            var startType = profile.PreferSoldiers && !profile.PreferMissionaries
                ? UnitType.Soldier
                : profile.PreferMissionaries
                    ? UnitType.Missionary
                    : UnitType.Chaplain;
            SpawnSchismaticUnit(record.BlocId, startType, clergyHex, schismCity);
            Debug.Log($"Schism bloc {record.BlocId} seeded a lone {startType} — your synod had no units to split.");
            return;
        }

        var parts = new List<string>();
        if (martial != null || (playerHasMartial && martial == null))
            parts.Add(martial != null ? $"peeled {martial.Type}" : "spawned martial");
        if (clergy != null || (playerHasClergy && clergy == null))
            parts.Add(clergy != null ? $"peeled {clergy.Type}" : "spawned clergy");
        Debug.Log($"Schism bloc {record.BlocId} mirrored your synod: {string.Join(", ", parts)}.");
    }

    static List<Unit> GetSynodUnitsOnMap(SynodPlayerId playerId)
    {
        if (TurnManager.Instance == null)
            return new List<Unit>();

        return TurnManager.Instance.GetSynodUnits(playerId)
            .Where(u => u.IsAlive && u.IsOnMap)
            .ToList();
    }

    static UnitType PickMirrorMartialType(IEnumerable<Unit> playerUnits, HeresyProfile profile)
    {
        var martial = playerUnits.Where(GarrisonBonus.IsMartialUnit).ToList();
        if (martial.Count == 0)
            return profile.PreferRanged && Random.value < 0.55f ? UnitType.Slinger : UnitType.Soldier;

        return martial
            .GroupBy(u => u.Type)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }

    static UnitType PickMirrorClergyType(IEnumerable<Unit> playerUnits, HeresyProfile profile)
    {
        var clergy = playerUnits.Where(u => ClergyRoster.IsClergyUnit(u.Type)).ToList();
        if (clergy.Count == 0)
            return PickSchismaticClergy(profile);

        return clergy
            .GroupBy(u => u.Type)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }

    static Unit TryPeelSynodUnit(
        SynodPlayerId playerId,
        SchismaticBlocId blocId,
        HexCoordinates rallyHex,
        System.Func<Unit, bool> predicate)
    {
        if (TurnManager.Instance == null || predicate == null)
            return null;

        // Peel the unit furthest from the capital — fringe peels away; home garrison stays.
        HexCoordinates? capitalHex = null;
        var capital = CityManager.Instance?.GetPrimaryPlayerCity();
        if (capital != null)
            capitalHex = capital.HexPosition;

        Unit best = null;
        int bestDistance = -1;
        foreach (var unit in TurnManager.Instance.GetSynodUnits(playerId))
        {
            if (!unit.IsAlive || !unit.IsOnMap || !predicate(unit))
                continue;

            int dist = capitalHex.HasValue
                ? unit.HexPosition.DistanceTo(capitalHex.Value)
                : 0;
            if (best == null || dist > bestDistance)
            {
                best = unit;
                bestDistance = dist;
            }
        }

        if (best == null)
            return null;

        best.ConvertToSchismaticBloc(blocId);
        if (best.HexPosition != rallyHex && HexGridMap.Instance != null)
            best.TryMoveTo(rallyHex);
        return best;
    }

    void SpawnSchismaticUnits(
        SchismRecord record,
        HeresyProfile profile,
        HexCoordinates soldierHex,
        HexCoordinates missionaryHex,
        City schismCity,
        bool spawnMartial = true)
    {
        if (!spawnMartial)
        {
            var startType = profile.PreferMissionaries ? UnitType.Missionary : UnitType.Chaplain;
            SpawnSchismaticUnit(record.BlocId, startType, soldierHex, schismCity);
            Debug.Log($"Schism bloc {record.BlocId} began with a {startType} only — martial units must be trained.");
            return;
        }

        var martialType = profile.PreferRanged && Random.value < 0.55f
            ? UnitType.Slinger
            : UnitType.Soldier;

        var clergyType = PickSchismaticClergy(profile);

        if (profile.PreferSoldiers && !profile.PreferMissionaries)
        {
            SpawnSchismaticUnit(record.BlocId, martialType, soldierHex);
            SpawnSchismaticUnit(record.BlocId, clergyType, missionaryHex, schismCity);
            return;
        }

        if (profile.PreferMissionaries)
        {
            SpawnSchismaticUnit(record.BlocId, UnitType.Missionary, soldierHex);
            SpawnSchismaticUnit(record.BlocId, clergyType, missionaryHex, schismCity);
            return;
        }

        SpawnSchismaticUnit(record.BlocId, martialType, soldierHex);
        SpawnSchismaticUnit(record.BlocId, clergyType, missionaryHex, schismCity);
    }

    static UnitType PickSchismaticClergy(HeresyProfile profile)
    {
        if (profile.SpiritualComfort >= 85f && !profile.PreferSoldiers)
            return UnitType.Bishop;
        if (profile.SpiritualComfort >= 75f && !profile.PreferSoldiers)
            return UnitType.Cantor;
        if (profile.PreferMissionaries || profile.SpiritualComfort >= 55f)
            return UnitType.Chaplain;
        return UnitType.Missionary;
    }

    void SpawnSchismaticUnit(SchismaticBlocId blocId, UnitType type, HexCoordinates hex, City rosterCity = null)
    {
        var go = new GameObject($"Schismatic_{blocId}_{type}");
        go.transform.SetParent(transform);
        var unit = go.AddComponent<Unit>();
        unit.Initialize(FactionId.Schismatic, type, hex);
        unit.SetSchismaticBloc(blocId);
        TurnManager.Instance?.RegisterUnit(unit);

        if (ClergyRoster.IsClergyUnit(type) && rosterCity != null)
            ClergyRoster.RegisterUnit(unit, rosterCity);
    }

    static List<HexCoordinates> CollectPlayerSchismAvoidHexes()
    {
        var avoid = new List<HexCoordinates>();
        if (CityManager.Instance == null)
            return avoid;

        foreach (var city in CityManager.Instance.GetSynodPlayerCities(SynodPlayerId.Player1))
        {
            if (city != null)
                avoid.Add(city.HexPosition);
        }

        var registry = SchismaticBlocRegistry.Instance;
        if (registry != null)
        {
            foreach (var bloc in registry.ActiveBlocs.Values)
                avoid.Add(bloc.CapitalHex);
        }

        return avoid;
    }

    public bool TryReinforceMatchingBloc(HeresyType heresy, string reason, bool nearPlayer = true)
    {
        var blocId = SchismaticBlocRegistry.Instance?.PickBlocForHeresy(heresy);
        if (blocId == null)
            return false;

        ReinforceExistingBloc(blocId.Value, reason, nearPlayer);
        return true;
    }

    /// <summary>Dissent overflow when three blocs already exist — strengthens an existing heresy.</summary>
    public void ReinforceExistingBloc(SchismaticBlocId blocId, string reason, bool nearPlayer = false)
    {
        var registry = SchismaticBlocRegistry.Instance;
        if (registry == null || !registry.TryGetBloc(blocId, out var record))
            return;

        var city = CityManager.Instance?.GetAiCity(blocId);
        if (city != null)
        {
            city.Population += Random.Range(3, 6);
            city.RefreshAppearance();
        }

        var profile = record.Profile;
        var rallyHex = record.CapitalHex;

        if (nearPlayer)
        {
            var playerCity = CityManager.Instance?.GetPrimaryPlayerCity();
            if (playerCity != null && HexGridMap.Instance != null)
            {
                foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(playerCity.HexPosition))
                {
                    if (!HexGridMap.Instance.TryGetTile(neighbor, out var nTile))
                        continue;
                    if (!TerrainRules.IsPassable(nTile.Terrain) || nTile.Occupant != null)
                        continue;
                    if (nTile.Settlement != null)
                        continue;
                    rallyHex = neighbor;
                    break;
                }
            }
        }
        else if (HexGridMap.Instance != null)
        {
            foreach (var neighbor in record.CapitalHex.GetNeighbors())
            {
                if (!HexGridMap.Instance.TryGetTile(neighbor, out var nTile))
                    continue;
                if (!TerrainRules.IsPassable(nTile.Terrain) || nTile.Occupant != null)
                    continue;

                rallyHex = neighbor;
                break;
            }
        }

        var unitType = profile.PreferSoldiers && !profile.PreferMissionaries
            ? UnitType.Soldier
            : PickSchismaticClergy(profile);

        // Spawn a martial raider when pressing the player; otherwise mirror profile.
        if (nearPlayer)
            unitType = profile.PreferRanged ? UnitType.Slinger : UnitType.Soldier;

        SpawnSchismaticUnit(blocId, unitType, rallyHex, city);

        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 4f, 0f, 100f);
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint - 3f, 0f, 100f);
        }

        TurnPhaseBanner.Instance?.Refresh(
            nearPlayer
                ? $"Dissent from {profile.CapitalSuffix} presses near your cities — no fourth capital, but their party grows."
                : $"Dissent joined {profile.CapitalSuffix} — no fourth capital, but their party grows.");
        Debug.LogWarning($"Dissent overflow: reinforced {profile.DisplayName} ({blocId}). {reason}");
        FirstSteps.Instance?.RefreshDashboard();
    }

    public void ReinforceWeakestBloc(string reason)
    {
        var blocId = SchismaticBlocRegistry.Instance?.PickWeakestBloc();
        if (blocId == null)
            return;

        ReinforceExistingBloc(blocId.Value, reason, nearPlayer: false);
    }
}
