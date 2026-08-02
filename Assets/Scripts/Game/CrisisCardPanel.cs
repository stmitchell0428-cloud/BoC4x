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

    Canvas ownedCanvas;
    GameObject panelRoot;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    Transform buttonRow;
    readonly List<GameObject> choiceButtons = new();
    IChoiceCardPresenter activePresenter;

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

    bool IsUiReady
    {
        get
        {
            if (panelRoot == null)
                return false;

            RebindUiRefs();
            return titleText != null && bodyText != null && buttonRow != null;
        }
    }

    void EnsureUI()
    {
        if (IsUiReady)
            return;

        TearDownUI();
        BuildUI();
        RebindUiRefs();
        if (buttonRow == null)
            EnsureButtonRow();
    }

    void RebindUiRefs()
    {
        if (panelRoot == null)
            return;

        var box = panelRoot.transform.Find("Box");
        if (box == null)
            return;

        if (titleText == null)
            titleText = box.Find("Title")?.GetComponent<TextMeshProUGUI>();
        if (bodyText == null)
            bodyText = box.Find("Body")?.GetComponent<TextMeshProUGUI>();
        if (buttonRow == null)
            buttonRow = box.Find("ButtonRow");
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

        titleText = null;
        bodyText = null;
        buttonRow = null;
    }

    Canvas ResolveCanvas()
    {
        if (ownedCanvas != null)
            return ownedCanvas;

        GameUiRoot.EnsureEventSystem();
        ownedCanvas = GameUiRoot.GetModalCanvas();
        return ownedCanvas;
    }

    void BuildUI()
    {
        try
        {
            var canvas = ResolveCanvas();
            if (canvas == null)
            {
                Debug.LogError("CrisisCardPanel: could not resolve overlay canvas.");
                return;
            }

            panelRoot = new GameObject("CrisisCardPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvas.transform, false);

            var rect = panelRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            panelRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var box = new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(panelRoot.transform, false);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(680f, 440f);
            box.AddComponent<Image>().color = new Color(0.12f, 0.07f, 0.1f, 0.98f);

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(box.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
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

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(box.transform, false);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(24f, 112f);
            bodyRect.offsetMax = new Vector2(-24f, -72f);
            bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(bodyText);
        bodyText.fontSize = 16f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.richText = true;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Ellipsis;

            var rowGo = new GameObject("ButtonRow", typeof(RectTransform));
            rowGo.transform.SetParent(box.transform, false);
            buttonRow = rowGo.transform;
            var rowRect = rowGo.GetComponent<RectTransform>();
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
            TearDownUI();
        }
    }

    void EnsureButtonRow()
    {
        if (panelRoot == null)
            return;

        var box = panelRoot.transform.Find("Box");
        if (box == null)
            return;

        var existing = box.Find("ButtonRow");
        if (existing != null)
        {
            buttonRow = existing;
            return;
        }

        var rowGo = new GameObject("ButtonRow", typeof(RectTransform));
        rowGo.transform.SetParent(box, false);
        buttonRow = rowGo.transform;
        var rowRect = rowGo.GetComponent<RectTransform>();
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

    public bool Show(string title, string body, IReadOnlyList<CrisisCardChoice> choices, IChoiceCardPresenter presenter = null)
    {
        if (choices == null || choices.Count == 0)
        {
            Debug.LogError("CrisisCardPanel.Show failed  -  no choices supplied.");
            return false;
        }

        activePresenter = presenter ?? CrisisManager.Instance;

        for (int attempt = 0; attempt < 3 && !IsUiReady; attempt++)
            EnsureUI();

        if (!IsUiReady)
        {
            Debug.LogError(
                "CrisisCardPanel.Show failed  -  UI could not be built. " +
                $"panelRoot={panelRoot != null}, titleText={titleText != null}, " +
                $"bodyText={bodyText != null}, buttonRow={buttonRow != null}, canvas={ownedCanvas != null}");
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

        BringToFront();
        panelRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        return true;
    }

    public void BringToFront()
    {
        if (panelRoot != null)
            panelRoot.transform.SetAsLastSibling();
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
        if (buttonRow == null)
        {
            Debug.LogError("CrisisCardPanel.CreateChoiceButton failed  -  button row missing.");
            return;
        }

        var btnGo = new GameObject(SanitizeButtonName(choice.Label), typeof(RectTransform));
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
            choice.OnSelect?.Invoke();
            Dismiss();
        });

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
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

    static string SanitizeButtonName(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "Choice";

        var cleaned = label.Trim();
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            cleaned = cleaned.Replace(c, '_');

        return cleaned.Length > 32 ? cleaned.Substring(0, 32) : cleaned;
    }

    void ForceDismiss()
    {
        var presenter = activePresenter;
        activePresenter = null;
        Hide();
        presenter?.OnChoiceCardCancelled();
    }

    void Dismiss()
    {
        var presenter = activePresenter;
        activePresenter = null;
        Hide();
        presenter?.OnChoiceCardDismissed();
    }
}
