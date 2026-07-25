using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Pick confessional identity when Wittenberg is founded.</summary>
public class IdentityPickerPanel : MonoBehaviour
{
    public static IdentityPickerPanel Instance { get; private set; }

    GameObject panelRoot;

    void Awake()
    {
        Instance = this;
        BuildUI();
        panelRoot.SetActive(false);
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

        panelRoot = new GameObject("IdentityPickerPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        var box = new GameObject("Box");
        box.transform.SetParent(panelRoot.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(560f, 400f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        CreateLabel(box.transform, "Title", "Choose the synod's confessional identity", new Vector2(0f, -12f), 22f, FontStyles.Bold);
        CreateLabel(box.transform, "Subtitle", "Wittenberg is founded  -  what will this church be known for?", new Vector2(0f, -44f), 16f, FontStyles.Normal);

        CreateIdentityButton(box.transform, ConfessionalIdentityId.MissionarySending, new Vector2(0f, -88f));
        CreateIdentityButton(box.transform, ConfessionalIdentityId.Magisterial, new Vector2(0f, -148f));
        CreateIdentityButton(box.transform, ConfessionalIdentityId.PastoralCare, new Vector2(0f, -208f));
        CreateIdentityButton(box.transform, ConfessionalIdentityId.ChemnitzConfessional, new Vector2(0f, -268f));
    }

    bool isRespecMode;

    void CreateIdentityButton(Transform parent, ConfessionalIdentityId id, Vector2 pos)
    {
        var btnGo = new GameObject(id.ToString());
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(500f, 58f);
        rect.anchoredPosition = pos;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.18f, 0.28f, 0.42f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SelectIdentity(id));

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontSize = 15f;
        tmp.text = TmpTextSanitizer.Sanitize(
            $"<b>{ConfessionalIdentityDatabase.DisplayName(id)}</b>\n" +
            ConfessionalIdentityDatabase.Description(id));
    }

    static void CreateLabel(Transform parent, string name, string text, Vector2 pos, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(520f, 32f);
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = TmpTextSanitizer.Sanitize(text);
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) tmp.font = existing.font;
        tmp.color = new Color(0.92f, 0.9f, 0.85f);
    }

    public void Show()
    {
        isRespecMode = false;
        SetHeader("Choose the synod's confessional identity",
            "Wittenberg is founded  -  what will this church be known for?");
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void ShowRespec()
    {
        isRespecMode = true;
        SetHeader("Confessional identity pivot",
            "The synod may redefine its witness once  -  choose a new identity.");
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    void SetHeader(string title, string subtitle)
    {
        var box = panelRoot?.transform.Find("Box");
        if (box == null) return;
        var titleLabel = box.Find("Title")?.GetComponent<TextMeshProUGUI>();
        var subtitleLabel = box.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        if (titleLabel != null) titleLabel.text = TmpTextSanitizer.Sanitize(title);
        if (subtitleLabel != null) subtitleLabel.text = TmpTextSanitizer.Sanitize(subtitle);
    }

    void SelectIdentity(ConfessionalIdentityId id)
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            faction.SetConfessionalIdentity(id);
            if (isRespecMode)
                faction.MarkIdentityRespecUsed();
            else
                faction.AddFame(10);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);

        ConfessionResearchManager.Instance?.ApplyBonusesToAllPlayerUnits();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh(
            isRespecMode
                ? $"Identity pivot: {ConfessionalIdentityDatabase.DisplayName(id)}"
                : $"Identity: {ConfessionalIdentityDatabase.DisplayName(id)}");
    }
}
