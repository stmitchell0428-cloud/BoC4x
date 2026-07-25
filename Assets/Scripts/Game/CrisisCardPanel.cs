using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public readonly struct CrisisCardChoice
{
    public readonly string Label;
    public readonly string Description;
    public readonly Action OnSelect;

    public CrisisCardChoice(string label, string description, Action onSelect)
    {
        Label = label;
        Description = description;
        OnSelect = onSelect;
    }
}

/// <summary>Modal crisis cards  -  concede, debate, discipline before schism (Decision 5/19).</summary>
public class CrisisCardPanel : MonoBehaviour
{
    public static CrisisCardPanel Instance { get; private set; }

    GameObject uiRoot;
    GameObject panelRoot;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    Transform buttonRow;
    readonly List<GameObject> choiceButtons = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsureUI();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        TearDownUI();
    }

    void Update()
    {
        if (!IsVisible || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            ForceDismiss();
    }

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    bool IsUiReady =>
        panelRoot != null && titleText != null && bodyText != null && buttonRow != null;

    void EnsureUI()
    {
        if (IsUiReady)
            return;

        TearDownUI();
        BuildUI();
    }

    void TearDownUI()
    {
        foreach (var btn in choiceButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        choiceButtons.Clear();

        if (panelRoot != null)
        {
            Destroy(panelRoot);
            panelRoot = null;
        }

        if (uiRoot != null)
        {
            Destroy(uiRoot);
            uiRoot = null;
        }

        titleText = null;
        bodyText = null;
        buttonRow = null;
    }

    void BuildUI()
    {
        try
        {
            GameUiRoot.EnsureEventSystem();
            var canvas = GameUiRoot.GetModalCanvas();
            if (canvas == null)
            {
                Debug.LogError("CrisisCardPanel: could not resolve a UI canvas.");
                return;
            }

            uiRoot = new GameObject("CrisisCardUiRoot");
            uiRoot.transform.SetParent(canvas.transform, false);

            var uiRootRect = uiRoot.AddComponent<RectTransform>();
            uiRootRect.anchorMin = Vector2.zero;
            uiRootRect.anchorMax = Vector2.one;
            uiRootRect.offsetMin = Vector2.zero;
            uiRootRect.offsetMax = Vector2.zero;

            panelRoot = new GameObject("CrisisCardPanel");
            panelRoot.transform.SetParent(uiRoot.transform, false);

            var rect = panelRoot.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.78f);
            bg.raycastTarget = true;

            var box = new GameObject("Box");
            box.transform.SetParent(panelRoot.transform, false);
            var boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(680f, 440f);
            box.AddComponent<Image>().color = new Color(0.12f, 0.07f, 0.1f, 0.98f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(box.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-32f, 52f);
            titleRect.anchoredPosition = new Vector2(0f, -12f);
            titleText = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(titleText);
            titleText.fontSize = 24f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(box.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(24f, 112f);
            bodyRect.offsetMax = new Vector2(-24f, -72f);
            bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(bodyText);
            bodyText.fontSize = 16f;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.richText = true;

            var rowGo = new GameObject("ButtonRow");
            rowGo.transform.SetParent(box.transform, false);
            buttonRow = rowGo.transform;
            var rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.sizeDelta = new Vector2(-32f, 88f);
            rowRect.anchoredPosition = new Vector2(0f, 16f);

            var buttonLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10f;
            buttonLayout.padding = new RectOffset(4, 4, 4, 4);
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = true;

            panelRoot.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"CrisisCardPanel.BuildUI failed: {ex.Message}\n{ex.StackTrace}");
            GameUiRoot.InvalidateCache();
            TearDownUI();
        }
    }

    static void ApplyFont(TextMeshProUGUI tmp)
    {
        var existing = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        if (existing != null && existing.font != null)
            tmp.font = existing.font;
        else if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        tmp.color = new Color(0.92f, 0.9f, 0.85f);
        tmp.raycastTarget = false;
    }

    public bool Show(string title, string body, IReadOnlyList<CrisisCardChoice> choices)
    {
        if (choices == null || choices.Count == 0)
        {
            Debug.LogError("CrisisCardPanel.Show failed  -  no choices supplied.");
            return false;
        }

        for (int attempt = 0; attempt < 3 && !IsUiReady; attempt++)
        {
            if (attempt > 0)
                GameUiRoot.InvalidateCache();
            EnsureUI();
        }

        if (!IsUiReady)
        {
            Debug.LogError("CrisisCardPanel.Show failed  -  UI could not be built.");
            return false;
        }

        titleText.text = TmpTextSanitizer.Sanitize(title);
        bodyText.text = TmpTextSanitizer.Sanitize(body);
        ClearChoiceButtons();

        float buttonWidth = choices.Count switch
        {
            1 => 240f,
            2 => 220f,
            _ => 200f
        };

        for (int i = 0; i < choices.Count; i++)
            CreateChoiceButton(choices[i], buttonWidth);

        uiRoot.transform.SetAsLastSibling();
        panelRoot.transform.SetAsLastSibling();
        panelRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        return true;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void ClearChoiceButtons()
    {
        foreach (var btn in choiceButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        choiceButtons.Clear();
    }

    void CreateChoiceButton(CrisisCardChoice choice, float width)
    {
        var btnGo = new GameObject(choice.Label);
        btnGo.transform.SetParent(buttonRow, false);

        var layout = btnGo.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = 72f;
        layout.preferredHeight = 72f;
        layout.flexibleWidth = 1f;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.42f, 0.2f, 0.16f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            Dismiss();
            choice.OnSelect?.Invoke();
        });

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 6f);
        labelRect.offsetMax = new Vector2(-8f, -6f);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 13f;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.text = TmpTextSanitizer.Sanitize($"<b>{choice.Label}</b>\n<size=11>{choice.Description}</size>");
        tmp.raycastTarget = false;

        choiceButtons.Add(btnGo);
    }

    void ForceDismiss()
    {
        Dismiss();
        CrisisManager.Instance?.CancelPendingCardChoice();
    }

    void Dismiss()
    {
        Hide();
        CrisisManager.Instance?.NotifyCardDismissed();
    }
}
