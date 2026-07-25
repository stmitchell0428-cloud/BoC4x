using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class CityScreenPanel : MonoBehaviour
{
    public static CityScreenPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI titleText;
    TextMeshProUGUI productionStatusText;
    TextMeshProUGUI statsText;
    TextMeshProUGUI detailText;
    ScrollRect detailScroll;
    Transform unitListRoot;
    Transform buildingListRoot;
    Transform secularListRoot;
    Transform upgradeListRoot;
    Transform cityTabsRoot;
    RectTransform buildListsRect;
    City activeCity;
    bool isOpen;

    CityBuildId? selectedBuild;
    UnitUpgradeId? selectedUpgrade;
    Button startBuildButton;
    TextMeshProUGUI startBuildButtonLabel;
    GameObject loyaltyBarRoot;
    Image loyaltyBarFill;
    TextMeshProUGUI loyaltyBarLabel;

    TMP_FontAsset uiFont;

    public bool IsOpen => isOpen;

    void Awake()
    {
        Instance = this;
        uiFont = FindAnyObjectByType<TextMeshProUGUI>()?.font;
        BuildUI();
        SetOpen(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current.cKey.wasPressedThisFrame) return;
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn) return;
        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen) return;

        if (isOpen)
        {
            SetOpen(false);
            return;
        }

        TryOpenNearestPlayerCity();
    }

    void TryOpenNearestPlayerCity()
    {
        if (CityManager.Instance == null) return;

        var cities = CityManager.Instance.GetPlayerCities();
        if (cities.Count == 0)
        {
            Debug.Log("No cities yet  -  select your settler on valid land and press F to found Wittenberg.");
            return;
        }

        City candidate = null;
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected != null && selected.Faction == FactionId.LutheranSynod)
            candidate = CityManager.Instance.GetCityForUnit(selected);

        if (candidate == null)
            candidate = CityManager.Instance.GetPrimaryPlayerCity() ?? cities[0];

        Open(candidate);
    }

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        panelRoot = new GameObject("CityScreenPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1200f, 620f);

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.13f, 0.97f);

        var leftColumnGo = new GameObject("LeftColumn");
        leftColumnGo.transform.SetParent(panelRoot.transform, false);
        var leftColumnRect = leftColumnGo.AddComponent<RectTransform>();
        leftColumnRect.anchorMin = Vector2.zero;
        leftColumnRect.anchorMax = Vector2.one;
        leftColumnRect.offsetMin = new Vector2(12f, 12f);
        leftColumnRect.offsetMax = new Vector2(-(UiDetailPane.SidebarWidth + 12f), -12f);

        var leftColumnLayout = leftColumnGo.AddComponent<VerticalLayoutGroup>();
        leftColumnLayout.spacing = 10f;
        leftColumnLayout.padding = new RectOffset(0, 48, 0, 0);
        leftColumnLayout.childControlWidth = true;
        leftColumnLayout.childControlHeight = true;
        leftColumnLayout.childForceExpandWidth = true;
        leftColumnLayout.childForceExpandHeight = false;
        leftColumnLayout.childAlignment = TextAnchor.UpperLeft;

        var headerGo = new GameObject("CityHeader");
        headerGo.transform.SetParent(leftColumnGo.transform, false);
        var headerLE = headerGo.AddComponent<LayoutElement>();
        headerLE.flexibleHeight = 0f;

        var headerLayout = headerGo.AddComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 6f;
        headerLayout.padding = new RectOffset(4, 4, 0, 0);
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;

        titleText = CreateLayoutLabel(headerGo.transform, "Title", 30f, 21f,
            TextAlignmentOptions.TopLeft, FontStyles.Bold);
        titleText.text = "City";

        var tabsGo = new GameObject("CityTabs");
        tabsGo.transform.SetParent(headerGo.transform, false);
        var tabsLE = tabsGo.AddComponent<LayoutElement>();
        tabsLE.preferredHeight = 26f;
        tabsLE.minHeight = 26f;
        var tabsLayout = tabsGo.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 6f;
        tabsLayout.childControlWidth = false;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = false;
        tabsLayout.childForceExpandHeight = true;
        cityTabsRoot = tabsGo.transform;

        productionStatusText = CreateLayoutLabel(headerGo.transform, "ProductionStatus", 22f, 14f,
            TextAlignmentOptions.TopLeft, FontStyles.Bold);
        productionStatusText.color = new Color(0.95f, 0.82f, 0.45f);

        statsText = CreateLayoutLabel(headerGo.transform, "Stats", 0f, 13f,
            TextAlignmentOptions.TopLeft);
        statsText.textWrappingMode = TextWrappingModes.Normal;
        statsText.overflowMode = TextOverflowModes.Overflow;
        var statsLE = statsText.gameObject.GetComponent<LayoutElement>();
        statsLE.minHeight = 64f;
        var statsFitter = statsText.gameObject.AddComponent<ContentSizeFitter>();
        statsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        statsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLoyaltyBar(headerGo.transform);

        var listsGo = new GameObject("BuildLists");
        listsGo.transform.SetParent(leftColumnGo.transform, false);
        buildListsRect = listsGo.AddComponent<RectTransform>();
        var listsLE = listsGo.AddComponent<LayoutElement>();
        listsLE.flexibleHeight = 1f;
        listsLE.minHeight = 220f;

        var listsLayout = listsGo.AddComponent<HorizontalLayoutGroup>();
        listsLayout.spacing = 10f;
        listsLayout.padding = new RectOffset(0, 0, 0, 0);
        listsLayout.childControlWidth = true;
        listsLayout.childControlHeight = true;
        listsLayout.childForceExpandWidth = true;
        listsLayout.childForceExpandHeight = true;
        listsLayout.childAlignment = TextAnchor.UpperLeft;

        unitListRoot = CreateBuildColumn(listsGo.transform, "Units", "Units");
        buildingListRoot = CreateBuildColumn(listsGo.transform, "Confessional", "Confessional");
        secularListRoot = CreateBuildColumn(listsGo.transform, "Secular", "Secular");
        upgradeListRoot = CreateBuildColumn(listsGo.transform, "Upgrades", "Upgrades (unit on city)");

        detailText = UiDetailPane.CreateSidebar(
            panelRoot.transform,
            out detailScroll,
            "Select a project to preview.\n\nUse Start production when ready.",
            uiFont);
        startBuildButton = UiDetailPane.CreateSidebarActionButton(
            panelRoot.transform,
            "StartBuildButton",
            "Start production",
            58f,
            OnStartBuildClicked,
            uiFont,
            new Color(0.18f, 0.38f, 0.28f, 1f));
        startBuildButtonLabel = startBuildButton.GetComponentInChildren<TextMeshProUGUI>();
        startBuildButton.interactable = false;

        CreateCloseButton(panelRoot.transform);
        CreateCancelButton(panelRoot.transform);
        PopulateBuildButtons();
        PopulateUpgradeButtons();
    }

    Transform CreateBuildColumn(Transform parent, string name, string headerText)
    {
        var columnGo = new GameObject(name);
        columnGo.transform.SetParent(parent, false);

        var columnLE = columnGo.AddComponent<LayoutElement>();
        columnLE.flexibleWidth = 1f;
        columnLE.minWidth = 220f;

        var columnLayout = columnGo.AddComponent<VerticalLayoutGroup>();
        columnLayout.spacing = 6f;
        columnLayout.childControlWidth = true;
        columnLayout.childControlHeight = true;
        columnLayout.childForceExpandWidth = true;
        columnLayout.childForceExpandHeight = false;
        columnLayout.padding = new RectOffset(0, 0, 0, 0);

        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(columnGo.transform, false);
        var headerLE = headerGo.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 22f;
        var header = headerGo.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) header.font = uiFont;
        header.text = headerText;
        header.fontSize = 14f;
        header.fontStyle = FontStyles.Bold;
        header.color = new Color(0.82f, 0.86f, 0.92f);
        header.alignment = TextAlignmentOptions.TopLeft;
        header.raycastTarget = false;

        var scrollShell = new GameObject("ScrollShell");
        scrollShell.transform.SetParent(columnGo.transform, false);
        var shellLE = scrollShell.AddComponent<LayoutElement>();
        shellLE.flexibleHeight = 1f;
        shellLE.minHeight = 200f;

        scrollShell.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.85f);

        var scrollGo = new GameObject("Scroll");
        scrollGo.transform.SetParent(scrollShell.transform, false);
        var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(4f, 4f);
        scrollRectTransform.offsetMax = new Vector2(-4f, -4f);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(2, 2, 2, 6);

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.viewport = vpRect;
        scroll.content = contentRect;

        return content.transform;
    }

    TextMeshProUGUI CreateLayoutLabel(Transform parent, string name, float preferredHeight, float fontSize,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        if (preferredHeight > 0f)
        {
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
        }

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) tmp.font = uiFont;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = new Color(0.9f, 0.92f, 0.88f);
        tmp.alignment = align;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.lineSpacing = 2f;
        return tmp;
    }

    void CreateLoyaltyBar(Transform parent)
    {
        loyaltyBarRoot = new GameObject("LoyaltyBar");
        loyaltyBarRoot.transform.SetParent(parent, false);

        var rowLE = loyaltyBarRoot.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 28f;
        rowLE.minHeight = 28f;

        var rowLayout = loyaltyBarRoot.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 4f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        loyaltyBarLabel = CreateLayoutLabel(loyaltyBarRoot.transform, "LoyaltyLabel", 14f, 12f,
            TextAlignmentOptions.TopLeft);
        loyaltyBarLabel.color = new Color(0.78f, 0.82f, 0.88f);

        var trackGo = new GameObject("Track");
        trackGo.transform.SetParent(loyaltyBarRoot.transform, false);
        var trackLE = trackGo.AddComponent<LayoutElement>();
        trackLE.preferredHeight = 12f;
        trackLE.minHeight = 12f;

        var trackImage = trackGo.AddComponent<Image>();
        trackImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(trackGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        loyaltyBarFill = fillGo.AddComponent<Image>();
        loyaltyBarFill.color = CityLoyaltySystem.LoyaltyBarColor(100f);
    }

    void UpdateLoyaltyBar(City city)
    {
        if (loyaltyBarRoot == null || loyaltyBarFill == null || loyaltyBarLabel == null || city == null)
            return;

        float loyalty = city.Loyalty;
        bool underSiege = CityLoyaltySystem.IsCityUnderEnemyOccupation(city);

        loyaltyBarLabel.text = TmpTextSanitizer.Sanitize(CityLoyaltySystem.CityScreenLoyaltyLabel(city));
        loyaltyBarFill.color = CityLoyaltySystem.LoyaltyBarColor(loyalty);

        var fillRect = loyaltyBarFill.rectTransform;
        float width = Mathf.Clamp01(loyalty / 100f);
        fillRect.anchorMax = new Vector2(width, 1f);

        if (underSiege)
            loyaltyBarLabel.color = new Color(0.95f, 0.72f, 0.45f);
        else if (city.Faction == FactionId.LutheranSynod)
            loyaltyBarLabel.color = new Color(0.72f, 0.88f, 0.76f);
        else
            loyaltyBarLabel.color = new Color(0.88f, 0.72f, 0.72f);
    }

    TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) tmp.font = uiFont;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = new Color(0.9f, 0.92f, 0.88f);
        tmp.alignment = align;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    void CreateCloseButton(Transform parent)
    {
        var btnGo = new GameObject("CloseButton");
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -12f);
        rect.sizeDelta = new Vector2(120f, 32f);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.22f, 0.28f, 0.38f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SetOpen(false));

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) label.font = uiFont;
        label.text = "Close";
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    void CreateCancelButton(Transform parent)
    {
        var btnGo = new GameObject("CancelBuildButton");
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-16f, 16f);
        rect.sizeDelta = new Vector2(UiDetailPane.SidebarWidth - 16f, 36f);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.38f, 0.22f, 0.18f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OnCancelBuildClicked);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) label.font = uiFont;
        label.text = "Cancel build";
        label.fontSize = 16f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    void OnCancelBuildClicked()
    {
        if (activeCity?.Production == null) return;
        if (!activeCity.Production.IsProducing)
        {
            UiDetailPane.SetDetailText(detailText, detailScroll, "No active city project to cancel.");
            return;
        }

        activeCity.Production.CancelActiveBuild();
        UiDetailPane.SetDetailText(detailText, detailScroll,
            "Production cancelled.\n\nConfessional projects refund half their manuscript cost.");
        Refresh();
        UpdateStartBuildButton();
    }

    void PopulateUpgradeButtons()
    {
        foreach (var def in UnitUpgradeDatabase.All)
            CreateUpgradeButton(upgradeListRoot, def.Id);
    }

    void CreateUpgradeButton(Transform parent, UnitUpgradeId id)
    {
        var def = UnitUpgradeDatabase.Get(id);
        var btnGo = new GameObject(id.ToString());
        btnGo.transform.SetParent(parent, false);

        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
        le.minHeight = 56f;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.18f, 0.28f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SelectUpgrade(id));

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 5f);
        labelRect.offsetMax = new Vector2(-8f, -5f);

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) label.font = uiFont;
        label.fontSize = 12f;
        label.lineSpacing = -2f;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.richText = true;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.text = FormatUpgradeButtonLabel(def, UnitUpgradeStatus.Locked);
    }

    string FormatUpgradeButtonLabel(UnitUpgradeDefinition def, UnitUpgradeStatus status)
    {
        string tag = status switch
        {
            UnitUpgradeStatus.Available => "+",
            UnitUpgradeStatus.WrongUnit => "-",
            UnitUpgradeStatus.NotOnCity => "o",
            UnitUpgradeStatus.InsufficientManuscripts => "$",
            UnitUpgradeStatus.ClergySlotsFull => "C",
            _ => "!"
        };
        string cost = $"{def.ManuscriptCost} mss";
        string from = Unit.TypeDisplayName(def.FromType);
        string to = Unit.TypeDisplayName(def.ToType);
        return $"<b>{tag} {def.Name}</b>\n<size=11><color=#AABBCC>{from} to {to} | {cost}</color></size>";
    }

    void SelectUpgrade(UnitUpgradeId id)
    {
        selectedUpgrade = id;
        var unit = TurnManager.Instance?.SelectedUnit;
        if (unit != null && UnitUpgradeService.TryUpgrade(unit, id))
        {
            UiDetailPane.SetDetailText(detailText, detailScroll, BuildUpgradeDetailText(id, "\n<color=#88CC88><b>Upgrade complete!</b></color>"));
            Refresh();
            return;
        }

        UiDetailPane.SetDetailText(detailText, detailScroll, BuildUpgradeDetailText(id, null));
        Refresh();
    }

    string BuildUpgradeDetailText(UnitUpgradeId id, string actionMessage)
    {
        var def = UnitUpgradeDatabase.Get(id);
        var unit = TurnManager.Instance?.SelectedUnit;
        var status = unit != null ? UnitUpgradeService.GetStatus(unit, id, activeCity) : UnitUpgradeStatus.WrongUnit;
        var sb = new StringBuilder();
        sb.AppendLine($"<size=22><b>{def.Name}</b></size>");
        sb.AppendLine();
        sb.AppendLine(def.Description);
        sb.AppendLine();
        sb.AppendLine($"<b>Effect</b>\n{def.EffectSummary}");
        sb.AppendLine();
        sb.AppendLine($"<b>Cost</b>  {def.ManuscriptCost} manuscripts (instant)");
        sb.AppendLine($"<b>Path</b>  {Unit.TypeDisplayName(def.FromType)} -> {Unit.TypeDisplayName(def.ToType)}");
        var techName = ConfessionTechDatabase.Get(def.RequiredTech).Name;
        bool techOk = ConfessionResearchManager.Instance != null &&
                        ConfessionResearchManager.Instance.IsTechUnlocked(def.RequiredTech);
        sb.AppendLine($"<b>Tech</b>  {techName}{(techOk ? " (unlocked)" : " (required)")}");
        sb.AppendLine();
        sb.AppendLine($"<b>Status</b>  {FormatUpgradeStatusHint(status, def, activeCity)}");
        if (!string.IsNullOrEmpty(actionMessage))
            sb.AppendLine(actionMessage);
        return sb.ToString();
    }

    static string FormatUpgradeStatusHint(UnitUpgradeStatus status, UnitUpgradeDefinition def, City city) => status switch
    {
        UnitUpgradeStatus.Available => "<color=#88CC88>Ready  -  click again or press U</color>",
        UnitUpgradeStatus.NotOnCity => "<color=#FFCC88>Move selected unit onto this city's hex</color>",
        UnitUpgradeStatus.WrongUnit => "<color=#888888>Selected unit cannot take this upgrade</color>",
        UnitUpgradeStatus.InsufficientManuscripts => "<color=#FF8888>Not enough manuscripts</color>",
        UnitUpgradeStatus.ClergySlotsFull => "<color=#FFCC88>Clergy roster full  -  replace or expand slots</color>",
        UnitUpgradeStatus.Locked when def != null && def.ToType is UnitType.Cantor or UnitType.Chaplain &&
            city != null && !ClergyRoster.HasSeminaryAccess(city)
            => "<color=#888888>Requires Seminary district or Seminary building in cluster</color>",
        _ => "<color=#888888>Research required tech first</color>"
    };

    void PopulateBuildButtons()
    {
        foreach (var def in CityBuildDatabase.ByCategory(CityBuildCategory.Unit))
            CreateBuildButton(unitListRoot, def.Id);
        foreach (var def in CityBuildDatabase.ByCategory(CityBuildCategory.ConfessionalBuilding))
            CreateBuildButton(buildingListRoot, def.Id);
        foreach (var def in CityBuildDatabase.ByCategory(CityBuildCategory.SecularBuilding))
            CreateBuildButton(secularListRoot, def.Id);
    }

    void CreateBuildButton(Transform parent, CityBuildId id)
    {
        var def = CityBuildDatabase.Get(id);
        var btnGo = new GameObject(id.ToString());
        btnGo.transform.SetParent(parent, false);

        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
        le.minHeight = 56f;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.16f, 0.22f, 0.3f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SelectBuild(id));

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 5f);
        labelRect.offsetMax = new Vector2(-8f, -5f);

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) label.font = uiFont;
        label.fontSize = 13f;
        label.lineSpacing = -2f;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.richText = true;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.text = FormatButtonLabel(def, CityBuildStatus.Available);
    }

    string FormatButtonLabel(CityBuildDefinition def, CityBuildStatus status)
    {
        string statusTag = status switch
        {
            CityBuildStatus.Completed => "*",
            CityBuildStatus.Building => ">",
            CityBuildStatus.Available => "+",
            _ => "-"
        };

        string progressLine = "";
        if (status == CityBuildStatus.Building &&
            activeCity?.Production != null &&
            activeCity.Production.ActiveBuildId == def.Id)
        {
            int? eta = activeCity.Production.EstimatedTurnsRemaining();
            if (eta.HasValue)
                progressLine = $"\n<size=11><color=#FFCC88>{eta.Value}t left</color></size>";
            else if (def.UsesProduction)
            {
                progressLine =
                    $"\n<size=11><color=#FFCC88>{activeCity.Production.ProductionProgress}/{def.ProductionCost}</color></size>";
            }
        }

        string costLine = FormatCostShort(def, activeCity);
        if (status == CityBuildStatus.Locked && id == CityBuildId.TrainColonist &&
            activeCity != null && !MissionHouseChain.CanTrainColonist(activeCity))
        {
            costLine = "Needs Mission House in cluster";
        }
        else if (status == CityBuildStatus.Locked && id == CityBuildId.TrainSiegeEngine &&
                 activeCity?.Production?.HasBuilding(CityBuildId.BuildArmory) != true)
        {
            costLine = "Needs Armory";
        }
        else if (status == CityBuildStatus.Locked && id == CityBuildId.TrainCoastalPatrol &&
                 activeCity != null && CityManager.Instance != null &&
                 !CityManager.Instance.CityTouchesNavalCoast(activeCity))
        {
            costLine = "Needs shore or naval coast";
        }
        else if (status == CityBuildStatus.Locked &&
                 (id == CityBuildId.BuildDock || id == CityBuildId.TrainCoastalGalley) &&
                 activeCity != null && CityManager.Instance != null &&
                 !CityManager.Instance.CityTouchesNavalCoast(activeCity))
        {
            costLine = "Needs shore or naval coast";
        }
        else if (status == CityBuildStatus.Locked && id == CityBuildId.TrainCoastalGalley &&
                 activeCity?.Production?.HasBuilding(CityBuildId.BuildDock) != true)
        {
            costLine = "Needs Dock";
        }
        else if (status == CityBuildStatus.Locked && def.RequiredTech.HasValue &&
            (ConfessionResearchManager.Instance == null ||
             !ConfessionResearchManager.Instance.IsTechUnlocked(def.RequiredTech.Value)))
        {
            costLine = $"Needs {ConfessionTechDatabase.Get(def.RequiredTech.Value).Name}";
        }

        return $"<b>{statusTag} {def.Name}</b>\n<size=11><color=#AABBCC>{costLine}</color></size>{progressLine}";
    }

    static string FormatCostShort(CityBuildDefinition def, City city = null)
    {
        if (def.UsesProduction)
            return $"{def.ProductionCost} production";

        int cost = def.ManuscriptCost;
        if (def.Id == CityBuildId.TrainColonist && city != null)
            cost = MissionHouseChain.EffectiveColonistCost(city);
        else if (def.Id == CityBuildId.TrainMissionary && city != null)
            cost = MissionHouseChain.EffectiveMissionaryCost(city);

        if (def.TurnsToComplete <= 1)
            return $"{cost} mss, {def.TurnsToComplete} turn";
        return $"{cost} mss, {def.TurnsToComplete} turns";
    }

    static string FormatCostDetail(CityBuildDefinition def, City city = null)
    {
        if (def.UsesProduction)
            return $"Cost: {def.ProductionCost} production (city yield applied each End Turn)";

        int cost = def.ManuscriptCost;
        if (def.Id == CityBuildId.TrainColonist && city != null)
            cost = MissionHouseChain.EffectiveColonistCost(city);
        else if (def.Id == CityBuildId.TrainMissionary && city != null)
            cost = MissionHouseChain.EffectiveMissionaryCost(city);

        return $"Cost: {cost} manuscripts, {def.TurnsToComplete} turns";
    }

    public void Open(City city)
    {
        if (city == null || city.Faction != FactionId.LutheranSynod) return;
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn) return;
        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen) return;

        if (activeCity != null && activeCity.Production != null)
            activeCity.Production.ProductionChanged -= Refresh;

        activeCity = city;
        selectedBuild = null;
        if (activeCity.Production != null)
            activeCity.Production.ProductionChanged += Refresh;

        if (activeCity.Production?.ActiveBuildId is CityBuildId activeId)
        {
            selectedBuild = activeId;
            UiDetailPane.SetDetailText(detailText, detailScroll, BuildBuildDetailText(activeId, null));
        }
        else
        {
            UiDetailPane.SetDetailText(detailText, detailScroll,
                "Select a project to preview.\n\nUse Start production when ready.");
        }

        UpdateStartBuildButton();
        SetOpen(true);

        if (city.IsHamlet && !city.HasChosenSpecialty)
            DistrictSpecialtyPickerPanel.Instance?.Show(city);
    }

    void RebuildCityTabs()
    {
        if (cityTabsRoot == null) return;

        foreach (Transform child in cityTabsRoot)
            Destroy(child.gameObject);

        if (CityManager.Instance == null)
            return;

        var cities = CityManager.Instance.GetPlayerCities();
        cityTabsRoot.gameObject.SetActive(cities.Count > 1);
        if (cities.Count <= 1)
            return;

        foreach (var city in cities)
        {
            var captured = city;
            var btnGo = new GameObject($"Tab_{city.CityName}");
            btnGo.transform.SetParent(cityTabsRoot, false);

            var layout = btnGo.AddComponent<LayoutElement>();
            layout.preferredWidth = Mathf.Min(160f, city.CityName.Length * 9f + 24f);
            layout.preferredHeight = 26f;

            var img = btnGo.AddComponent<Image>();
            bool selected = activeCity == city;
            img.color = selected
                ? new Color(0.22f, 0.38f, 0.58f, 1f)
                : new Color(0.16f, 0.18f, 0.24f, 1f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Open(captured));

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 2f);
            labelRect.offsetMax = new Vector2(-6f, -2f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) label.font = uiFont;
            label.text = TmpTextSanitizer.Sanitize(city.SettlementDisplayName());
            label.fontSize = 13f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }
    }

    void SelectBuild(CityBuildId id)
    {
        selectedBuild = id;
        UiDetailPane.SetDetailText(detailText, detailScroll, BuildBuildDetailText(id, null));
        UpdateStartBuildButton();
        Refresh();
    }

    void OnStartBuildClicked()
    {
        if (!selectedBuild.HasValue || activeCity?.Production == null) return;

        var id = selectedBuild.Value;
        string actionMessage;
        if (activeCity.Production.TryStartBuild(id))
            actionMessage = "\n<color=#88CC88><b>Production started!</b></color>";
        else
            actionMessage = "\n" + GetBuildStartFailureMessage(id);

        UiDetailPane.SetDetailText(detailText, detailScroll, BuildBuildDetailText(id, actionMessage));
        UpdateStartBuildButton();
        Refresh();
    }

    string BuildBuildDetailText(CityBuildId id, string actionMessage)
    {
        var def = CityBuildDatabase.Get(id);
        var sb = new StringBuilder();
        sb.AppendLine($"<size=22><b>{def.Name}</b></size>");
        sb.AppendLine();
        sb.AppendLine(def.Description);
        sb.AppendLine();
        sb.AppendLine($"<b>Effect</b>\n{def.EffectSummary}");
        sb.AppendLine();
        sb.AppendLine($"<b>Cost</b>  {FormatCostDetail(def, activeCity)}");

        if (def.UsesProduction && activeCity != null)
            sb.AppendLine($"<b>City yield</b>  {activeCity.ProductionYieldLabel()}");

        if (def.RequiredTech.HasValue)
        {
            var techName = ConfessionTechDatabase.Get(def.RequiredTech.Value).Name;
            bool unlocked = ConfessionResearchManager.Instance != null &&
                            ConfessionResearchManager.Instance.IsTechUnlocked(def.RequiredTech.Value);
            sb.AppendLine();
            sb.AppendLine(unlocked
                ? $"<b>Tech</b>  {techName} (unlocked)"
                : $"<b>Tech required</b>  {techName}");
        }

        if (!string.IsNullOrEmpty(actionMessage))
            sb.AppendLine(actionMessage);

        if (activeCity?.Production != null &&
            activeCity.Production.ActiveBuildId == id &&
            activeCity.Production.IsProducing)
        {
            sb.AppendLine();
            sb.AppendLine(activeCity.Production.ActiveBuildProgressBlock());
        }

        return sb.ToString();
    }

    string FormatProductionStatusLine()
    {
        if (activeCity?.Production == null || !activeCity.Production.IsProducing)
            return "Production: idle";

        var production = activeCity.Production;
        var def = CityBuildDatabase.Get(production.ActiveBuildId.Value);
        int? eta = production.EstimatedTurnsRemaining();

        if (def.UsesProduction)
        {
            string etaText = eta.HasValue ? $" | ~{eta.Value} turn{(eta.Value == 1 ? "" : "s")} left" : "";
            return $"Building: {def.Name} ({production.ProductionProgress}/{def.ProductionCost} prod{etaText})";
        }

        return $"Building: {def.Name} ({production.TurnsRemainingOnProject} turn{(production.TurnsRemainingOnProject == 1 ? "" : "s")} left)";
    }

    string GetBuildStartFailureMessage(CityBuildId id)
    {
        if (activeCity?.Production == null)
            return "<color=#FF8888>City unavailable.</color>";

        var def = CityBuildDatabase.Get(id);
        return activeCity.Production.GetStatus(id) switch
        {
            CityBuildStatus.Completed => "<color=#88CC88>Already built here.</color>",
            CityBuildStatus.Building => "<color=#FFCC55>Already in the queue.</color>",
            CityBuildStatus.Available => def.UsesProduction
                ? "<color=#FF8888>Cannot start  -  check manuscripts or queue.</color>"
                : "<color=#FF8888>Not enough manuscripts.</color>",
            _ when id == CityBuildId.TrainColonist && activeCity != null &&
                   !MissionHouseChain.CanTrainColonist(activeCity)
                => "<color=#888888>Build a Mission House anywhere in this city cluster first.</color>",
            _ when id == CityBuildId.TrainSiegeEngine &&
                   activeCity?.Production?.HasBuilding(CityBuildId.BuildArmory) != true
                => "<color=#888888>Requires an Armory in this city.</color>",
            _ when id == CityBuildId.TrainCoastalPatrol && activeCity != null &&
                   CityManager.Instance != null &&
                   !CityManager.Instance.CityTouchesNavalCoast(activeCity)
                => "<color=#888888>City must touch shore or a tagged naval coast hex.</color>",
            _ when (id == CityBuildId.BuildDock || id == CityBuildId.TrainCoastalGalley) &&
                   activeCity != null &&
                   CityManager.Instance != null &&
                   !CityManager.Instance.CityTouchesNavalCoast(activeCity)
                => "<color=#888888>City must touch shore or a tagged naval coast hex.</color>",
            _ when id == CityBuildId.TrainCoastalGalley &&
                   activeCity?.Production?.HasBuilding(CityBuildId.BuildDock) != true
                => "<color=#888888>Requires a Dock in this city.</color>",
            _ when def.RequiredTech.HasValue &&
                   (ConfessionResearchManager.Instance == null ||
                    !ConfessionResearchManager.Instance.IsTechUnlocked(def.RequiredTech.Value))
                => "<color=#888888>Requires secular tech first.</color>",
            _ when def.UsesProduction && CityGrowthSystem.GetProductionWorkerMultiplier(activeCity) < 0.5f
                => "<color=#FFCC88>Workers stretched  -  production at reduced speed.</color>",
            _ when activeCity != null && selectedBuild.HasValue &&
                   !ClergyRoster.CanTrainBuild(activeCity, selectedBuild.Value)
                => selectedBuild.Value switch
                {
                    CityBuildId.TrainPastor =>
                        "<color=#888888>Ordain one pastor per parish church in the cluster (build churches first).</color>",
                    CityBuildId.TrainDeaconess =>
                        "<color=#888888>Deaconess commissions require a Seminary district.</color>",
                    CityBuildId.TrainCantor =>
                        "<color=#888888>Cantors train at Seminary districts only.</color>",
                    _ => "<color=#888888>Not available at this settlement.</color>"
                },
            CityBuildStatus.ClergySlotsFull
                => "<color=#FFCC88>Clergy roster full  -  one per role, expand via Seminary district.</color>",
            _ => "<color=#888888>Finish current production first.</color>"
        };
    }

    void UpdateStartBuildButton()
    {
        if (startBuildButton == null) return;

        if (!selectedBuild.HasValue || activeCity?.Production == null)
        {
            startBuildButton.interactable = false;
            if (startBuildButtonLabel != null)
                startBuildButtonLabel.text = "Start production";
            return;
        }

        var status = activeCity.Production.GetStatus(selectedBuild.Value);
        startBuildButton.interactable = status == CityBuildStatus.Available;
        if (startBuildButtonLabel != null)
        {
            startBuildButtonLabel.text = status switch
            {
                CityBuildStatus.Building => "In queue",
                CityBuildStatus.Completed => "Completed",
                _ => "Start production"
            };
        }
    }

    public void RefreshIfOpen()
    {
        if (isOpen)
            Refresh();
    }

    public void Refresh()
    {
        if (!isOpen || activeCity == null) return;

        RebuildCityTabs();

        var faction = FirstSteps.Instance;
        int manuscripts = faction != null ? faction.scriptureManuscripts : 0;
        var production = activeCity.Production;

        if (titleText != null)
            titleText.text = TmpTextSanitizer.Sanitize($"{activeCity.SettlementDisplayName()}  (C to close)");

        if (productionStatusText != null)
            productionStatusText.text = TmpTextSanitizer.Sanitize(FormatProductionStatusLine());

        if (statsText != null)
        {
            var mssLine = activeCity.ManuscriptTilesLabel();
            statsText.text = TmpTextSanitizer.Sanitize(
                $"Pop {activeCity.Population}  |  Mss {manuscripts}  |  {activeCity.ProductionBreakdownLabel()}\n" +
                $"{activeCity.CultureSummaryLabel()}\n" +
                activeCity.TerritorySummaryLabel() +
                (string.IsNullOrEmpty(mssLine) ? "" : $"\n{mssLine}") +
                (string.IsNullOrEmpty(ClergyRoster.FormatRosterLine(activeCity))
                    ? ""
                    : $"\n{ClergyRoster.FormatRosterLine(activeCity)}"));
        }

        UpdateLoyaltyBar(activeCity);

        RefreshButtonList(unitListRoot, false);
        RefreshButtonList(buildingListRoot, false);
        RefreshButtonList(secularListRoot, true);
        RefreshUpgradeList();

        if (statsText != null && statsText.transform.parent is RectTransform headerRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(headerRect);
        if (buildListsRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(buildListsRect);

        Canvas.ForceUpdateCanvases();
    }

    void RefreshUpgradeList()
    {
        if (upgradeListRoot == null) return;
        var unit = TurnManager.Instance?.SelectedUnit;

        foreach (Transform child in upgradeListRoot)
        {
            if (!System.Enum.TryParse<UnitUpgradeId>(child.name, out var id)) continue;

            bool relevant = HamletSpecialtyDatabase.IsUpgradeAllowed(activeCity, id);
            child.gameObject.SetActive(relevant);
            if (!relevant) continue;

            var def = UnitUpgradeDatabase.Get(id);
            var status = unit != null
                ? UnitUpgradeService.GetStatus(unit, id, activeCity)
                : UnitUpgradeStatus.WrongUnit;

            var label = child.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = TmpTextSanitizer.Sanitize(FormatUpgradeButtonLabel(def, status));

            var img = child.GetComponent<Image>();
            if (img == null) continue;

            img.color = status switch
            {
                UnitUpgradeStatus.Available => new Color(0.28f, 0.22f, 0.42f, 1f),
                UnitUpgradeStatus.NotOnCity => new Color(0.18f, 0.16f, 0.22f, 1f),
                UnitUpgradeStatus.InsufficientManuscripts => new Color(0.28f, 0.16f, 0.16f, 1f),
                UnitUpgradeStatus.ClergySlotsFull => new Color(0.28f, 0.22f, 0.14f, 1f),
                _ => new Color(0.14f, 0.14f, 0.16f, 1f)
            };
            if (selectedUpgrade == id)
                img.color = new Color(
                    Mathf.Min(img.color.r + 0.1f, 1f),
                    Mathf.Min(img.color.g + 0.1f, 1f),
                    Mathf.Min(img.color.b + 0.08f, 1f), 1f);
        }
    }

    void RefreshButtonList(Transform root, bool secular)
    {
        if (root == null || activeCity?.Production == null) return;

        foreach (Transform child in root)
        {
            if (!System.Enum.TryParse<CityBuildId>(child.name, out var id)) continue;

            bool relevant = HamletSpecialtyDatabase.IsBuildAllowed(activeCity, id);
            child.gameObject.SetActive(relevant);

            if (!relevant) continue;

            var def = CityBuildDatabase.Get(id);
            var status = activeCity.Production.GetStatus(id);
            bool isSelected = selectedBuild == id;

            var label = child.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = TmpTextSanitizer.Sanitize(FormatButtonLabel(def, status));

            var img = child.GetComponent<Image>();
            if (img == null) continue;

            img.color = status switch
            {
                CityBuildStatus.Completed => new Color(0.14f, 0.28f, 0.18f, 1f),
                CityBuildStatus.Building => new Color(0.35f, 0.28f, 0.12f, 1f),
                CityBuildStatus.Available => secular
                    ? new Color(0.14f, 0.30f, 0.36f, 1f)
                    : new Color(0.18f, 0.28f, 0.42f, 1f),
                _ => new Color(0.14f, 0.14f, 0.16f, 1f)
            };
            if (isSelected)
                img.color = new Color(
                    Mathf.Min(img.color.r + 0.12f, 1f),
                    Mathf.Min(img.color.g + 0.12f, 1f),
                    Mathf.Min(img.color.b + 0.08f, 1f), 1f);
        }

        UpdateStartBuildButton();
    }

    void SetOpen(bool open)
    {
        isOpen = open;
        if (panelRoot != null)
            panelRoot.SetActive(open);

        TerrainInfoPanel.Instance?.SetBottomHudVisible(!open && !(ConfessionTechPanel.Instance?.IsOpen ?? false));

        if (!open)
        {
            if (activeCity != null && activeCity.Production != null)
                activeCity.Production.ProductionChanged -= Refresh;
            activeCity = null;
            selectedBuild = null;
            HexSelectionController.Instance?.ClearHighlights();
        }
        else
        {
            Refresh();
        }
    }
}
