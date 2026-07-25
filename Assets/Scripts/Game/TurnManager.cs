using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public readonly struct TurnSlot
{
    public readonly FactionId Faction;
    public readonly SchismaticBlocId BlocId;

    public TurnSlot(FactionId faction, SchismaticBlocId blocId = SchismaticBlocId.None)
    {
        Faction = faction;
        BlocId = blocId;
    }
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    readonly List<TurnSlot> turnOrder = new() { new TurnSlot(FactionId.LutheranSynod) };
    readonly Dictionary<FactionId, List<Unit>> factionUnits = new();

    int turnNumber = 1;
    int activeSlotIndex;

    public FactionId ActiveFaction => turnOrder[activeSlotIndex].Faction;
    public SchismaticBlocId ActiveSchismaticBloc => turnOrder[activeSlotIndex].BlocId;
    public int TurnNumber => turnNumber;
    public Unit SelectedUnit { get; private set; }

    public event System.Action TurnStarted;
    public event System.Action TurnEnded;

    void Awake()
    {
        Instance = this;
        factionUnits[FactionId.LutheranSynod] = new List<Unit>();
    }

    public bool SchismActive => turnOrder.Any(s => s.Faction == FactionId.Schismatic);

    public void ActivateSchismFaction() =>
        ActivateSchismaticBloc(SchismaticBlocId.Bloc1);

    public void ActivateSchismaticBloc(SchismaticBlocId blocId)
    {
        if (blocId == SchismaticBlocId.None)
            return;

        foreach (var slot in turnOrder)
        {
            if (slot.Faction == FactionId.Schismatic && slot.BlocId == blocId)
                return;
        }

        turnOrder.Add(new TurnSlot(FactionId.Schismatic, blocId));
        if (!factionUnits.ContainsKey(FactionId.Schismatic))
            factionUnits[FactionId.Schismatic] = new List<Unit>();
    }

    public void RegisterUnit(Unit unit)
    {
        if (!factionUnits.ContainsKey(unit.Faction))
            factionUnits[unit.Faction] = new List<Unit>();
        factionUnits[unit.Faction].Add(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        if (factionUnits.TryGetValue(unit.Faction, out var list))
            list.Remove(unit);
        if (SelectedUnit == unit)
        {
            SelectedUnit = null;
            TerrainInfoPanel.Instance?.RefreshSelection();
        }
    }

    public IReadOnlyList<Unit> GetUnits(FactionId faction) =>
        factionUnits.TryGetValue(faction, out var list) ? list : System.Array.Empty<Unit>();

    public IReadOnlyList<Unit> GetBlocUnits(SchismaticBlocId blocId) =>
        GetUnits(FactionId.Schismatic).Where(u => u.IsAlive && u.SchismaticBloc == blocId).ToList();

    public void BeginGame()
    {
        turnNumber = 1;
        activeSlotIndex = 0;
        StartFactionTurn();
    }

    public void EndTurn()
    {
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return;

        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
            return;

        if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
            return;

        TurnEnded?.Invoke();
        SelectedUnit = null;
        HexSelectionController.Instance?.ClearHighlights();
        TerrainInfoPanel.Instance?.RefreshSelection();

        activeSlotIndex = (activeSlotIndex + 1) % turnOrder.Count;
        if (activeSlotIndex == 0)
            turnNumber++;

        StartFactionTurn();
    }

    void StartFactionTurn()
    {
        var slot = turnOrder[activeSlotIndex];
        foreach (var unit in GetUnits(slot.Faction).Where(u => u.IsAlive))
        {
            if (slot.Faction == FactionId.Schismatic && unit.SchismaticBloc != slot.BlocId)
                continue;
            unit.RefreshTurn();
        }

        TurnStarted?.Invoke();

        if (slot.Faction == FactionId.LutheranSynod)
            FogOfWarManager.Instance?.Refresh();

        if (slot.Faction == FactionId.Schismatic)
            SimpleAI.Instance?.PlayTurn(slot.BlocId);
    }

    public void SelectUnit(Unit unit)
    {
        if (unit != null && (unit.Faction != ActiveFaction || !unit.IsAlive))
            return;

        if (ActiveFaction == FactionId.Schismatic && unit.SchismaticBloc != ActiveSchismaticBloc)
            return;

        SelectedUnit = unit;
        TerrainInfoPanel.Instance?.RefreshSelection();
    }

    public bool IsPlayerTurn => ActiveFaction == FactionId.LutheranSynod;
}
