using UnityEngine;
using TMPro;

/// <summary>Top-left dashboard: population, research, production, Walther track.</summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    public float leftPadding = 16f;
    public float topPadding = 12f;
    public float rowGap = 8f;
    public float minRowHeight = 22f;
    public float panelWidth = 720f;
    public float primaryFontSize = 19f;
    public float secondaryFontSize = 17f;

    TextMeshProUGUI populationText;
    TextMeshProUGUI adherenceText;
    TextMeshProUGUI manuscriptText;
    TextMeshProUGUI waltherText;

    static readonly Color DashboardColor = new(0.92f, 0.9f, 0.85f);

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
        SetRowVisible(populationText, visible);
        SetRowVisible(adherenceText, visible);
        SetRowVisible(manuscriptText, visible);
        SetRowVisible(waltherText, visible);
    }

    static void SetRowVisible(TextMeshProUGUI tmp, bool visible)
    {
        if (tmp != null)
            tmp.gameObject.SetActive(visible);
    }

    public void ResolveReferences()
    {
        var faction = FirstSteps.Instance ?? FindAnyObjectByType<FirstSteps>();
        if (faction != null)
        {
            populationText ??= faction.populationUIText;
            adherenceText ??= faction.adherenceUIText;
            manuscriptText ??= faction.manuscriptUIText;
            waltherText ??= faction.waltherDashboardUIText;
        }

        if (populationText != null && adherenceText != null &&
            manuscriptText != null && waltherText != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            switch (tmp.gameObject.name)
            {
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
            faction.populationUIText ??= populationText;
            faction.adherenceUIText ??= adherenceText;
            faction.manuscriptUIText ??= manuscriptText;
            faction.waltherDashboardUIText ??= waltherText;
        }
    }

    public void Relayout()
    {
        ResolveReferences();

        float y = -topPadding;
        y = PlaceRow(populationText, y, primaryFontSize);
        y = PlaceRow(adherenceText, y, primaryFontSize);
        y = PlaceRow(manuscriptText, y, primaryFontSize);
        PlaceRow(waltherText, y, secondaryFontSize);
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
}
