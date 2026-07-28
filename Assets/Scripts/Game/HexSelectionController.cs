using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HexSelectionController : MonoBehaviour
{
    public static HexSelectionController Instance { get; private set; }

    Camera mainCamera;
    readonly HashSet<HexCoordinates> highlightedTiles = new();

    void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    void Update()
    {
        UpdateTerrainHover();

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn) return;
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (mainCamera == null) return;
        if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen) return;
        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var world = ScreenToWorld2D(Mouse.current.position.ReadValue());

        var hit = Physics2D.Raycast(world, Vector2.zero);
        if (hit.collider != null)
        {
            var city = hit.collider.GetComponent<City>();
            if (city != null)
            {
                HandleCityClick(city);
                return;
            }

            var unit = hit.collider.GetComponent<Unit>();
            if (unit != null)
            {
                HandleUnitClick(unit);
                return;
            }

            var tile = hit.collider.GetComponent<HexTile>();
            if (tile != null)
            {
                HandleHexClick(tile.Coordinates);
                return;
            }
        }

        HandleHexClick(HexGridMap.Instance.WorldToHex(world));
    }

    void UpdateTerrainHover()
    {
        if (Mouse.current == null || mainCamera == null)
        {
            TerrainInfoPanel.Instance?.SetHoveredHex(null);
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            TerrainInfoPanel.Instance?.SetHoveredHex(null);
            return;
        }

        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen)
        {
            TerrainInfoPanel.Instance?.SetHoveredHex(null);
            return;
        }

        if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen)
        {
            TerrainInfoPanel.Instance?.SetHoveredHex(null);
            return;
        }

        var world = ScreenToWorld2D(Mouse.current.position.ReadValue());
        var hit = Physics2D.Raycast(world, Vector2.zero);
        if (hit.collider != null)
        {
            var tile = hit.collider.GetComponent<HexTile>();
            if (tile != null)
            {
                TerrainInfoPanel.Instance?.SetHoveredHex(tile.Coordinates);
                return;
            }

            var unit = hit.collider.GetComponent<Unit>();
            if (unit != null)
            {
                TerrainInfoPanel.Instance?.SetHoveredHex(unit.HexPosition);
                return;
            }
        }

        var hex = HexGridMap.Instance != null
            ? HexGridMap.Instance.WorldToHex(world)
            : default;
        if (HexGridMap.Instance != null && HexGridMap.Instance.TryGetTile(hex, out _))
            TerrainInfoPanel.Instance?.SetHoveredHex(hex);
        else
            TerrainInfoPanel.Instance?.SetHoveredHex(null);
    }

    static Vector3 ScreenToWorld2D(Vector2 screenPos)
    {
        var cam = Camera.main;
        var pos = new Vector3(screenPos.x, screenPos.y, 0f);
        pos.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(pos);
    }

    void HandleCityClick(City city)
    {
        if (city == null || TurnManager.Instance == null) return;
        if (city.Faction != TurnManager.Instance.ActiveFaction) return;
        if (city.Faction == FactionId.LutheranSynod &&
            city.SynodPlayer != TurnManager.Instance.ActiveSynodPlayer)
            return;
        CityScreenPanel.Instance?.Open(city);
    }

    void HandleUnitClick(Unit unit)
    {
        if (unit.Faction != FactionId.LutheranSynod &&
            unit.Faction != FactionId.Schismatic &&
            FogOfWarManager.Instance != null &&
            !FogOfWarManager.Instance.IsVisible(unit.HexPosition))
            return;

        if (unit.Faction == FactionId.LutheranSynod &&
            unit.SynodPlayer != SynodPlayerId.Player1 &&
            FogOfWarManager.Instance != null &&
            !FogOfWarManager.Instance.IsVisible(unit.HexPosition))
            return;

        var tm = TurnManager.Instance;
        if (unit.Faction != tm.ActiveFaction) return;
        if (unit.Faction == FactionId.LutheranSynod && unit.SynodPlayer != tm.ActiveSynodPlayer) return;

        var selected = tm.SelectedUnit;
        if (selected != null && selected != unit && selected.IsOnMap &&
            AmphibiousTransport.TryEmbark(selected, unit))
        {
            tm.SelectUnit(unit);
            CameraFollow.Instance?.FollowUnit(unit);
            FocusUnit(unit);
            PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
            return;
        }

        if (tm.SelectedUnit == unit)
        {
            ClearHighlights();
            tm.SelectUnit(null);
            GalleyCargoPanel.Instance?.Hide();
            TerrainInfoPanel.Instance?.RefreshSelection();
            RefreshNomadicCapitalHighlights();
            return;
        }

        tm.SelectUnit(unit);
        CameraFollow.Instance?.FollowUnit(unit);
        FocusUnit(unit);
    }

    public void FocusUnit(Unit unit)
    {
        if (unit == null)
        {
            GalleyCargoPanel.Instance?.Hide();
            return;
        }

        ShowReachable(unit);
        TerrainInfoPanel.Instance?.RefreshSelection();
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        GalleyCargoPanel.Instance?.Refresh(unit);
    }

    void HandleHexClick(HexCoordinates hex)
    {
        if (!HexGridMap.Instance.TryGetTile(hex, out var tile)) return;

        if (FogOfWarManager.Instance != null &&
            FogOfWarManager.Instance.GetVisibility(hex) == FogVisibility.Unexplored)
            return;

        var tm = TurnManager.Instance;
        var selected = tm.SelectedUnit;

        if (selected == null)
        {
            if (tile.Occupant != null && tile.Occupant.Faction == tm.ActiveFaction)
            {
                tm.SelectUnit(tile.Occupant);
                CameraFollow.Instance?.FollowUnit(tile.Occupant);
                FocusUnit(tile.Occupant);
                return;
            }

            if (tile.Settlement != null &&
                tile.Settlement.Faction == tm.ActiveFaction &&
                (tm.ActiveFaction != FactionId.LutheranSynod ||
                 tile.Settlement.SynodPlayer == tm.ActiveSynodPlayer))
            {
                HandleCityClick(tile.Settlement);
                return;
            }

            return;
        }

        if (tile.Occupant == selected)
        {
            if (selected.HasMoveOrder)
            {
                selected.ClearMoveOrder();
                ShowReachable(selected);
                TerrainInfoPanel.Instance?.RefreshUnitDisplay();
                PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
                return;
            }

            ClearHighlights();
            tm.SelectUnit(null);
            GalleyCargoPanel.Instance?.Hide();
            TerrainInfoPanel.Instance?.RefreshSelection();
            RefreshNomadicCapitalHighlights();
            return;
        }

        if (tile.Occupant != null && FactionRelations.AreHostile(selected, tile.Occupant))
        {
            if (FogOfWarManager.Instance != null && !FogOfWarManager.Instance.IsVisible(hex))
                return;

            if (CombatSystem.AreInAttackRange(selected.HexPosition, hex, selected) && !selected.HasAttacked)
            {
                CombatSystem.Resolve(selected, tile.Occupant);
                ClearHighlights();
                TerrainInfoPanel.Instance?.RefreshUnitDisplay();
                if (selected.IsAlive)
                    ShowReachable(selected);
                PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
            }
            return;
        }

        if (tile.Occupant == null)
        {
            if (selected.Type == UnitType.CoastalGalley &&
                AmphibiousTransport.TryDisembark(
                    selected,
                    hex,
                    GalleyCargoPanel.Instance?.GetSelectedPassenger(selected)))
            {
                ShowReachable(selected);
                GalleyCargoPanel.Instance?.Refresh(selected);
                TerrainInfoPanel.Instance?.RefreshUnitDisplay();
                PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
                return;
            }

            if (selected.TryMoveTo(hex) || selected.TryIssueMoveOrder(hex))
            {
                CityManager.Instance?.TryCaptureCityAt(selected, hex);
                ShowReachable(selected);
                TerrainInfoPanel.Instance?.RefreshMissionaryTile();
                PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
            }
            return;
        }
    }

    public void ShowReachableForUnit(Unit selected) => ShowReachable(selected);

    public void RefreshHighlightsForSelection()
    {
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected != null && selected.IsOnMap)
            ShowReachable(selected);
    }

    void ShowReachable(Unit selected)
    {
        ClearHighlights();
        if (selected == null) return;

        if (HexGridMap.Instance.TryGetTile(selected.HexPosition, out var originTile))
            MarkHighlight(selected.HexPosition, HighlightKind.Selected);

        foreach (var coords in HexGridMap.Instance.GetReachableHexes(
                     selected.HexPosition, selected.MovementRemaining, selected.Faction, selected.Type))
            MarkHighlight(coords, HighlightKind.Move);

        HighlightMoveOrderPath(selected);
        HighlightAttackTargets(selected);
        HighlightDisembarkShores(selected);
        HighlightPlacementRecommendations(selected);

        if (AppealOverlayController.Instance != null && AppealOverlayController.Instance.IsActive)
            AppealOverlayController.Instance.Refresh();
    }

    void MarkHighlight(HexCoordinates coords, HighlightKind kind)
    {
        if (HexGridMap.Instance == null || !HexGridMap.Instance.TryGetTile(coords, out var tile))
            return;

        tile.SetHighlight(kind);
        highlightedTiles.Add(coords);
    }

    void HighlightDisembarkShores(Unit selected)
    {
        if (selected == null || selected.Type != UnitType.CoastalGalley)
            return;

        foreach (var coords in AmphibiousTransport.GetDisembarkHexes(selected))
            MarkHighlight(coords, HighlightKind.PlacementGood);
    }

    void HighlightPlacementRecommendations(Unit selected)
    {
        if (NomadicFoundingGate.IsNomadicPhase)
            HighlightCapitalFoundingSites();
    }

    public void HighlightCapitalFoundingSites()
    {
        if (!NomadicFoundingGate.IsNomadicPhase || HexGridMap.Instance == null)
            return;

        var top = CityPlacementAdvisor.GetTopCapitalSites(3);
        foreach (var entry in top)
        {
            if (FogOfWarManager.Instance != null &&
                FogOfWarManager.Instance.GetVisibility(entry.hex) == FogVisibility.Unexplored)
                continue;

            int tier = CityPlacementAdvisor.GetPlacementHighlightTier(entry.hex, top);
            if (tier == 0)
                continue;

            MarkHighlight(
                entry.hex,
                tier >= 2 ? HighlightKind.PlacementExcellent : HighlightKind.PlacementGood);
        }
    }

    public void RefreshNomadicCapitalHighlights()
    {
        if (!NomadicFoundingGate.IsNomadicPhase)
            return;

        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected != null && selected.IsOnMap)
        {
            ShowReachable(selected);
            return;
        }

        ClearHighlights();
        HighlightCapitalFoundingSites();
    }

    void HighlightMoveOrderPath(Unit selected)
    {
        if (selected == null || !selected.HasMoveOrder || HexGridMap.Instance == null)
            return;

        var target = selected.MoveOrderTarget.Value;
        if (!HexGridMap.Instance.TryFindMovementPath(
                selected.HexPosition, target, selected.Faction, selected.Type, out var path))
            return;

        foreach (var coords in path)
        {
            if (coords == selected.HexPosition || highlightedTiles.Contains(coords))
                continue;
            MarkHighlight(coords, HighlightKind.MovePath);
        }
    }

    void HighlightAttackTargets(Unit selected)
    {
        if (selected == null || selected.HasAttacked || TurnManager.Instance == null) return;

        foreach (var enemy in TurnManager.Instance.GetUnits(FactionId.Schismatic))
        {
            if (!enemy.IsAlive) continue;
            if (!CombatSystem.AreInAttackRange(selected.HexPosition, enemy.HexPosition, selected)) continue;
            if (FogOfWarManager.Instance != null && !FogOfWarManager.Instance.IsVisible(enemy.HexPosition))
                continue;
            MarkHighlight(enemy.HexPosition, HighlightKind.Attack);
        }

        foreach (var enemy in TurnManager.Instance.GetUnits(FactionId.LutheranSynod))
        {
            if (!enemy.IsAlive || !FactionRelations.AreHostile(selected, enemy)) continue;
            if (!CombatSystem.AreInAttackRange(selected.HexPosition, enemy.HexPosition, selected)) continue;
            if (FogOfWarManager.Instance != null && !FogOfWarManager.Instance.IsVisible(enemy.HexPosition))
                continue;
            MarkHighlight(enemy.HexPosition, HighlightKind.Attack);
        }
    }

    public void ClearHighlights()
    {
        if (HexGridMap.Instance == null)
        {
            highlightedTiles.Clear();
            return;
        }

        foreach (var coords in highlightedTiles)
        {
            if (HexGridMap.Instance.TryGetTile(coords, out var tile))
                tile.ClearHighlight();
        }

        highlightedTiles.Clear();
    }
}
