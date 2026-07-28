using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public readonly struct TurnSlot
{
    public readonly FactionId Faction;
    public readonly SchismaticBlocId BlocId;
    public readonly SynodPlayerId SynodPlayer;

    public TurnSlot(
        FactionId faction,
        SchismaticBlocId blocId = SchismaticBlocId.None,
        SynodPlayerId synodPlayer = SynodPlayerId.Player1)
    {
        Faction = faction;
        BlocId = blocId;
        SynodPlayer = synodPlayer;
    }
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    readonly List<TurnSlot> turnOrder = new() { new TurnSlot(FactionId.LutheranSynod, synodPlayer: SynodPlayerId.Player1) };
    readonly Dictionary<FactionId, List<Unit>> factionUnits = new();

    int turnNumber = 1;
    int activeSlotIndex;

    public FactionId ActiveFaction => turnOrder[activeSlotIndex].Faction;
    public SchismaticBlocId ActiveSchismaticBloc => turnOrder[activeSlotIndex].BlocId;
    public SynodPlayerId ActiveSynodPlayer => turnOrder[activeSlotIndex].SynodPlayer;
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

    public void ActivateSynodPlayer(SynodPlayerId playerId)
    {
        if (playerId is SynodPlayerId.None or SynodPlayerId.Player1)
            return;

        foreach (var slot in turnOrder)
        {
            if (slot.Faction == FactionId.LutheranSynod && slot.SynodPlayer == playerId)
                return;
        }

        turnOrder.Add(new TurnSlot(FactionId.LutheranSynod, synodPlayer: playerId));
    }

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

    public IReadOnlyList<Unit> GetSynodUnits(SynodPlayerId playerId) =>
        GetUnits(FactionId.LutheranSynod)
            .Where(u => u.IsAlive &&
                        u.Faction == FactionId.LutheranSynod &&
                        u.SynodPlayer == playerId)
            .ToList();

    public IReadOnlyList<Unit> GetBlocUnits(SchismaticBlocId blocId) =>
        GetUnits(FactionId.Schismatic)
            .Where(u => u.IsAlive &&
                        u.Faction == FactionId.Schismatic &&
                        u.SchismaticBloc == blocId)
            .ToList();

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

        EndTurnPhaseController.SanitizeStaleChoicePanelsPublic();
        if (EndTurnPhaseController.TryGetEndTurnBlockReason(out string blockReason))
        {
            Debug.LogWarning($"TurnManager.EndTurn blocked: {blockReason}");
            return;
        }

        if (IsPlayerTurn)
            AdvancePendingPlayerMoveOrders();

        TurnEnded?.Invoke();
        SelectedUnit = null;
        HexSelectionController.Instance?.ClearHighlights();
        TerrainInfoPanel.Instance?.RefreshSelection();

        int previousTurn = turnNumber;
        var previousSlot = turnOrder[activeSlotIndex];
        activeSlotIndex = (activeSlotIndex + 1) % turnOrder.Count;
        if (activeSlotIndex == 0)
            turnNumber++;

        if (turnNumber != previousTurn)
            Debug.Log(
                $"Turn advanced {previousTurn} → {turnNumber} " +
                $"(after {DescribeSlot(previousSlot)}; order size {turnOrder.Count}).");

        StartFactionTurn();
    }

    void StartFactionTurn()
    {
        var slot = turnOrder[activeSlotIndex];
        foreach (var unit in GetUnits(slot.Faction).Where(u => u.IsAlive))
        {
            if (unit.Faction != slot.Faction)
                continue;
            if (slot.Faction == FactionId.Schismatic && unit.SchismaticBloc != slot.BlocId)
                continue;
            if (slot.Faction == FactionId.LutheranSynod && unit.SynodPlayer != slot.SynodPlayer)
                continue;
            unit.RefreshTurn();
        }

        TurnStarted?.Invoke();

        if (slot.Faction == FactionId.LutheranSynod)
        {
            if (slot.SynodPlayer == SynodPlayerId.Player1)
                FogOfWarManager.Instance?.Refresh();
            else
                SimpleAI.Instance?.PlaySynodTurn(slot.SynodPlayer);
        }
        else if (slot.Faction == FactionId.Schismatic)
            SimpleAI.Instance?.PlayTurn(slot.BlocId);
    }

    static string DescribeSlot(TurnSlot slot)
    {
        if (slot.Faction == FactionId.LutheranSynod)
            return $"synod {slot.SynodPlayer}";
        return $"schism {slot.BlocId}";
    }

    void AdvancePendingPlayerMoveOrders()
    {
        foreach (var unit in GetSynodUnits(SynodPlayerId.Player1))
        {
            if (!unit.IsAlive || !unit.IsOnMap || !unit.HasMoveOrder)
                continue;

            while (unit.HasMoveOrder && unit.MovementRemaining > 0)
            {
                if (!unit.CommitPendingMoveOrder() && !unit.AdvanceMoveOrder())
                    break;
            }
        }

        FogOfWarManager.Instance?.Refresh();
    }

    public void SelectUnit(Unit unit)
    {
        if (unit == null)
        {
            SelectedUnit = null;
            TerrainInfoPanel.Instance?.RefreshSelection();
            return;
        }

        if (unit.Faction != ActiveFaction || !unit.IsAlive)
            return;

        if (ActiveFaction == FactionId.Schismatic && unit.SchismaticBloc != ActiveSchismaticBloc)
            return;

        if (ActiveFaction == FactionId.LutheranSynod && unit.SynodPlayer != ActiveSynodPlayer)
            return;

        SelectedUnit = unit;
        TerrainInfoPanel.Instance?.RefreshSelection();
    }

    public bool IsPlayerTurn =>
        ActiveFaction == FactionId.LutheranSynod && ActiveSynodPlayer == SynodPlayerId.Player1;
}
