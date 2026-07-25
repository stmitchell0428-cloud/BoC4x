using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ConfessionTechPanel : MonoBehaviour
{
    public static ConfessionTechPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI detailText;
    ScrollRect detailScroll;
    Transform columnsRoot;
    ScrollRect scrollRect;
    RectTransform scrollContentRect;
    bool isOpen;

    ConfessionTechId? selectedTech;
    Button startResearchButton;
    TextMeshProUGUI startResearchButtonLabel;
    TechTreeCategory activeTree = TechTreeCategory.Spiritual;
    Button spiritualTabButton;
    Button secularTabButton;
    Image spiritualTabImage;
    Image secularTabImage;
    Transform treeTabsRoot;

    TMP_FontAsset uiFont;

    static readonly string[] EraNames =
    {
        "Reformation",
        "Confessions",
        "Age of Orthodoxy",
        "Synodical Era",
        "Modern Confession",
        "Global Witness"
    };

    void Awake()
    {
        Instance = this;
        uiFont = FindExistingFont();
        BuildUI();
        SetOpen(false);
    }

    void Start()
    {
        if (ConfessionResearchManager.Instance != null)
            ConfessionResearchManager.Instance.ResearchChanged += Refresh;
    }

    static TMP_FontAsset FindExistingFont()
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        return existing != null ? existing.font : null;
    }

    void ApplyFont(TextMeshProUGUI tmp)
    {
        if (uiFont != null)
            tmp.font = uiFont;
    }

    void OnDestroy()
    {
        if (ConfessionResearchManager.Instance != null)
            ConfessionResearchManager.Instance.ResearchChanged -= Refresh;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen)
                return;
            if (TurnManager.Instance == null || TurnManager.Instance.IsPlayerTurn)
                SetOpen(!isOpen);
        }

        if (!isOpen || scrollRect == null || Mouse.current == null) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftBracketKey.wasPressedThisFrame ||
                Keyboard.current.qKey.wasPressedThisFrame)
            {
                SwitchTree(TechTreeCategory.Spiritual);
                return;
            }

            if (Keyboard.current.rightBracketKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                SwitchTree(TechTreeCategory.Secular);
                return;
            }
        }

        float wheel = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(wheel) < 0.01f) return;

        var content = scrollRect.content;
        var viewport = scrollRect.viewport;
        if (content == null || viewport == null) return;

        float scrollStep = wheel * 0.0025f;
        if (content.rect.height > viewport.rect.height)
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollStep);
        if (content.rect.width > viewport.rect.width)
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - scrollStep);
    }

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        panelRoot = new GameObject("ConfessionTechPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(880f, 580f);
        panelRect.anchoredPosition = Vector2.zero;

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

        CreateTitle();
        CreateTreeTabs();
        CreateScrollArea();
        detailText = UiDetailPane.CreateSidebar(
            panelRoot.transform,
            out detailScroll,
            "Select a doctrine or culture tech.\n\nSpiritual and secular research run in parallel.",
            uiFont);
        startResearchButton = UiDetailPane.CreateSidebarActionButton(
            panelRoot.transform,
            "StartResearchButton",
            "Start research",
            58f,
            OnStartResearchClicked,
            uiFont,
            new Color(0.18f, 0.38f, 0.28f, 1f));
        startResearchButtonLabel = startResearchButton.GetComponentInChildren<TextMeshProUGUI>();
        startResearchButton.interactable = false;
        CreateCancelResearchButton();
        RebuildColumnsForActiveTree();
        BringHeaderAboveScroll();
        RebuildScrollContent();
    }

    void BringHeaderAboveScroll()
    {
        if (scrollRect == null || treeTabsRoot == null)
            return;

        int scrollIndex = scrollRect.transform.GetSiblingIndex();
        treeTabsRoot.SetSiblingIndex(scrollIndex + 1);
        var title = treeTabsRoot.parent != null
            ? treeTabsRoot.parent.Find("Title")
            : null;
        if (title != null)
            title.SetSiblingIndex(scrollIndex + 2);
    }

    void RebuildScrollContent()
    {
        if (scrollContentRect == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContentRect);
    }

    void CreateTitle()
    {
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelRoot.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 40f);
        titleRect.anchoredPosition = new Vector2(0f, -6f);

        var title = titleGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(title);
        title.raycastTarget = false;
        title.text = "Research  (T close, Q/E or [ ] switch trees)";
        title.fontSize = 20f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.92f, 0.88f, 0.72f);
    }

    void CreateTreeTabs()
    {
        var tabsGo = new GameObject("TreeTabs");
        treeTabsRoot = tabsGo.transform;
        tabsGo.transform.SetParent(panelRoot.transform, false);
        var tabsRect = tabsGo.AddComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0f, 1f);
        tabsRect.anchorMax = new Vector2(1f, 1f);
        tabsRect.pivot = new Vector2(0.5f, 1f);
        tabsRect.sizeDelta = new Vector2(-(UiDetailPane.SidebarWidth + 32f), 36f);
        tabsRect.anchoredPosition = new Vector2(-(UiDetailPane.SidebarWidth * 0.5f), -44f);

        var layout = tabsGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(16, 16, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        spiritualTabButton = CreateTreeTab(tabsGo.transform, "Spiritual", "Doctrine & Culture", OnSpiritualTabClicked, out spiritualTabImage);
        secularTabButton = CreateTreeTab(tabsGo.transform, "Secular", "Science & Civic", OnSecularTabClicked, out secularTabImage);
        UpdateTreeTabVisuals();
    }

    Button CreateTreeTab(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick, out Image bg)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);

        var layout = btnGo.AddComponent<LayoutElement>();
        layout.minHeight = 32f;
        layout.preferredHeight = 32f;
        layout.flexibleWidth = 1f;

        bg = btnGo.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.2f, 0.28f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.raycastTarget = false;
        tmp.text = label;
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    void OnSpiritualTabClicked() => SwitchTree(TechTreeCategory.Spiritual);

    void OnSecularTabClicked() => SwitchTree(TechTreeCategory.Secular);

    void SwitchTree(TechTreeCategory tree)
    {
        if (activeTree == tree) return;
        activeTree = tree;
        selectedTech = null;
        UiDetailPane.SetDetailText(
            detailText,
            detailScroll,
            activeTree == TechTreeCategory.Spiritual
                ? "Select a doctrine or culture tech.\n\nSpiritual research runs in parallel with secular research."
                : "Select a science or civic tech.\n\nSecular research runs in parallel with spiritual research.");
        UpdateTreeTabVisuals();
        RebuildColumnsForActiveTree();
        ResetScrollPosition();
        Refresh();
    }

    void ResetScrollPosition()
    {
        if (scrollRect == null) return;
        scrollRect.horizontalNormalizedPosition = 0f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    void UpdateTreeTabVisuals()
    {
        if (spiritualTabImage != null)
        {
            spiritualTabImage.color = activeTree == TechTreeCategory.Spiritual
                ? new Color(0.22f, 0.34f, 0.28f, 1f)
                : new Color(0.16f, 0.2f, 0.28f, 1f);
        }

        if (secularTabImage != null)
        {
            secularTabImage.color = activeTree == TechTreeCategory.Secular
                ? new Color(0.18f, 0.3f, 0.38f, 1f)
                : new Color(0.16f, 0.2f, 0.28f, 1f);
        }
    }

    void CreateScrollArea()
    {
        var scrollGo = new GameObject("Scroll");
        scrollGo.transform.SetParent(panelRoot.transform, false);
        var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(12f, 12f);
        scrollRectTransform.offsetMax = new Vector2(-UiDetailPane.SidebarWidth - 20f, -92f);

        scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 25f;
        scrollRect.inertia = true;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;

        var vpImage = viewport.AddComponent<Image>();
        vpImage.color = new Color(0f, 0f, 0f, 0.01f);
        vpImage.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        scrollContentRect = content.AddComponent<RectTransform>();
        scrollContentRect.anchorMin = new Vector2(0f, 1f);
        scrollContentRect.anchorMax = new Vector2(0f, 1f);
        scrollContentRect.pivot = new Vector2(0f, 1f);
        scrollContentRect.anchoredPosition = Vector2.zero;

        var hLayout = content.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 6f;
        hLayout.padding = new RectOffset(4, 4, 4, 4);
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.childAlignment = TextAnchor.UpperLeft;

        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRect;
        scrollRect.content = scrollContentRect;
        columnsRoot = content.transform;
    }

    void CreateCancelResearchButton()
    {
        var btnGo = new GameObject("CancelResearchButton");
        btnGo.transform.SetParent(panelRoot.transform, false);
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
        btn.onClick.AddListener(OnCancelResearchClicked);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(label);
        label.text = "Cancel research";
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    void OnCancelResearchClicked()
    {
        var rm = ConfessionResearchManager.Instance;
        if (rm == null) return;

        var tree = selectedTech.HasValue
            ? TechTreeRules.CategoryFor(selectedTech.Value)
            : activeTree;

        bool fullRefund = rm.WouldCancelRefundFull(tree);
        if (rm.CancelResearch(tree))
        {
            string treeLabel = tree == TechTreeCategory.Spiritual ? "Spiritual" : "Secular";
            UiDetailPane.SetDetailText(detailText, detailScroll, fullRefund
                ? $"{treeLabel} research cancelled.\n\nFull manuscript refund (same turn)."
                : $"{treeLabel} research cancelled.\n\nHalf the manuscript cost was refunded.");
        }
        else
        {
            UiDetailPane.SetDetailText(detailText, detailScroll, $"No active {tree.ToString().ToLower()} research to cancel.");
        }

        Refresh();
        UpdateStartResearchButton();
    }

    void RebuildColumnsForActiveTree()
    {
        if (columnsRoot == null) return;

        foreach (Transform child in columnsRoot)
            Destroy(child.gameObject);

        for (int tier = 1; tier <= ConfessionTechDatabase.TierCount; tier++)
        {
            if (!HasTreeContentForTier(tier, activeTree))
                continue;

            var col = new GameObject($"Era{tier}");
            col.transform.SetParent(columnsRoot, false);
            var colRect = col.AddComponent<RectTransform>();
            colRect.sizeDelta = new Vector2(170f, 0f);

            var le = col.AddComponent<LayoutElement>();
            le.minWidth = 170f;
            le.preferredWidth = 170f;

            var colFitter = col.AddComponent<ContentSizeFitter>();
            colFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            colFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = col.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(col.transform, false);
            var header = headerGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(header);
            header.text = EraNames[tier - 1];
            header.raycastTarget = false;
            header.fontSize = 15f;
            header.fontStyle = FontStyles.Bold;
            header.alignment = TextAlignmentOptions.Center;
            header.color = new Color(0.7f, 0.78f, 0.95f);
            headerGo.AddComponent<LayoutElement>().preferredHeight = 24f;

            if (activeTree == TechTreeCategory.Spiritual)
            {
                if (HasTrackForTier(tier, TechTrack.Doctrine, TechTreeCategory.Spiritual))
                {
                    AddTrackHeader(col.transform, "Doctrine");
                    foreach (var node in ConfessionTechDatabase.ByTier(tier, TechTrack.Doctrine))
                        CreateTechButton(col.transform, node);
                }

                if (HasTrackForTier(tier, TechTrack.Culture, TechTreeCategory.Spiritual))
                {
                    AddTrackHeader(col.transform, "Culture");
                    foreach (var node in ConfessionTechDatabase.ByTier(tier, TechTrack.Culture))
                        CreateTechButton(col.transform, node);
                }
            }
            else
            {
                if (HasTrackForTier(tier, TechTrack.Secular, TechTreeCategory.Secular))
                {
                    AddTrackHeader(col.transform, "Science & Civic");
                    foreach (var node in ConfessionTechDatabase.ByTier(tier, TechTrack.Secular))
                        CreateTechButton(col.transform, node);
                }
            }
        }

        RebuildScrollContent();
    }

    static bool HasTreeContentForTier(int tier, TechTreeCategory tree)
    {
        foreach (var _ in ConfessionTechDatabase.ByTier(tier, tree))
            return true;
        return false;
    }

    void AddTrackHeader(Transform parent, string label)
    {
        var go = new GameObject($"Track_{label}");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.raycastTarget = false;
        tmp.text = TmpTextSanitizer.Sanitize(label);
        tmp.fontSize = 12f;
        tmp.fontStyle = FontStyles.Italic;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = label switch
        {
            "Culture" => new Color(0.85f, 0.72f, 0.45f),
            "Secular" or "Science & Civic" => new Color(0.55f, 0.78f, 0.88f),
            _ => new Color(0.55f, 0.65f, 0.8f)
        };
        go.AddComponent<LayoutElement>().preferredHeight = 18f;
    }

    static bool HasTrackForTier(int tier, TechTrack track, TechTreeCategory tree)
    {
        foreach (var node in ConfessionTechDatabase.ByTier(tier, track))
        {
            if (TechTreeRules.CategoryFor(node.Track) == tree)
                return true;
        }

        return false;
    }

    void CreateTechButton(Transform parent, ConfessionTechNode node)
    {
        var btnGo = new GameObject(node.Id.ToString());
        btnGo.transform.SetParent(parent, false);

        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 44f;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.3f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        var id = node.Id;
        btn.onClick.AddListener(() => SelectTech(id));

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(6f, 4f);
        labelRect.offsetMax = new Vector2(-6f, -4f);

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(label);
        label.raycastTarget = false;
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.richText = true;
        label.text = TmpTextSanitizer.Sanitize(FormatButtonLabel(node));
    }

    string FormatButtonLabel(ConfessionTechNode node)
    {
        var rm = ConfessionResearchManager.Instance;
        var status = rm != null ? rm.GetStatus(node.Id) : ConfessionTechStatus.Locked;
        string statusLine = status switch
        {
            ConfessionTechStatus.Unlocked => "*",
            ConfessionTechStatus.Researching => ">",
            ConfessionTechStatus.Available => "+",
            ConfessionTechStatus.AdherenceLocked => "!",
            _ => "-"
        };

        string track = node.Track switch
        {
            TechTrack.Culture => "Culture | ",
            TechTrack.Secular => "Science | ",
            _ => ""
        };

        return $"<b>{statusLine} {track}{node.Name}</b>\n<size=13><color=#AABBCC>{node.ManuscriptCost} mss | {node.TurnsToComplete} turns</color></size>";
    }

    void SelectTech(ConfessionTechId id)
    {
        selectedTech = id;
        UiDetailPane.SetDetailText(detailText, detailScroll, BuildTechDetailText(id, null));
        UpdateStartResearchButton();
        Refresh();
    }

    void OnStartResearchClicked()
    {
        if (!selectedTech.HasValue) return;
        var rm = ConfessionResearchManager.Instance;
        if (rm == null) return;

        var id = selectedTech.Value;
        var tree = TechTreeRules.CategoryFor(id);
        bool hadOther = rm.HasActiveResearchInTree(tree) && rm.ActiveResearchIdForTree(tree) != id;
        string actionMessage;
        if (rm.TryStartResearch(id))
        {
            actionMessage = hadOther
                ? "\n<color=#88CC88><b>Research changed!</b></color>"
                : "\n<color=#88CC88><b>Research started!</b></color>";
        }
        else
        {
            actionMessage = "\n" + GetResearchStartFailureMessage(id);
        }

        UiDetailPane.SetDetailText(detailText, detailScroll, BuildTechDetailText(id, actionMessage));
        UpdateStartResearchButton();
        Refresh();
    }

    string BuildTechDetailText(ConfessionTechId id, string actionMessage)
    {
        var node = ConfessionTechDatabase.Get(id);
        var rm = ConfessionResearchManager.Instance;
        if (rm == null) return "";

        var sb = new StringBuilder();
        sb.AppendLine($"<size=22><b>{node.Name}</b></size>");

        if (node.HasFigure)
            sb.AppendLine($"<color=#C9B896>{node.FigureName} ({node.Lifespan})</color>");

        sb.AppendLine();
        sb.AppendLine(node.Description);
        sb.AppendLine();
        sb.AppendLine($"<b>Effect</b>\n{node.EffectSummary}");
        sb.AppendLine();
        sb.AppendLine($"<b>Cost</b>  {node.ManuscriptCost} manuscripts, {node.TurnsToComplete} turns");

        if (node.MinAdherence > 0f)
            sb.AppendLine($"<b>Adherence</b>  {node.MinAdherence:F0}%+ required");

        if (node.Prerequisites.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Requires</b>");
            foreach (var prereq in node.Prerequisites)
                sb.AppendLine($"* {ConfessionTechDatabase.Get(prereq).Name}");
        }

        sb.AppendLine();
        sb.AppendLine($"Potency now: {rm.AdherencePotency * 100f:F0}%");

        if (!string.IsNullOrEmpty(actionMessage))
            sb.AppendLine(actionMessage);

        return sb.ToString();
    }

    string GetResearchStartFailureMessage(ConfessionTechId id)
    {
        var rm = ConfessionResearchManager.Instance;
        if (rm == null || FirstSteps.Instance == null)
            return "<color=#FF8888>Faction state unavailable.</color>";

        return rm.GetStatus(id) switch
        {
            ConfessionTechStatus.Unlocked => "<color=#88CC88>Already completed.</color>",
            ConfessionTechStatus.Researching => "<color=#FFCC55>Already researching this doctrine.</color>",
            ConfessionTechStatus.AdherenceLocked => "<color=#CC8866>Need 40%+ adherence to research.</color>",
            ConfessionTechStatus.Locked => "<color=#888888>Prerequisites not met.</color>",
            ConfessionTechStatus.Available => "<color=#FF8888>Not enough manuscripts.</color>",
            _ => "<color=#888888>Cannot start this research.</color>"
        };
    }

    void UpdateStartResearchButton()
    {
        if (startResearchButton == null) return;

        if (!selectedTech.HasValue || ConfessionResearchManager.Instance == null)
        {
            startResearchButton.interactable = false;
            if (startResearchButtonLabel != null)
                startResearchButtonLabel.text = "Start research";
            return;
        }

        var rm = ConfessionResearchManager.Instance;
        var status = rm.GetStatus(selectedTech.Value);
        var tree = TechTreeRules.CategoryFor(selectedTech.Value);

        startResearchButton.interactable = status == ConfessionTechStatus.Available;
        if (startResearchButtonLabel != null)
        {
            startResearchButtonLabel.text = status switch
            {
                ConfessionTechStatus.Researching => "Researching...",
                ConfessionTechStatus.Unlocked => "Completed",
                ConfessionTechStatus.Available when rm.HasActiveResearchInTree(tree) => "Switch research",
                _ => "Start research"
            };
        }
    }

    public void Refresh()
    {
        if (columnsRoot == null) return;
        foreach (Transform col in columnsRoot)
        {
            foreach (Transform child in col)
            {
                if (child.name == "Header") continue;
                if (!System.Enum.TryParse<ConfessionTechId>(child.name, out var id)) continue;

                var tmp = child.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = TmpTextSanitizer.Sanitize(FormatButtonLabel(ConfessionTechDatabase.Get(id)));

                var img = child.GetComponent<Image>();
                if (img == null || ConfessionResearchManager.Instance == null) continue;

                var status = ConfessionResearchManager.Instance.GetStatus(id);
                bool isCulture = ConfessionTechDatabase.Get(id).Track == TechTrack.Culture;
                bool isSecular = ConfessionTechDatabase.Get(id).Track == TechTrack.Secular;
                bool isSelected = selectedTech == id;
                img.color = status switch
                {
                    ConfessionTechStatus.Unlocked => isSecular
                        ? new Color(0.14f, 0.28f, 0.32f, 1f)
                        : isCulture
                            ? new Color(0.28f, 0.26f, 0.14f, 1f)
                            : new Color(0.15f, 0.32f, 0.2f, 1f),
                    ConfessionTechStatus.Researching => isSecular
                        ? new Color(0.18f, 0.32f, 0.38f, 1f)
                        : isCulture
                            ? new Color(0.38f, 0.30f, 0.12f, 1f)
                            : new Color(0.35f, 0.28f, 0.12f, 1f),
                    ConfessionTechStatus.Available => isSecular
                        ? new Color(0.16f, 0.30f, 0.38f, 1f)
                        : isCulture
                            ? new Color(0.32f, 0.26f, 0.16f, 1f)
                            : new Color(0.18f, 0.28f, 0.42f, 1f),
                    ConfessionTechStatus.AdherenceLocked => new Color(0.22f, 0.16f, 0.14f, 1f),
                    _ => new Color(0.14f, 0.14f, 0.16f, 1f)
                };
                if (isSelected)
                    img.color = new Color(
                        Mathf.Min(img.color.r + 0.12f, 1f),
                        Mathf.Min(img.color.g + 0.12f, 1f),
                        Mathf.Min(img.color.b + 0.08f, 1f), 1f);
            }
        }

        UpdateStartResearchButton();
    }

    public bool IsOpen => isOpen;

    void SetOpen(bool open)
    {
        isOpen = open;
        if (panelRoot != null)
            panelRoot.SetActive(open);
        TerrainInfoPanel.Instance?.SetBottomHudVisible(!open && !(CityScreenPanel.Instance?.IsOpen ?? false));
        if (open)
        {
            BringHeaderAboveScroll();
            UpdateTreeTabVisuals();
            Refresh();
            RebuildScrollContent();
        }
    }
}
