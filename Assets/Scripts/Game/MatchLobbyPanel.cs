using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Full pre-game lobby  -  seed, map size, players, heresy pack, wrap style (Decision 21).</summary>
public class MatchLobbyPanel : MonoBehaviour
{
    public static MatchLobbyPanel Instance { get; private set; }

    const float LabelX = -250f;
    const float ValueX = -40f;
    const float ValueWidth = 280f;
    const float ArrowWidth = 44f;
    const float ArrowGap = 6f;

    GameObject panelRoot;
    MatchSettings draft = MatchSettings.CreateDefault();
    MapSizePreset mapPreset = MapSizePreset.Standard;

    TextMeshProUGUI seedValueText;
    TextMeshProUGUI mapSizeValueText;
    TextMeshProUGUI playerCountValueText;
    TextMeshProUGUI wrapValueText;
    TextMeshProUGUI heresyValueText;
    TextMeshProUGUI coastalValueText;
    TextMeshProUGUI summaryText;
    TextMeshProUGUI seedCaptionText;
    TMP_InputField seedInput;

    void Awake()
    {
        Instance = this;
        draft = MatchSettings.CreateDefault();
        var (w, h) = MapSizePresets.Dimensions(mapPreset);
        draft.MapWidth = w;
        draft.MapHeight = h;
    }

    void Start() => EnsureUiBuilt();

    public bool EnsureUiBuilt()
    {
        if (panelRoot != null)
            return true;

        BuildUI();
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (panelRoot == null)
            Debug.LogError("MatchLobbyPanel: failed to build UI  -  no Canvas available.");

        return panelRoot != null;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void BuildUI()
    {
        GameUiRoot.EnsureEventSystem();
        var canvas = GameUiRoot.GetCanvas();
        if (canvas == null)
            return;

        panelRoot = new GameObject("MatchLobbyPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);

        var box = new GameObject("Box");
        box.transform.SetParent(panelRoot.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(640f, 580f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        CreateLabel(box.transform, "Title", "Book of Concord  -  New Match",
            new Vector2(0f, -16f), new Vector2(580f, 36f), 26f, FontStyles.Bold, TextAlignmentOptions.Center);
        CreateLabel(box.transform, "Subtitle",
            "Configure the map and schism pool before the synod sets out.",
            new Vector2(0f, -52f), new Vector2(580f, 48f), 15f, FontStyles.Normal, TextAlignmentOptions.Center);
        CreateLabel(box.transform, "Hint",
            "Use the  <  and  >  buttons beside each row to change settings. Solo (1 player) is the default.",
            new Vector2(0f, -88f), new Vector2(580f, 24f), 13f, FontStyles.Italic, TextAlignmentOptions.Center)
            .color = new Color(0.72f, 0.76f, 0.82f);

        float rowY = -124f;
        const float rowStep = 52f;

        CreateRow(box.transform, "Map seed", ref rowY, rowStep, out seedValueText, CycleSeed, createSeedInput: true);
        CreateRow(box.transform, "Map size", ref rowY, rowStep, out mapSizeValueText, CycleMapSize);
        CreateRow(box.transform, "Players", ref rowY, rowStep, out playerCountValueText, CyclePlayerCount);
        CreateRow(box.transform, "Map wrap", ref rowY, rowStep, out wrapValueText, CycleWrap);
        CreateRow(box.transform, "Heresy pack", ref rowY, rowStep, out heresyValueText, CycleHeresyPack);
        CreateRow(box.transform, "Coasts & rivers", ref rowY, rowStep, out coastalValueText, CycleCoastal);

        summaryText = CreateLabel(box.transform, "Summary", "",
            new Vector2(0f, -408f), new Vector2(560f, 96f), 14f, FontStyles.Italic, TextAlignmentOptions.TopLeft);
        summaryText.textWrappingMode = TextWrappingModes.Normal;
        summaryText.color = new Color(0.78f, 0.82f, 0.88f);

        CreateStartButton(box.transform);
        RefreshLabels();
    }

    void CreateRow(
        Transform parent,
        string label,
        ref float y,
        float step,
        out TextMeshProUGUI valueText,
        System.Action<int> onCycle,
        bool createSeedInput = false)
    {
        CreateLabel(parent, $"{label}Label", label,
            new Vector2(LabelX, y), new Vector2(170f, 28f), 16f, FontStyles.Bold, TextAlignmentOptions.MidlineRight);

        float prevX = ValueX - ArrowWidth - ArrowGap;
        float nextX = ValueX + ValueWidth + ArrowGap;
        float controlY = y - 4f;

        CreateArrowButton(parent, $"{label}Prev", "<", new Vector2(prevX, controlY), () => onCycle(-1));
        CreateArrowButton(parent, $"{label}Next", ">", new Vector2(nextX, controlY), () => onCycle(1));

        if (createSeedInput)
        {
            var frame = CreateValueFrame(parent, $"{label}Frame", new Vector2(ValueX, controlY),
                new Vector2(ValueWidth, 36f));

            var inputGo = new GameObject("SeedInput");
            inputGo.transform.SetParent(frame.transform, false);
            var inputRect = inputGo.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(10f, 4f);
            inputRect.offsetMax = new Vector2(-10f, -4f);

            seedInput = inputGo.AddComponent<TMP_InputField>();
            seedInput.textComponent = CreateInputText(inputGo.transform);
            seedInput.text = "0";
            seedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            seedInput.onEndEdit.AddListener(_ => ApplySeedFromInput());

            seedCaptionText = CreateLabel(parent, "SeedCaption", MapSeedPresets.Caption(0),
                new Vector2(ValueX, y - 38f), new Vector2(ValueWidth, 18f), 12f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            seedCaptionText.color = new Color(0.68f, 0.74f, 0.82f);

            valueText = CreateLabel(parent, $"{label}Value", "",
                new Vector2(ValueX, y), new Vector2(ValueWidth, 28f), 14f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            valueText.gameObject.SetActive(false);
        }
        else
        {
            var frame = CreateValueFrame(parent, $"{label}Frame", new Vector2(ValueX, controlY),
                new Vector2(ValueWidth, 36f));

            valueText = CreateLabel(parent, $"{label}Value", "",
                new Vector2(ValueX + 10f, y), new Vector2(ValueWidth - 20f, 36f), 15f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            valueText.transform.SetParent(frame.transform, false);
            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = new Vector2(10f, 4f);
            valueRect.offsetMax = new Vector2(-10f, -4f);
            valueText.textWrappingMode = TextWrappingModes.Normal;
            valueText.raycastTarget = false;
        }

        y -= step;
    }

    static GameObject CreateValueFrame(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var frameGo = new GameObject(name);
        frameGo.transform.SetParent(parent, false);
        var rect = frameGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        var bg = frameGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.16f, 0.22f, 1f);
        bg.raycastTarget = false;

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(frameGo.transform, false);
        var borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        var border = borderGo.AddComponent<Image>();
        border.color = new Color(0.34f, 0.46f, 0.62f, 0.85f);
        border.raycastTarget = false;

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(frameGo.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(1f, 1f);
        innerRect.offsetMax = new Vector2(-1f, -1f);
        var inner = innerGo.AddComponent<Image>();
        inner.color = new Color(0.12f, 0.16f, 0.22f, 1f);
        inner.raycastTarget = false;

        return frameGo;
    }

    static void CreateArrowButton(Transform parent, string name, string glyph, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(44f, 36f);
        rect.anchoredPosition = pos;

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(btnGo.transform, false);
        var borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        var border = borderGo.AddComponent<Image>();
        border.color = new Color(0.34f, 0.46f, 0.62f, 0.85f);
        border.raycastTarget = false;

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(btnGo.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(1f, 1f);
        innerRect.offsetMax = new Vector2(-1f, -1f);
        var img = innerGo.AddComponent<Image>();
        img.color = new Color(0.24f, 0.36f, 0.52f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor = new Color(0.24f, 0.36f, 0.52f, 1f);
        colors.highlightedColor = new Color(0.32f, 0.48f, 0.66f, 1f);
        colors.pressedColor = new Color(0.18f, 0.28f, 0.42f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        btn.onClick.AddListener(onClick);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.text = glyph;
        tmp.raycastTarget = false;
    }

    static TextMeshProUGUI CreateInputText(Transform parent)
    {
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(parent, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = 15f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    void CreateStartButton(Transform parent)
    {
        var btnGo = new GameObject("StartMatch");
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(220f, 44f);
        rect.anchoredPosition = new Vector2(0f, 20f);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.24f, 0.42f, 0.28f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(StartMatchClicked);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.text = "Begin Match";
    }

    static int WrapIndex(int index, int count) => (index % count + count) % count;

    static TextMeshProUGUI CreateLabel(
        Transform parent,
        string name,
        string text,
        Vector2 pos,
        Vector2 size,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.text = TmpTextSanitizer.Sanitize(text);
        tmp.raycastTarget = false;
        return tmp;
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null && existing.font != null)
            tmp.font = existing.font;
        else if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        tmp.color = new Color(0.92f, 0.9f, 0.85f);
    }

    void CycleSeed(int direction)
    {
        int idx = MapSeedPresets.IndexOf(draft.MapSeed);
        if (idx < 0)
            idx = 0;

        draft.MapSeed = MapSeedPresets.SeedAt(idx + direction);
        if (seedInput != null)
            seedInput.text = draft.MapSeed.ToString();
        RefreshLabels();
    }

    void ApplySeedFromInput()
    {
        if (seedInput == null) return;
        if (int.TryParse(seedInput.text, out int seed))
            draft.MapSeed = Mathf.Max(0, seed);
        else
            draft.MapSeed = 0;
        RefreshLabels();
    }

    void CycleMapSize(int direction)
    {
        mapPreset = (MapSizePreset)WrapIndex((int)mapPreset + direction, 3);
        var (w, h) = MapSizePresets.Dimensions(mapPreset);
        draft.MapWidth = w;
        draft.MapHeight = h;
        RefreshLabels();
    }

    void CyclePlayerCount(int direction)
    {
        draft.PlayerCount = WrapIndex(draft.PlayerCount - 1 + direction, 4) + 1;
        RefreshLabels();
    }

    void CycleWrap(int direction)
    {
        draft.WrapStyle = (MapWrapStyle)WrapIndex((int)draft.WrapStyle + direction, 3);
        RefreshLabels();
    }

    void CycleHeresyPack(int direction)
    {
        draft.HeresyPack = (HeresyPackId)WrapIndex((int)draft.HeresyPack + direction, 3);
        RefreshLabels();
    }

    void CycleCoastal(int direction)
    {
        int idx = draft.CoastalDensity == CoastalDensity.Normal ? 0 : 1;
        draft.CoastalDensity = WrapIndex(idx + direction, 2) == 0
            ? CoastalDensity.Normal
            : CoastalDensity.Archipelago;
        RefreshLabels();
    }

    void RefreshLabels()
    {
        if (seedInput != null && !seedInput.isFocused)
            seedInput.text = draft.MapSeed.ToString();
        if (seedCaptionText != null)
            seedCaptionText.text = TmpTextSanitizer.Sanitize(MapSeedPresets.Caption(draft.MapSeed));
        if (mapSizeValueText != null)
            mapSizeValueText.text = TmpTextSanitizer.Sanitize(MapSizePresets.Label(mapPreset));
        if (playerCountValueText != null)
            playerCountValueText.text = TmpTextSanitizer.Sanitize(draft.PlayerCount == 1
                ? "1 (solo synod)"
                : $"{draft.PlayerCount} ({draft.PlayerCount - 1} AI synod rival(s))");
        if (wrapValueText != null)
            wrapValueText.text = TmpTextSanitizer.Sanitize(MatchSettingsLabels.Wrap(draft.WrapStyle));
        if (heresyValueText != null)
            heresyValueText.text = TmpTextSanitizer.Sanitize(MatchSettingsLabels.HeresyPack(draft.HeresyPack));
        if (coastalValueText != null)
            coastalValueText.text = TmpTextSanitizer.Sanitize(MatchSettingsLabels.Coastal(draft.CoastalDensity));
        if (summaryText != null)
            summaryText.text = TmpTextSanitizer.Sanitize(draft.FormatSummary(multiline: true));
    }

    void StartMatchClicked()
    {
        ApplySeedFromInput();
        MatchLobbyController.Instance?.UpdateSettings(draft);
        MatchLobbyController.Instance?.BeginMatch(draft);
    }

    public void Show()
    {
        if (!EnsureUiBuilt())
            return;

        SetHudVisible(false);
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        GameHUD.SetDashboardVisible(true);
    }

    static void SetHudVisible(bool visible) => GameHUD.SetDashboardVisible(visible);
}
