using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Eden-to-synod chronicle displayed while the match world loads.</summary>
public class LoadingScreenPanel : MonoBehaviour
{
    public static LoadingScreenPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI chapterText;
    TextMeshProUGUI narrativeText;
    TextMeshProUGUI continueLabel;
    TextMeshProUGUI footerHintText;
    Button continueButton;
    Button skipButton;
    Image progressFill;

    int beatIndex;
    bool loadComplete;
    bool introSkipped;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

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

    void Update()
    {
        if (!IsVisible)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!introSkipped && keyboard.escapeKey.wasPressedThisFrame)
        {
            OnSkipIntroClicked();
            return;
        }

        if (continueButton == null || !continueButton.interactable)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame ||
            keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            OnContinueClicked();
        }
    }

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        panelRoot = new GameObject("LoadingScreenPanel");
        panelRoot.transform.SetParent(canvas.transform, false);
        panelRoot.transform.SetAsLastSibling();

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.09f, 0.97f);
        bg.raycastTarget = true;

        CreateLabel(panelRoot.transform, "Title", "Book of Concord",
            new Vector2(0f, -48f), new Vector2(640f, 40f), 30f, FontStyles.Bold, TextAlignmentOptions.Center);

        CreateLabel(panelRoot.transform, "Subtitle", "East of Eden  -  a chronicle of exile and confession",
            new Vector2(0f, -84f), new Vector2(620f, 28f), 14f, FontStyles.Italic, TextAlignmentOptions.Center)
            .color = new Color(0.72f, 0.76f, 0.82f);

        chapterText = CreateLabel(panelRoot.transform, "Chapter", "",
            new Vector2(0f, -124f), new Vector2(560f, 28f), 17f, FontStyles.Bold, TextAlignmentOptions.Center);
        chapterText.color = new Color(0.78f, 0.86f, 0.92f);

        var bodyGo = new GameObject("Narrative");
        bodyGo.transform.SetParent(panelRoot.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 1f);
        bodyRect.anchorMax = new Vector2(0.5f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.sizeDelta = new Vector2(560f, 340f);
        bodyRect.anchoredPosition = new Vector2(0f, -156f);
        narrativeText = bodyGo.AddComponent<TextMeshProUGUI>();
        CopyFont(narrativeText);
        narrativeText.fontSize = 15f;
        narrativeText.alignment = TextAlignmentOptions.TopLeft;
        narrativeText.richText = true;
        narrativeText.textWrappingMode = TextWrappingModes.Normal;
        narrativeText.lineSpacing = 2f;

        CreateBottomBar(panelRoot.transform);
        CreateContinueButton(panelRoot.transform);
        CreateSkipButton(panelRoot.transform);
    }

    void CreateBottomBar(Transform parent)
    {
        var barBgGo = new GameObject("ProgressBg");
        barBgGo.transform.SetParent(parent, false);
        var barBgRect = barBgGo.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.5f, 0f);
        barBgRect.anchorMax = new Vector2(0.5f, 0f);
        barBgRect.pivot = new Vector2(0.5f, 0f);
        barBgRect.sizeDelta = new Vector2(420f, 10f);
        barBgRect.anchoredPosition = new Vector2(0f, 96f);
        barBgGo.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 1f);

        var fillGo = new GameObject("ProgressFill");
        fillGo.transform.SetParent(barBgGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        progressFill = fillGo.AddComponent<Image>();
        progressFill.color = new Color(0.34f, 0.56f, 0.42f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;

        footerHintText = CreateBottomLabel(parent, "FooterHint", "Click Continue or press Space to read the next chapter.",
            new Vector2(0f, 72f), new Vector2(560f, 22f), 13f, FontStyles.Italic, TextAlignmentOptions.Center);
        footerHintText.color = new Color(0.68f, 0.72f, 0.78f);
    }

    void CreateContinueButton(Transform parent)
    {
        var btnGo = new GameObject("Continue");
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(220f, 44f);
        rect.anchoredPosition = new Vector2(0f, 28f);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.22f, 0.34f, 0.48f, 1f);
        continueButton = btnGo.AddComponent<Button>();
        continueButton.targetGraphic = img;
        continueButton.onClick.AddListener(OnContinueClicked);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        continueLabel = labelGo.AddComponent<TextMeshProUGUI>();
        CopyFont(continueLabel);
        continueLabel.alignment = TextAlignmentOptions.Center;
        continueLabel.fontStyle = FontStyles.Bold;
        continueLabel.text = "Continue";
        continueLabel.raycastTarget = false;
    }

    void CreateSkipButton(Transform parent)
    {
        var btnGo = new GameObject("SkipIntro");
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(140f, 36f);
        rect.anchoredPosition = new Vector2(-24f, -24f);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.24f, 0.92f);
        skipButton = btnGo.AddComponent<Button>();
        skipButton.targetGraphic = img;
        skipButton.onClick.AddListener(OnSkipIntroClicked);

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
        tmp.text = "Skip intro";
        tmp.raycastTarget = false;
    }

    public void Show()
    {
        if (panelRoot == null) return;
        panelRoot.transform.SetAsLastSibling();
        beatIndex = 0;
        loadComplete = false;
        introSkipped = false;
        SetLoadProgress(0f);
        ShowBeat(beatIndex);
        RefreshContinueButton();
        if (skipButton != null)
            skipButton.gameObject.SetActive(true);
        panelRoot.SetActive(true);
    }

    public void SetLoadProgress(float progress)
    {
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(progress);
    }

    public void NotifyLoadComplete()
    {
        loadComplete = true;
        SetLoadProgress(1f);

        if (introSkipped)
        {
            Hide();
            return;
        }

        RefreshContinueButton();
    }

    void ShowBeat(int index)
    {
        var beat = LoadingNarrative.GetBeat(index);
        if (chapterText != null)
            chapterText.text = TmpTextSanitizer.Sanitize(beat.Chapter);
        if (narrativeText != null)
            narrativeText.text = TmpTextSanitizer.Sanitize(beat.Body);
    }

    void RefreshContinueButton()
    {
        if (continueButton == null || continueLabel == null)
            return;

        bool onLastBeat = beatIndex >= LoadingNarrative.BeatCount - 1;
        continueLabel.text = TmpTextSanitizer.Sanitize(onLastBeat
            ? loadComplete ? "Go forth" : "Preparing the land..."
            : "Continue");
        continueButton.interactable = !onLastBeat || loadComplete;

        if (footerHintText != null)
        {
            if (introSkipped)
            {
                footerHintText.text = TmpTextSanitizer.Sanitize(loadComplete
                    ? "Entering the map..."
                    : "Skipping intro  -  the map is still generating...");
            }
            else
            {
                footerHintText.text = TmpTextSanitizer.Sanitize(onLastBeat
                    ? loadComplete
                        ? "Click Go forth or press Space to begin your match."
                        : "The map is still generating..."
                    : "Click Continue, press Space, or choose Skip intro (Esc).");
            }
        }

        if (skipButton != null)
            skipButton.gameObject.SetActive(!introSkipped);
    }

    void OnSkipIntroClicked()
    {
        if (introSkipped)
            return;

        introSkipped = true;
        if (loadComplete)
        {
            Hide();
            return;
        }

        ShowSkippedState();
        RefreshContinueButton();
    }

    void ShowSkippedState()
    {
        if (chapterText != null)
            chapterText.text = TmpTextSanitizer.Sanitize("Preparing the map");
        if (narrativeText != null)
            narrativeText.text = TmpTextSanitizer.Sanitize(
                "The chronicle is set aside for now. The wilderness east of Eden is still being laid hex by hex.\n\n" +
                "You will enter the match as soon as generation finishes.");
        beatIndex = LoadingNarrative.BeatCount - 1;
    }

    void OnContinueClicked()
    {
        if (continueButton != null && !continueButton.interactable)
            return;

        if (beatIndex < LoadingNarrative.BeatCount - 1)
        {
            beatIndex++;
            ShowBeat(beatIndex);
            RefreshContinueButton();
            return;
        }

        if (loadComplete)
            Hide();
    }

    public void Hide()
    {
        beatIndex = 0;
        loadComplete = false;
        introSkipped = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);
        GameHUD.Instance?.EnsureDashboardVisible();
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) tmp.font = existing.font;
        tmp.color = new Color(0.92f, 0.9f, 0.85f);
        tmp.raycastTarget = false;
    }

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
        return tmp;
    }

    static TextMeshProUGUI CreateBottomLabel(
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
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.text = TmpTextSanitizer.Sanitize(text);
        return tmp;
    }
}
