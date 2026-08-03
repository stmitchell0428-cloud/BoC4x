using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>Detailed synod status: legacies, identity, yields, diplomacy — opened with Y.</summary>
public class SynodBriefPanel : MonoBehaviour
{
    public static SynodBriefPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI bodyText;
    Transform diplomacyActionsRoot;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        EnsureUI();
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!IsVisible || Keyboard.current == null)
            return;

        // Y is handled only in FirstSteps.Toggle — handling it here too double-fires
        // (Hide then Show) and leaves the brief stuck open.
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
    }

    void EnsureUI()
    {
        if (panelRoot != null && bodyText != null)
            return;

        var canvas = GameUiRoot.GetModalCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("SynodBriefPanel: no canvas available.");
            return;
        }

        BuildUI(canvas);
    }

    void BuildUI(Canvas canvas)
    {
        panelRoot = new GameObject("SynodBriefPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        var box = new GameObject("Box");
        box.transform.SetParent(panelRoot.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(620f, 560f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

        CreateLabel(box.transform, "Title", "Synod Brief", new Vector2(0f, -14f), 24f, FontStyles.Bold);
        CreateLabel(box.transform, "Subtitle", "Legacies, identity, yields, and diplomacy", new Vector2(0f, -44f), 14f, FontStyles.Italic);

        var scrollGo = new GameObject("Scroll");
        scrollGo.transform.SetParent(box.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(20f, 52f);
        scrollRect.offsetMax = new Vector2(-20f, -68f);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.5f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 400f);

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(content.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = new Vector2(-8f, 400f);

        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        CopyFont(bodyText);
        bodyText.fontSize = 14f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.richText = true;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Overflow;

        scroll.content = contentRect;
        scroll.viewport = viewportRect;

        var diplomacyGo = new GameObject("DiplomacyActions");
        diplomacyGo.transform.SetParent(box.transform, false);
        diplomacyActionsRoot = diplomacyGo.transform;
        var diplomacyRect = diplomacyGo.AddComponent<RectTransform>();
        diplomacyRect.anchorMin = new Vector2(0f, 0f);
        diplomacyRect.anchorMax = new Vector2(1f, 0f);
        diplomacyRect.pivot = new Vector2(0.5f, 0f);
        diplomacyRect.sizeDelta = new Vector2(-40f, 34f);
        diplomacyRect.anchoredPosition = new Vector2(0f, 52f);

        var diplomacyLayout = diplomacyGo.AddComponent<HorizontalLayoutGroup>();
        diplomacyLayout.spacing = 8f;
        diplomacyLayout.childAlignment = TextAnchor.MiddleCenter;
        diplomacyLayout.childControlWidth = false;
        diplomacyLayout.childControlHeight = true;
        diplomacyLayout.childForceExpandWidth = false;
        diplomacyLayout.childForceExpandHeight = true;

        CreateButton(box.transform, "Close (Y)", new Vector2(0f, 16f), Hide);
    }

    static void CreateLabel(Transform parent, string name, string text, Vector2 pos, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(560f, 28f);
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = TmpTextSanitizer.Sanitize(text);
        tmp.raycastTarget = false;
    }

    void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(label);
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(160f, 34f);
        rect.anchoredPosition = pos;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.22f, 0.32f, 0.48f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
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
        tmp.fontSize = 14f;
        tmp.text = TmpTextSanitizer.Sanitize(label);
        tmp.raycastTarget = false;
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null)
            tmp.font = existing.font;
        tmp.color = new Color(0.92f, 0.9f, 0.85f);
    }

    public void Toggle()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        EnsureUI();
        if (panelRoot == null || bodyText == null)
            return;

        RefreshContent();
        panelRoot.SetActive(true);
    }

    public void RefreshContent()
    {
        if (bodyText == null)
            return;

        bodyText.text = TmpTextSanitizer.Sanitize(FirstSteps.Instance?.FormatSynodBriefContent() ?? "Synod brief unavailable.");
        bodyText.ForceMeshUpdate();

        var content = bodyText.transform.parent.GetComponent<RectTransform>();
        if (content != null)
        {
            float height = Mathf.Max(400f, bodyText.preferredHeight + 16f);
            content.sizeDelta = new Vector2(0f, height);
            bodyText.rectTransform.sizeDelta = new Vector2(-8f, height);
        }

        RebuildDiplomacyActions();
    }

    void RebuildDiplomacyActions()
    {
        if (diplomacyActionsRoot == null)
            return;

        for (int i = diplomacyActionsRoot.childCount - 1; i >= 0; i--)
            Destroy(diplomacyActionsRoot.GetChild(i).gameObject);

        var diplomacy = SynodDiplomacyManager.Instance;
        if (diplomacy == null || !diplomacy.HasRivals)
            return;

        foreach (var rival in diplomacy.ActiveRivals)
        {
            if (diplomacy.IsTruceActive(rival))
                continue;

            var rivalId = rival;
            CreateDiplomacyButton(
                $"Colloquy truce — {SynodPlayerDatabase.DisplayName(rivalId)}",
                () => diplomacy.TryProposeTruceFromBrief(rivalId));
        }
    }

    void CreateDiplomacyButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(label);
        btnGo.transform.SetParent(diplomacyActionsRoot, false);
        btnGo.AddComponent<LayoutElement>().preferredWidth = 220f;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.16f, 0.28f, 0.22f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(6f, 0f);
        labelRect.offsetMax = new Vector2(-6f, 0f);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 12f;
        tmp.text = TmpTextSanitizer.Sanitize(label);
        tmp.raycastTarget = false;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
