using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cycles the player through units that still need orders; End Turn advances the queue.
/// </summary>
public class PlayerUnitCycle : MonoBehaviour
{
    public static PlayerUnitCycle Instance { get; private set; }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted += OnTurnStarted;
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted -= OnTurnStarted;
    }

    void Update()
    {
        if (Keyboard.current == null || TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return;
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return;
        if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen)
            return;
        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            CycleNextUnit(wrapOnly: true);
    }

    void OnTurnStarted()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return;

        var actionable = GetUnitsNeedingOrders();
        if (actionable.Count > 0)
            FocusUnit(actionable[0]);
        else
            TurnManager.Instance.SelectUnit(null);

        RefreshTurnBannerHint();
    }

    public void TryEndTurnOrCycleNext()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return;

        var actionable = GetUnitsNeedingOrders();
        if (actionable.Count == 0)
        {
            if (EndTurnPhaseController.Instance != null &&
                EndTurnPhaseController.Instance.TryBeginPhasedEndTurn())
                return;
            TurnManager.Instance.EndTurn();
            return;
        }

        var selected = TurnManager.Instance.SelectedUnit;
        if (selected == null || !actionable.Contains(selected))
        {
            FocusUnit(actionable[0]);
            return;
        }

        int index = actionable.IndexOf(selected);
        if (index >= actionable.Count - 1)
        {
            if (EndTurnPhaseController.Instance != null &&
                EndTurnPhaseController.Instance.TryBeginPhasedEndTurn())
                return;
            TurnManager.Instance.EndTurn();
            return;
        }

        FocusUnit(actionable[index + 1]);
    }

    public void CycleNextUnit(bool wrapOnly = false)
    {
        var actionable = GetUnitsNeedingOrders();
        if (actionable.Count == 0)
            return;

        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected == null || !actionable.Contains(selected))
        {
            FocusUnit(actionable[0]);
            return;
        }

        int index = actionable.IndexOf(selected);
        int nextIndex = wrapOnly ? (index + 1) % actionable.Count : Mathf.Min(index + 1, actionable.Count - 1);
        FocusUnit(actionable[nextIndex]);
    }

    public void FocusUnit(Unit unit)
    {
        if (unit == null || TurnManager.Instance == null)
            return;

        TurnManager.Instance.SelectUnit(unit);
        HexSelectionController.Instance?.FocusUnit(unit);
        CameraFollow.Instance?.FollowUnit(unit);
        FirstSteps.Instance?.BindPlayerUnit(unit);
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        RefreshTurnBannerHint();
    }

    public void OnUnitOrdersChanged()
    {
        RefreshTurnBannerHint();
    }

    List<Unit> GetUnitsNeedingOrders()
    {
        if (TurnManager.Instance == null)
            return new List<Unit>();

        return TurnManager.Instance.GetUnits(FactionId.LutheranSynod)
            .Where(u => u != null && u.IsAlive && u.NeedsOrders)
            .OrderBy(u => u.Type)
            .ThenBy(u => (long)EntityId.ToULong(u.GetEntityId()))
            .ToList();
    }

    void RefreshTurnBannerHint()
    {
        var actionable = GetUnitsNeedingOrders();
        if (actionable.Count == 0)
        {
            TurnPhaseBanner.Instance?.Refresh("End Turn to finish");
            return;
        }

        var selected = TurnManager.Instance?.SelectedUnit;
        int index = selected != null ? actionable.IndexOf(selected) + 1 : 0;
        if (index <= 0) index = 1;
        TurnPhaseBanner.Instance?.Refresh($"Unit {index}/{actionable.Count}  |  Tab next  |  End Turn cycles");
    }
}
