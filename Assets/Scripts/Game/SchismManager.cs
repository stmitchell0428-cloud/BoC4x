using System.Collections.Generic;
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

    /// <summary>Stub for future AI synod factions  -  schismatic blocs can split when AI meters fail.</summary>
    public bool TryTriggerAiSchism(City sourceCity, HeresyType heresy, string reason)
    {
        if (sourceCity == null || sourceCity.Faction != FactionId.LutheranSynod)
            return false;

        Debug.LogWarning($"AI schism queued at {sourceCity.CityName}: {HeresyDatabase.ProfileFor(heresy).DisplayName}  -  {reason}");
        return false;
    }

    void SplitPopulation(bool controlledSplit)
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        int divisor = controlledSplit ? 4 : 3;
        int splinterPop = Mathf.Max(6, faction.population / divisor);
        faction.population = Mathf.Max(0, faction.population - splinterPop);
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
