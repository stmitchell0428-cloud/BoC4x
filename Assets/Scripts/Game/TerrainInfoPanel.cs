using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Hover readout, missionary end-turn tile, and terrain color legend.</summary>
public class TerrainInfoPanel : MonoBehaviour
{
    public static TerrainInfoPanel Instance { get; private set; }

    TextMeshProUGUI hoverText;
    TextMeshProUGUI missionaryText;
    TextMeshProUGUI cityYieldText;
    TextMeshProUGUI selectionText;
    RectTransform rootRect;
    GameObject rootObject;
    HexCoordinates? hoveredHex;

    const float LegendHeight = 28f;
    const float RowGap = 4f;
    const float PanelWidth = 720f;

    void Awake()
    {
        Instance = this;
        BuildUI();
        RefreshMissionaryTile();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var         root = new GameObject("TerrainInfoPanel");
        rootObject = root;
        root.transform.SetParent(canvas.transform, false);
        rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.zero;
        rootRect.pivot = Vector2.zero;
        rootRect.anchoredPosition = new Vector2(12f, 12f);
        rootRect.sizeDelta = new Vector2(PanelWidth, 168f);

        selectionText = CreateText(root.transform, "SelectionText", 16f);
        cityYieldText = CreateText(root.transform, "CityYieldText", 15f);
        missionaryText = CreateText(root.transform, "MissionaryText", 15f);
        hoverText = CreateText(root.transform, "HoverText", 15f);
        CreateText(root.transform, "LegendLine1", 12f).text =
            "O+ settler  > scout  ~ patrol  <> galley  + missionary  # soldier  |  O board  L land troops";
        CreateText(root.transform, "LegendLine2", 12f).text =
            "Settler/colonist: green = best founding hex  |  hover for yield rating";

        var legend1 = root.transform.Find("LegendLine1")?.GetComponent<RectTransform>();
        var legend2 = root.transform.Find("LegendLine2")?.GetComponent<RectTransform>();
        if (legend1 != null)
        {
            legend1.anchoredPosition = new Vector2(0f, 14f);
            legend1.sizeDelta = new Vector2(PanelWidth, 14f);
        }
        if (legend2 != null)
        {
            legend2.anchoredPosition = Vector2.zero;
            legend2.sizeDelta = new Vector2(PanelWidth, 14f);
        }

        SetPanelText(selectionText, "Selected: none");
        SetPanelText(cityYieldText, "City production: -");
        SetPanelText(hoverText, "Hover a hex or unit for details.");
        SetPanelText(missionaryText, "Missionary tile: -");
        RefreshCityYield();
    }

    public void SetBottomHudVisible(bool visible)
    {
        if (rootObject != null)
            rootObject.SetActive(visible);
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(680f, 22f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null)
            tmp.font = existing.font;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.9f, 0.92f, 0.88f);
        tmp.alignment = TextAlignmentOptions.BottomLeft;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    static void SetPanelText(TextMeshProUGUI tmp, string text)
    {
        if (tmp != null)
            tmp.text = TmpTextSanitizer.Sanitize(text);
    }

    void RelayoutPanel()
    {
        if (rootRect == null) return;

        float y = LegendHeight + RowGap;
        y = StackRow(hoverText, y);
        y = StackRow(missionaryText, y);
        y = StackRow(cityYieldText, y);
        y = StackRow(selectionText, y);
        rootRect.sizeDelta = new Vector2(PanelWidth, y + RowGap);
    }

    static float StackRow(TextMeshProUGUI tmp, float y)
    {
        if (tmp == null) return y;

        tmp.ForceMeshUpdate();
        float height = Mathf.Max(20f, tmp.preferredHeight + 4f);
        var rect = tmp.rectTransform;
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(680f, height);
        return y + height + RowGap;
    }

    public void RefreshCityYield()
    {
        if (cityYieldText == null) return;

        if (CityManager.Instance == null)
        {
            SetPanelText(cityYieldText, "City production: -");
            RelayoutPanel();
            return;
        }

        SetPanelText(cityYieldText, CityManager.Instance.FormatPlayerCityStatusLine());
        RelayoutPanel();
    }

    public void RefreshSelection()
    {
        if (selectionText == null) return;

        var selected = TurnManager.Instance != null ? TurnManager.Instance.SelectedUnit : null;
        if (selected == null)
        {
            SetPanelText(selectionText, "Selected: none - click a blue unit");
            RelayoutPanel();
            return;
        }

        string marker = selected.Type switch
        {
            UnitType.Settler => "O+",
            UnitType.Scout => ">",
            UnitType.Soldier => "#",
            UnitType.Slinger => "o",
            UnitType.Defender => "#",
            UnitType.Chaplain => "^",
            UnitType.Pastor => "P",
            UnitType.Bishop => "B",
            UnitType.Archbishop => "A",
            UnitType.Missionary => "x",
            UnitType.Cantor => "c",
            UnitType.SiegeEngine => "s",
            UnitType.CoastalPatrol => "~",
            UnitType.CoastalGalley => "<>",
            _ => "+"
        };
        SetPanelText(selectionText,
            $"<b>Selected: {marker} {Unit.TypeDisplayName(selected.Type)}</b> - {selected.RoleSummary}" +
            NavalMovementRules.FormatUnitNavalHint(selected) +
            AmphibiousTransport.FormatGalleyCargoHint(selected) +
            AmphibiousTransport.FormatEmbarkHint(selected) +
            FormatPlacementSelectionAdvice(selected));
        RelayoutPanel();
    }

    static string FormatPlacementSelectionAdvice(Unit selected)
    {
        if (selected == null) return "";

        if (selected.Type == UnitType.Settler && selected.IsNomadicFounder && NomadicFoundingGate.IsNomadicPhase)
        {
            if (!NomadicFoundingGate.RequirementsMet)
                return "\n" + NomadicFoundingGate.FormatProgressLine();

            var top = CityPlacementAdvisor.GetTopCapitalSites(3);
            var here = CityPlacementAdvisor.EvaluateCapitalSite(selected.HexPosition);
            var sb = new System.Text.StringBuilder();
            sb.Append("\n<color=#AADDFF><b>Capital placement</b></color>  -  ");
            sb.Append(here.FormatCompact());
            if (top.Count > 0)
            {
                sb.Append("\nBest sites: ");
                for (int i = 0; i < top.Count; i++)
                {
                    if (i > 0) sb.Append("  |  ");
                    sb.Append(top[i].score.FormatSiteLabel(top[i].hex));
                }
                sb.Append("\n<size=11><color=#88CCAA>Green highlights = recommended founding hexes</color></size>");
            }
            return sb.ToString();
        }

        return "";
    }

    public void RefreshUnitDisplay()
    {
        RefreshSelection();
        UpdateHoverText();
    }

    public void SetHoveredHex(HexCoordinates? coords)
    {
        hoveredHex = coords;
        UpdateHoverText();
    }

    public void RefreshMissionaryTile()
    {
        if (missionaryText == null) return;

        var faction = FirstSteps.Instance;
        if (faction == null || HexGridMap.Instance == null)
        {
            SetPanelText(missionaryText, "Missionary tile: -");
            return;
        }

        var leadUnit = FindLeadSynodUnit();
        if (leadUnit == null ||
            !HexGridMap.Instance.TryGetTerrainInfo(leadUnit.HexPosition, out var info))
        {
            SetPanelText(missionaryText, CityManager.Instance != null && CityManager.Instance.GetPrimaryPlayerCity() == null
                ? NomadicFoundingGate.FormatProgressLine() ?? "Nomadic start: preach, survey, and bind a catechism before founding."
                : "Missionary tile: -");
            RelayoutPanel();
            return;
        }

        SetPanelText(missionaryText, info.FormatMissionaryLine());
        UpdateHoverText();
        RelayoutPanel();
    }

    void UpdateHoverText()
    {
        if (hoverText == null) return;

        string text = "Hover a hex or unit for details.";

        if (hoveredHex.HasValue && HexGridMap.Instance != null &&
            HexGridMap.Instance.TryGetTile(hoveredHex.Value, out var tile))
        {
            if (FogOfWarManager.Instance != null &&
                FogOfWarManager.Instance.GetVisibility(hoveredHex.Value) == FogVisibility.Unexplored)
            {
                text = "<b>Unexplored</b>  -  send a unit to scout this territory.";
            }
            else if (FogOfWarManager.Instance != null &&
                     FogOfWarManager.Instance.GetVisibility(hoveredHex.Value) == FogVisibility.Explored &&
                     tile.Occupant != null &&
                     tile.Occupant.Faction != FactionId.LutheranSynod)
            {
                text = "<b>Explored</b>  -  no synod units in sight here now.";
            }
            else if (tile.Occupant != null)
            {
                var unit = tile.Occupant;
                string marker = unit.Type switch
                {
                    UnitType.Settler => "O+",
                    UnitType.Scout => ">",
                    UnitType.Soldier => "#",
                    UnitType.Slinger => "o",
                    UnitType.Defender => "#",
                    UnitType.Chaplain => "^",
                    UnitType.Pastor => "P",
                    UnitType.Bishop => "B",
                    UnitType.Archbishop => "A",
                    UnitType.Missionary => "x",
                    UnitType.Cantor => "c",
                    UnitType.SiegeEngine => "s",
                    UnitType.CoastalPatrol => "~",
                    UnitType.CoastalGalley => "<>",
                    _ => "+"
                };
                string actionHint = unit.CanFoundNomadicCapital
                    ? "  |  <color=#FFDD66>F = found Wittenberg</color>"
                    : unit.CanFoundFrontierCity
                        ? "  |  <color=#FFDD66>F = found 2nd city</color>"
                    : unit.CanNomadicPreach && NomadicFoundingGate.IsNomadicPhase
                        ? "  |  <color=#FFDD66>Space = preach</color>"
                        : "";
                string cityHint = tile.Settlement != null &&
                                  tile.Settlement.Faction == FactionId.LutheranSynod &&
                                  tile.Settlement.SynodPlayer == SynodPlayerId.Player1
                    ? "  |  <color=#AACCFF>C = city</color>"
                    : "";
                string placementHint = FormatHoveredPlacementAdvice(hoveredHex.Value);
                text =
                    $"<b>{marker} {Unit.TypeDisplayName(unit.Type)}</b> ({SynodPlayerDatabase.DisplayName(unit.SynodPlayer)})  -  {unit.RoleSummary}" +
                    NavalMovementRules.FormatUnitNavalHint(unit) +
                    $"{actionHint}{cityHint}{placementHint}";
            }
            else if (tile.Settlement != null)
            {
                var city = tile.Settlement;
                var mssLabel = city.ManuscriptTilesLabel();
                var selected = TurnManager.Instance?.SelectedUnit;
                text +=
                    $"<b>{city.SettlementDisplayName()}</b> ({SynodPlayerDatabase.DisplayName(city.SynodPlayer)}, {city.SettlementKindLabel()})  -  {city.ProductionBreakdownLabel()}\n" +
                    $"{city.CultureSummaryLabel()}  |  {city.TerritorySummaryLabel()}  |  Queue: {city.Production?.ActiveBuildLabel() ?? "None"}\n" +
                    CityLoyaltySystem.FormatHoverLoyaltyBlock(city, selected) +
                    GarrisonBonus.FormatCityGarrisonHint(city);
                if (!string.IsNullOrEmpty(mssLabel))
                    text += $"\n{mssLabel}";
                text += city.Faction == FactionId.LutheranSynod && city.SynodPlayer == SynodPlayerId.Player1
                    ? "  |  <color=#AACCFF>Click or C to manage</color>"
                    : city.Faction == FactionId.LutheranSynod &&
                      city.SynodPlayer != SynodPlayerId.Player1 &&
                      SynodDiplomacyManager.Instance != null
                        ? $"\n{SynodDiplomacyManager.Instance.FormatStatusLabel(city.SynodPlayer)}  |  <color=#AABBCC>D diplomacy</color>"
                        : "";
            }
            else if (HexGridMap.Instance.TryGetTerrainInfo(hoveredHex.Value, out var info))
            {
                text = info.FormatHoverLine();
                text += FormatHoveredPlacementAdvice(hoveredHex.Value);
                var leadUnit = FindLeadSynodUnit();
                if (leadUnit != null && leadUnit.HexPosition == hoveredHex.Value)
                {
                    text += leadUnit.Type == UnitType.Settler
                        ? "  <color=#FFDD66><b><- settler here</b></color>"
                        : "  <color=#FFDD66><b><- missionary here</b></color>";
                }
            }
        }

        SetPanelText(hoverText, text);
        RelayoutPanel();
    }

    static string FormatHoveredPlacementAdvice(HexCoordinates hex)
    {
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected == null) return "";

        if (selected.CanFoundNomadicCapital)
        {
            var score = CityPlacementAdvisor.EvaluateCapitalSite(hex);
            return $"\n<color=#AADDFF><b>Capital site:</b></color> {score.FormatCompact()}";
        }

        return "";
    }

    static Unit FindLeadSynodUnit()
    {
        if (TurnManager.Instance == null) return null;
        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
        {
            if (!unit.IsAlive) continue;
            if (unit.Type == UnitType.Settler && unit.IsNomadicFounder)
                return unit;
        }

        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
        {
            if (unit.Type == UnitType.Missionary && unit.IsAlive)
                return unit;
        }

        return null;
    }

    static Unit FindMissionary()
    {
        if (TurnManager.Instance == null) return null;
        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
        {
            if (unit.Type == UnitType.Missionary && unit.IsAlive)
                return unit;
        }
        return null;
    }
}
