using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Cargo slots and per-passenger landing when a coastal galley is selected.</summary>
public class GalleyCargoPanel : MonoBehaviour
{
    public static GalleyCargoPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI headerText;
    Transform slotRow;
    readonly List<GameObject> slotEntries = new();

    Unit trackedGalley;
    int selectedPassengerIndex;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        Instance = this;
        BuildUI();
        Hide();
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

        panelRoot = new GameObject("GalleyCargoPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(300f, 168f);
        rect.anchoredPosition = new Vector2(-12f, 12f);

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.1f, 0.16f, 0.94f);

        headerText = CreateText(panelRoot.transform, "<b>Galley cargo</b>", new Vector2(0f, -8f), 16f, FontStyles.Bold);

        slotRow = new GameObject("Slots").transform;
        slotRow.SetParent(panelRoot.transform, false);
        var slotRect = slotRow.gameObject.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 1f);
        slotRect.anchorMax = new Vector2(0.5f, 1f);
        slotRect.pivot = new Vector2(0.5f, 1f);
        slotRect.sizeDelta = new Vector2(280f, 96f);
        slotRect.anchoredPosition = new Vector2(0f, -34f);
    }

    public void Refresh(Unit unit)
    {
        if (!AmphibiousTransport.IsGalleyTransporter(unit) ||
            unit.SynodPlayer != SynodPlayerId.Player1)
        {
            Hide();
            return;
        }

        trackedGalley = unit;
        if (selectedPassengerIndex >= unit.EmbarkedCount)
            selectedPassengerIndex = 0;

        EnsureUI();
        panelRoot.SetActive(true);
        headerText.text = TmpTextSanitizer.Sanitize(
            $"<b>Galley cargo</b>  {unit.EmbarkedCount}/{unit.EmbarkCapacity}" +
            (unit.EmbarkedCount > 0
                ? "  |  click shore or <color=#FFDD66>Land</color>"
                : "  |  <color=#88CCFF>O</color> board adjacent troops"));

        RebuildSlots(unit);
    }

    public void Hide()
    {
        trackedGalley = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public Unit GetSelectedPassenger(Unit galley)
    {
        if (galley == null || galley != trackedGalley || galley.EmbarkedCount == 0)
            return null;

        var passengers = galley.EmbarkedPassengers;
        if (selectedPassengerIndex < 0 || selectedPassengerIndex >= passengers.Count)
            return passengers[0];

        return passengers[selectedPassengerIndex];
    }

    void RebuildSlots(Unit galley)
    {
        foreach (var entry in slotEntries)
        {
            if (entry != null)
                Destroy(entry);
        }
        slotEntries.Clear();

        for (int i = 0; i < galley.EmbarkCapacity; i++)
        {
            bool filled = i < galley.EmbarkedCount;
            var passenger = filled ? galley.EmbarkedPassengers[i] : null;
            bool selected = filled && i == selectedPassengerIndex;
            string label = filled
                ? $"{i + 1}. {Unit.TypeDisplayName(passenger.Type)}"
                : $"{i + 1}. Empty";

            float y = -i * 44f;
            var row = new GameObject($"Slot{i}");
            row.transform.SetParent(slotRow, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(270f, 38f);
            rowRect.anchoredPosition = new Vector2(0f, y);
            slotEntries.Add(row);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(170f, 32f);
            labelRect.anchoredPosition = new Vector2(8f, 0f);
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            CopyFont(labelText);
            labelText.fontSize = 13f;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = selected ? new Color(0.85f, 0.95f, 1f) : Color.white;
            labelText.text = TmpTextSanitizer.Sanitize(label);

            if (filled)
            {
                int captureIndex = i;
                CreateSmallButton(row.transform, "Select", new Vector2(188f, 0f), new Color(0.18f, 0.28f, 0.38f, 1f), () =>
                {
                    selectedPassengerIndex = captureIndex;
                    Refresh(galley);
                });
                CreateSmallButton(row.transform, "Land", new Vector2(248f, 0f), new Color(0.2f, 0.34f, 0.26f, 1f), () =>
                {
                    selectedPassengerIndex = captureIndex;
                    TryLandPassenger(galley, passenger);
                });
            }
        }
    }

    void TryLandPassenger(Unit galley, Unit passenger)
    {
        var targets = AmphibiousTransport.GetDisembarkHexes(galley);
        if (targets.Count == 0)
        {
            Debug.Log("Land unavailable — galley needs move points, cargo, and an adjacent shore hex.");
            return;
        }

        HexCoordinates best = PickLandingHex(galley, targets);
        if (!AmphibiousTransport.TryDisembark(galley, best, passenger))
            return;

        Unit landed = null;
        if (HexGridMap.Instance != null && HexGridMap.Instance.TryGetTile(best, out var tile))
            landed = tile.Occupant;

        if (landed != null)
        {
            TurnManager.Instance?.SelectUnit(landed);
            HexSelectionController.Instance?.FocusUnit(landed);
        }
        else
        {
            HexSelectionController.Instance?.FocusUnit(galley);
        }

        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
    }

    static HexCoordinates PickLandingHex(Unit galley, List<HexCoordinates> targets)
    {
        if (targets.Count == 1 || HexGridMap.Instance == null || CityManager.Instance == null)
            return targets[0];

        HexCoordinates best = targets[0];
        int bestScore = int.MinValue;
        foreach (var city in CityManager.Instance.AllCities)
        {
            if (city.Faction == galley.Faction && city.Faction == FactionId.LutheranSynod &&
                city.SynodPlayer == galley.SynodPlayer)
                continue;
            if (city.Faction != FactionId.Schismatic &&
                !(city.Faction == FactionId.LutheranSynod && city.SynodPlayer != galley.SynodPlayer))
                continue;

            foreach (var hex in targets)
            {
                int score = -HexGridMap.Instance.WrappedDistance(hex, city.HexPosition);
                if (city.IsCapital) score += 3;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = hex;
                }
            }
        }

        return bestScore > int.MinValue ? best : targets[0];
    }

    void EnsureUI()
    {
        if (panelRoot == null)
            BuildUI();
    }

    static TextMeshProUGUI CreateText(Transform parent, string text, Vector2 pos, float size, FontStyles style)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(280f, 24f);
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.color = Color.white;
        tmp.richText = true;
        tmp.text = TmpTextSanitizer.Sanitize(text);
        return tmp;
    }

    static void CreateSmallButton(Transform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(52f, 28f);
        rect.anchoredPosition = pos;

        go.AddComponent<Image>().color = color;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = 11f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;
    }

    static void CopyFont(TextMeshProUGUI target)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null)
            target.font = existing.font;
    }
}
