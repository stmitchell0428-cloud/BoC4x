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
                out var missionaryHex))
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
        var schismCity = SpawnSchismaticCity(record, schismCapital);
        SpawnSchismaticUnits(record, profile, soldierHex, missionaryHex, schismCity);

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
                out var missionaryHex))
        {
            Debug.LogWarning($"AI schism blocked for {sourceCity.CityName}: no valid dissent site.");
            return false;
        }

        var heresy = registry.PickHeresyForCrisis(crisis, registry.HasAnySchism);
        var profile = HeresyDatabase.ProfileFor(heresy);
        string synodName = SynodPlayerDatabase.DisplayName(playerId);
        string reason =
            $"{synodName} fractured under {tensionLabel} — {profile.DisplayName} broke away near {sourceCity.CityName}.";

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
        if (TurnManager.Instance == null)
            return;

        Unit peel = null;
        foreach (var unit in TurnManager.Instance.GetSynodUnits(playerId))
        {
            if (!unit.IsAlive || !unit.IsOnMap)
                continue;

            if (unit.Type is UnitType.Soldier or UnitType.Slinger or UnitType.Archer or UnitType.Horseman)
            {
                peel = unit;
                break;
            }
        }

        peel ??= TurnManager.Instance.GetSynodUnits(playerId).FirstOrDefault(u => u.IsAlive && u.IsOnMap);

        if (peel == null)
            return;

        peel.ConvertToSchismaticBloc(blocId);
        if (peel.HexPosition != rallyHex && HexGridMap.Instance != null)
            peel.TryMoveTo(rallyHex);
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

    void SpawnSchismaticUnits(SchismRecord record, HeresyProfile profile, HexCoordinates soldierHex, HexCoordinates missionaryHex, City schismCity)
    {
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
}
