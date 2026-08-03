using System.Collections.Generic;
using UnityEngine;

public readonly struct SchismRecord
{
    public readonly SchismaticBlocId BlocId;
    public readonly HeresyType Heresy;
    public readonly string Reason;
    public readonly HexCoordinates CapitalHex;
    public readonly int TurnNumber;

    public SchismRecord(
        SchismaticBlocId blocId,
        HeresyType heresy,
        string reason,
        HexCoordinates capitalHex,
        int turnNumber)
    {
        BlocId = blocId;
        Heresy = heresy;
        Reason = reason;
        CapitalHex = capitalHex;
        TurnNumber = turnNumber;
    }

    public HeresyProfile Profile => HeresyDatabase.ProfileFor(Heresy);
    public string CapitalName => Profile.CapitalSuffix;
}

/// <summary>Tracks active schismatic blocs, heresy profiles, and per-bloc growth metrics.</summary>
public class SchismaticBlocRegistry : MonoBehaviour
{
    public const int MaxBlocs = 3;

    public static SchismaticBlocRegistry Instance { get; private set; }

    readonly Dictionary<SchismaticBlocId, SchismRecord> activeBlocs = new();

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public IReadOnlyDictionary<SchismaticBlocId, SchismRecord> ActiveBlocs => activeBlocs;
    public int ActiveCount => activeBlocs.Count;
    public bool HasAnySchism => activeBlocs.Count > 0;

    public bool TryRegisterBloc(SchismRecord record)
    {
        if (activeBlocs.ContainsKey(record.BlocId))
            return false;

        activeBlocs[record.BlocId] = record;
        return true;
    }

    public bool TryGetBloc(SchismaticBlocId blocId, out SchismRecord record) =>
        activeBlocs.TryGetValue(blocId, out record);

    public HeresyProfile ProfileForBloc(SchismaticBlocId blocId)
    {
        if (activeBlocs.TryGetValue(blocId, out var record))
            return record.Profile;
        return HeresyDatabase.ProfileFor(HeresyType.DoctrinalDrift);
    }

    public SchismaticBlocId? AllocateBlocId()
    {
        foreach (SchismaticBlocId id in new[] { SchismaticBlocId.Bloc1, SchismaticBlocId.Bloc2, SchismaticBlocId.Bloc3 })
        {
            if (!activeBlocs.ContainsKey(id))
                return id;
        }

        return null;
    }

    public bool TryDissolveBloc(SchismaticBlocId blocId)
    {
        if (!activeBlocs.TryGetValue(blocId, out var record))
            return false;

        if (TurnManager.Instance != null)
        {
            foreach (var unit in TurnManager.Instance.GetUnits(FactionId.Schismatic))
            {
                if (unit == null || !unit.IsAlive || unit.SchismaticBloc != blocId)
                    continue;
                unit.TakeDamage(unit.MaxHealth);
            }
        }

        var city = CityManager.Instance?.GetAiCity(blocId);
        if (city != null && city.Faction == FactionId.Schismatic)
            city.Capture(FactionId.LutheranSynod, SynodPlayerId.Player1);

        activeBlocs.Remove(blocId);
        Debug.LogWarning($"Schismatic bloc {blocId} ({record.CapitalName}) dissolved.");
        return true;
    }

    public void UnregisterBloc(SchismaticBlocId blocId)
    {
        if (blocId == SchismaticBlocId.None)
            return;
        activeBlocs.Remove(blocId);
    }

    public HeresyType PickHeresyForCrisis(CrisisType crisis, bool isRepeat)
    {
        var activeHeresies = new HashSet<HeresyType>();
        foreach (var record in activeBlocs.Values)
            activeHeresies.Add(record.Heresy);

        var pack = MatchLobbyController.Instance?.Current?.HeresyPack ?? HeresyPackId.FullCanon;
        return HeresyDatabase.PickHeresyForCrisis(crisis, isRepeat, activeHeresies, pack);
    }

    public SchismaticBlocId? PickBlocForHeresy(HeresyType heresy)
    {
        foreach (var record in activeBlocs.Values)
        {
            if (record.Heresy == heresy)
                return record.BlocId;
        }

        return null;
    }

    public SchismaticBlocId? PickWeakestBloc()
    {
        SchismaticBlocId? weakest = null;
        int lowestPop = int.MaxValue;
        if (CityManager.Instance == null)
            return PickBlocForHeresy(HeresyType.None);

        foreach (var record in activeBlocs.Values)
        {
            var city = CityManager.Instance.GetAiCity(record.BlocId);
            int pop = city != null ? city.Population : 0;
            if (pop < lowestPop)
            {
                lowestPop = pop;
                weakest = record.BlocId;
            }
        }

        return weakest;
    }

    public string FormatStatusLine()
    {
        if (activeBlocs.Count == 0)
            return "";

        if (activeBlocs.Count == 1)
        {
            foreach (var record in activeBlocs.Values)
                return $"<color=#EE7766><b>Schism: {record.CapitalName}</b></color>";
        }

        var names = new List<string>();
        foreach (var record in activeBlocs.Values)
            names.Add(record.CapitalName);
        return $"<color=#EE7766><b>Schisms ({activeBlocs.Count}):</b> {string.Join(", ", names)}</color>";
    }

    public CityGrowthSystem.FactionGrowthMetrics GetGrowthMetrics(SchismaticBlocId blocId)
    {
        var profile = ProfileForBloc(blocId);
        return new CityGrowthSystem.FactionGrowthMetrics
        {
            Adherence = profile.Adherence,
            SpiritualComfort = profile.SpiritualComfort,
            CivicRestraint = profile.CivicRestraint,
            UseWaltherTension = true
        };
    }
}
