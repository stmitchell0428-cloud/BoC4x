using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>Top-left dashboard: queues, population, research, production, Walther track.</summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    public float leftPadding = 14f;
    public float topPadding = 10f;
    public float rowGap = 6f;
    public float minRowHeight = 20f;
    public float panelWidth = 520f;
    public float queuePanelWidth = 360f;
    public float primaryFontSize = 17f;
    public float secondaryFontSize = 15f;
    public float queueFontSize = 17f;
    public float queuePanelPadding = 8f;

    TextMeshProUGUI queueReviewText;
    RectTransform queueReviewPanel;
    TextMeshProUGUI nomadicFoundingText;
    RectTransform nomadicFoundingPanel;
    TextMeshProUGUI populationText;
    TextMeshProUGUI adherenceText;
    TextMeshProUGUI manuscriptText;
    TextMeshProUGUI waltherText;
    RectTransform statsPanel;
    RectTransform dashboardColumnPanel;

    public float DashboardBottomY { get; private set; }

    static readonly Color DashboardColor = new(0.92f, 0.9f, 0.85f);
    static readonly Color StatsPanelColor = new(0.05f, 0.08f, 0.13f, 0.88f);
    static readonly Color StatsPanelBorderColor = new(0.35f, 0.48f, 0.62f, 0.75f);
    static readonly Color DashboardColumnColor = new(0.04f, 0.06f, 0.10f, 0.72f);
    static readonly Color QueuePanelColor = new(0.05f, 0.08f, 0.13f, 0.94f);
    static readonly Color QueuePanelBorderColor = new(0.35f, 0.48f, 0.62f, 0.9f);
    static readonly Color NomadicPanelColor = new(0.12f, 0.09f, 0.05f, 0.94f);
    static readonly Color NomadicPanelBorderColor = new(0.72f, 0.55f, 0.22f, 0.9f);

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        ResolveReferences();
        SetDashboardVisible(true);
        Relayout();
    }

    public static void SetDashboardVisible(bool visible)
    {
        Instance?.SetDashboardVisibleInternal(visible);
    }

    /// <summary>Hide only the BUILD/RESEARCH box; keep pop / adherence / manuscripts / Walther.</summary>
    public static void SetQueuePanelVisible(bool visible)
    {
        Instance?.SetQueuePanelVisibleInternal(visible);
    }

    public void EnsureDashboardVisible()
    {
        ResolveReferences();
        SetDashboardVisibleInternal(true);
        Relayout();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void SetDashboardVisibleInternal(bool visible)
    {
        ResolveReferences();
        SetRowVisible(queueReviewPanel, visible);
        SetRowVisible(nomadicFoundingPanel, visible && NomadicFoundingGate.IsNomadicPhase);
        SetRowVisible(populationText, visible);
        SetRowVisible(adherenceText, visible);
        SetRowVisible(manuscriptText, visible);
        SetRowVisible(waltherText, visible);
        SetRowVisible(statsPanel, visible);
        if (visible)
            Relayout();
    }

    void SetQueuePanelVisibleInternal(bool visible)
    {
        ResolveReferences();
        SetRowVisible(queueReviewPanel, visible);
        Relayout();
    }

    static void SetRowVisible(TextMeshProUGUI tmp, bool visible)
    {
        if (tmp != null)
            tmp.gameObject.SetActive(visible);
    }

    static void SetRowVisible(RectTransform rect, bool visible)
    {
        if (rect != null)
            rect.gameObject.SetActive(visible);
    }

    public void ResolveReferences()
    {
        EnsureQueueReviewRow();

        var faction = FirstSteps.Instance ?? FindAnyObjectByType<FirstSteps>();
        if (faction != null)
        {
            queueReviewText ??= faction.queueReviewUIText;
            populationText ??= faction.populationUIText;
            adherenceText ??= faction.adherenceUIText;
            manuscriptText ??= faction.manuscriptUIText;
            waltherText ??= faction.waltherDashboardUIText;
        }

        if (populationText != null && adherenceText != null &&
            manuscriptText != null && waltherText != null && queueReviewText != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            switch (tmp.gameObject.name)
            {
                case "QueueReviewText":
                    queueReviewText ??= tmp;
                    break;
                case "PopulationText":
                    populationText ??= tmp;
                    break;
                case "AdherenceText":
                    adherenceText ??= tmp;
                    break;
                case "ManuscriptText":
                    manuscriptText ??= tmp;
                    break;
                case "WaltherText":
                    waltherText ??= tmp;
                    break;
            }
        }

        if (faction != null)
        {
            faction.queueReviewUIText ??= queueReviewText;
            faction.populationUIText ??= populationText;
            faction.adherenceUIText ??= adherenceText;
            faction.manuscriptUIText ??= manuscriptText;
            faction.waltherDashboardUIText ??= waltherText;
        }
    }

    void EnsureQueueReviewRow()
    {
        if (queueReviewText != null && queueReviewPanel != null)
            return;

        Transform parent = populationText != null
            ? populationText.transform.parent
            : FindAnyObjectByType<Canvas>()?.transform;

        if (parent == null)
            return;

        var panelGo = new GameObject("QueueReviewPanel");
        panelGo.transform.SetParent(parent, false);
        queueReviewPanel = panelGo.AddComponent<RectTransform>();

        var bg = panelGo.AddComponent<Image>();
        bg.color = QueuePanelColor;

        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = QueuePanelBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        var textGo = new GameObject("QueueReviewText");
        textGo.transform.SetParent(panelGo.transform, false);
        queueReviewText = textGo.AddComponent<TextMeshProUGUI>();

        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null)
            queueReviewText.font = existing.font;

        queueReviewText.richText = true;
        queueReviewText.raycastTarget = false;
        queueReviewText.textWrappingMode = TextWrappingModes.Normal;
        queueReviewText.overflowMode = TextOverflowModes.Overflow;
        queueReviewText.alignment = TextAlignmentOptions.TopLeft;
        queueReviewText.color = DashboardColor;

        var faction = FirstSteps.Instance ?? FindAnyObjectByType<FirstSteps>();
        if (faction != null)
            faction.queueReviewUIText = queueReviewText;
    }

    public void Relayout()
    {
        ResolveReferences();

        float y = -topPadding;
        y = PlaceNomadicFoundingRow(y);
        y = PlaceQueueReviewRow(y);
        float statsTopY = y;
        y = PlaceRow(populationText, y, primaryFontSize);
        y = PlaceRow(adherenceText, y, primaryFontSize);
        y = PlaceRow(manuscriptText, y, primaryFontSize);
        y = PlaceRow(waltherText, y, secondaryFontSize);
        PlaceStatsPanel(statsTopY, y);

        DashboardBottomY = ComputeDashboardBottomY();
        UpdateDashboardColumnLayout();
        TurnPhaseBanner.Instance?.ApplyHudClearance(QueuePanelRightEdge + 8f, topPadding);
        TerrainInfoPanel.Instance?.ApplyTopHudClearance(DashboardBottomY);
    }

    void EnsureDashboardColumn()
    {
        if (dashboardColumnPanel != null)
            return;

        Transform parent = populationText != null
            ? populationText.transform.parent
            : FindAnyObjectByType<Canvas>()?.transform;
        if (parent == null)
            return;

        var go = new GameObject("DashboardColumn");
        go.transform.SetParent(parent, false);
        dashboardColumnPanel = go.AddComponent<RectTransform>();
        var bg = go.AddComponent<Image>();
        bg.color = DashboardColumnColor;
        bg.raycastTarget = false;
        dashboardColumnPanel.SetAsFirstSibling();
    }

    void UpdateDashboardColumnLayout()
    {
        EnsureDashboardColumn();
        if (dashboardColumnPanel == null)
            return;

        float height = Mathf.Max(80f, DashboardBottomY + 6f);
        float width = Mathf.Max(queuePanelWidth, panelWidth) + leftPadding + 4f;
        dashboardColumnPanel.anchorMin = new Vector2(0f, 1f);
        dashboardColumnPanel.anchorMax = new Vector2(0f, 1f);
        dashboardColumnPanel.pivot = new Vector2(0f, 1f);
        dashboardColumnPanel.anchoredPosition = new Vector2(2f, -2f);
        dashboardColumnPanel.sizeDelta = new Vector2(width, height);
    }

    float ComputeDashboardBottomY()
    {
        float bottom = topPadding;
        bottom = Mathf.Max(bottom, RowBottomEdge(nomadicFoundingPanel));
        bottom = Mathf.Max(bottom, RowBottomEdge(queueReviewPanel));
        bottom = Mathf.Max(bottom, RowBottomEdge(populationText?.rectTransform));
        bottom = Mathf.Max(bottom, RowBottomEdge(adherenceText?.rectTransform));
        bottom = Mathf.Max(bottom, RowBottomEdge(manuscriptText?.rectTransform));
        bottom = Mathf.Max(bottom, RowBottomEdge(waltherText?.rectTransform));
        return bottom;
    }

    static float RowBottomEdge(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return 0f;

        return -rect.anchoredPosition.y + rect.sizeDelta.y;
    }

    public float QueuePanelRightEdge => leftPadding + queuePanelWidth;

    float PlaceNomadicFoundingRow(float y)
    {
        if (!NomadicFoundingGate.IsNomadicPhase)
        {
            // Hide the Found Wittenberg checklist once a capital exists.
            if (nomadicFoundingText != null)
                nomadicFoundingText.text = "";
            SetRowVisible(nomadicFoundingPanel, false);
            SetRowVisible(nomadicFoundingText, false);
            return y;
        }

        EnsureNomadicFoundingRow();
        if (nomadicFoundingText == null || nomadicFoundingPanel == null)
            return y;

        string progress = NomadicFoundingGate.FormatProgressLine();
        nomadicFoundingText.text = TmpTextSanitizer.Sanitize(
            "<color=#FFDD88><b>Found Wittenberg</b></color>\n" +
            (progress ?? "Complete preach, scout survey, and catechism binding."));

        nomadicFoundingText.fontSize = queueFontSize - 1f;
        nomadicFoundingText.lineSpacing = 4f;
        nomadicFoundingText.margin = Vector4.zero;
        nomadicFoundingText.ForceMeshUpdate();

        float textHeight = Mathf.Max(minRowHeight * 2f, nomadicFoundingText.preferredHeight + 4f);
        float panelHeight = textHeight + queuePanelPadding * 2f;

        nomadicFoundingPanel.gameObject.SetActive(true);
        nomadicFoundingPanel.anchorMin = new Vector2(0f, 1f);
        nomadicFoundingPanel.anchorMax = new Vector2(0f, 1f);
        nomadicFoundingPanel.pivot = new Vector2(0f, 1f);
        nomadicFoundingPanel.anchoredPosition = new Vector2(leftPadding, y);
        nomadicFoundingPanel.sizeDelta = new Vector2(queuePanelWidth, panelHeight);

        var textRect = nomadicFoundingText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(queuePanelPadding, queuePanelPadding);
        textRect.offsetMax = new Vector2(-queuePanelPadding, -queuePanelPadding);

        return y - panelHeight - rowGap;
    }

    void EnsureNomadicFoundingRow()
    {
        if (nomadicFoundingText != null && nomadicFoundingPanel != null)
            return;

        Transform parent = populationText != null
            ? populationText.transform.parent
            : FindAnyObjectByType<Canvas>()?.transform;

        if (parent == null)
            return;

        var panelGo = new GameObject("NomadicFoundingPanel");
        panelGo.transform.SetParent(parent, false);
        nomadicFoundingPanel = panelGo.AddComponent<RectTransform>();

        var bg = panelGo.AddComponent<Image>();
        bg.color = NomadicPanelColor;

        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = NomadicPanelBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        var textGo = new GameObject("NomadicFoundingText");
        textGo.transform.SetParent(panelGo.transform, false);
        nomadicFoundingText = textGo.AddComponent<TextMeshProUGUI>();

        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null)
            nomadicFoundingText.font = existing.font;

        nomadicFoundingText.richText = true;
        nomadicFoundingText.raycastTarget = false;
        nomadicFoundingText.textWrappingMode = TextWrappingModes.Normal;
        nomadicFoundingText.overflowMode = TextOverflowModes.Overflow;
        nomadicFoundingText.alignment = TextAlignmentOptions.TopLeft;
        nomadicFoundingText.color = DashboardColor;
    }

    float PlaceQueueReviewRow(float y)
    {
        if (queueReviewText == null || queueReviewPanel == null)
            return y;
        if (!queueReviewPanel.gameObject.activeSelf)
            return y;

        queueReviewText.fontSize = queueFontSize;
        queueReviewText.lineSpacing = 4f;
        queueReviewText.margin = Vector4.zero;
        queueReviewText.ForceMeshUpdate();

        float textHeight = Mathf.Max(minRowHeight, queueReviewText.preferredHeight + 4f);
        float panelHeight = textHeight + queuePanelPadding * 2f;

        queueReviewPanel.anchorMin = new Vector2(0f, 1f);
        queueReviewPanel.anchorMax = new Vector2(0f, 1f);
        queueReviewPanel.pivot = new Vector2(0f, 1f);
        queueReviewPanel.anchoredPosition = new Vector2(leftPadding, y);
        queueReviewPanel.sizeDelta = new Vector2(queuePanelWidth, panelHeight);

        var textRect = queueReviewText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(queuePanelPadding, queuePanelPadding);
        textRect.offsetMax = new Vector2(-queuePanelPadding, -queuePanelPadding);

        return y - panelHeight - rowGap;
    }

    float PlaceRow(TextMeshProUGUI tmp, float y, float fontSize)
    {
        if (tmp == null) return y - minRowHeight;

        var rect = tmp.rectTransform;
        if (rect == null) return y - minRowHeight;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(leftPadding, y);
        rect.sizeDelta = new Vector2(panelWidth, minRowHeight);

        tmp.fontSize = fontSize;
        tmp.lineSpacing = 2f;
        tmp.margin = Vector4.zero;
        tmp.raycastTarget = false;
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = DashboardColor;

        tmp.ForceMeshUpdate();
        float height = Mathf.Max(minRowHeight, tmp.preferredHeight + 6f);
        rect.sizeDelta = new Vector2(panelWidth, height);
        return y - height - rowGap;
    }

    void PlaceStatsPanel(float topY, float bottomY)
    {
        if (populationText == null && adherenceText == null &&
            manuscriptText == null && waltherText == null)
        {
            SetRowVisible(statsPanel, false);
            return;
        }

        EnsureStatsPanel();
        if (statsPanel == null)
            return;

        bool anyVisible =
            IsRowActive(populationText) || IsRowActive(adherenceText) ||
            IsRowActive(manuscriptText) || IsRowActive(waltherText);
        statsPanel.gameObject.SetActive(anyVisible);
        if (!anyVisible)
            return;

        float height = topY - bottomY + rowGap;
        statsPanel.anchorMin = new Vector2(0f, 1f);
        statsPanel.anchorMax = new Vector2(0f, 1f);
        statsPanel.pivot = new Vector2(0f, 1f);
        statsPanel.anchoredPosition = new Vector2(leftPadding - 8f, topY + 6f);
        statsPanel.sizeDelta = new Vector2(panelWidth + 16f, height + 12f);
        statsPanel.SetAsFirstSibling();
    }

    static bool IsRowActive(TextMeshProUGUI tmp) =>
        tmp != null && tmp.gameObject.activeInHierarchy;

    void EnsureStatsPanel()
    {
        if (statsPanel != null)
            return;

        Transform parent = populationText != null
            ? populationText.transform.parent
            : FindAnyObjectByType<Canvas>()?.transform;
        if (parent == null)
            return;

        var panelGo = new GameObject("StatsPanel");
        panelGo.transform.SetParent(parent, false);
        statsPanel = panelGo.AddComponent<RectTransform>();

        var bg = panelGo.AddComponent<Image>();
        bg.color = StatsPanelColor;
        bg.raycastTarget = false;

        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = StatsPanelBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
    }
}
