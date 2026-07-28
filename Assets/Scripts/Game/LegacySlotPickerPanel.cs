using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Pick which legacy trait to replace when all 3 slots are full.</summary>
public class LegacySlotPickerPanel : MonoBehaviour
{
    public static LegacySlotPickerPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI bodyText;
    SynodLegacyTraitId pendingTrait;
    readonly List<GameObject> dynamicButtons = new();

    void Awake() => Instance = this;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Start()
    {
        EnsureUI();
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureUI()
    {
        if (panelRoot != null && bodyText != null)
            return;

        var canvas = GameUiRoot.GetModalCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("LegacySlotPickerPanel: no canvas available.");
            return;
        }

        BuildUI(canvas);
    }

    void BuildUI(Canvas canvas)
    {
        panelRoot = new GameObject("LegacySlotPickerPanel");
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
        boxRect.sizeDelta = new Vector2(560f, 460f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(box.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-32f, 48f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        CopyFont(title);
        title.fontSize = 22f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.text = "Legacy trait slot full";

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(box.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(24f, 72f);
        bodyRect.offsetMax = new Vector2(-24f, -64f);
        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        CopyFont(bodyText);
        bodyText.fontSize = 15f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.richText = true;

        CreateStaticButton(box.transform, "Keep current traits", new Vector2(0f, 20f), DeclineNewTrait);
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) tmp.font = existing.font;
        tmp.color = new Color(0.92f, 0.9f, 0.85f);
        tmp.raycastTarget = false;
    }

    void CreateStaticButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(label);
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(200f, 36f);
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
        tmp.text = TmpTextSanitizer.Sanitize(label);
        tmp.raycastTarget = false;
    }

    public void Show(SynodLegacyTraitId newTrait)
    {
        EnsureUI();
        if (panelRoot == null || bodyText == null)
        {
            Debug.LogWarning("LegacySlotPickerPanel.Show failed — UI could not be built.");
            return;
        }

        pendingTrait = newTrait;

        ClearDynamicButtons();
        bodyText.text = TmpTextSanitizer.Sanitize(
            $"<b>New trait earned</b>\n{SynodLegacyTraitDatabase.FormatDetailBlock(newTrait)}\n\n" +
            "<b>Active slots</b>  -  choose one to replace (3 max):\n");

        var box = panelRoot.transform.Find("Box");
        if (box == null) return;

        var active = SynodLegacyManager.Instance?.ActiveSlots;
        int index = 0;
        foreach (var id in active ?? System.Array.Empty<SynodLegacyTraitId>())
        {
            AppendActiveSlotSummary(id);
            CreateSlotButton(box, id, 72f + index * 52f);
            index++;
        }

        panelRoot.SetActive(true);
        TurnPhaseBanner.Instance?.Refresh("<color=#DDCC88><b>Legacy slot full</b></color>  -  replace a trait or keep current");
    }

    void CreateSlotButton(Transform box, SynodLegacyTraitId replaceId, float y)
    {
        var btnGo = new GameObject($"Replace_{replaceId}");
        btnGo.transform.SetParent(box, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(500f, 48f);
        rect.anchoredPosition = new Vector2(0f, y);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.35f, 0.22f, 0.18f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => ConfirmReplace(replaceId));

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 13f;
        tmp.text = TmpTextSanitizer.Sanitize(
            $"Replace {SynodLegacyTraitDatabase.DisplayName(replaceId)}\n" +
            $"<size=11><color=#BBDDAA>{SynodLegacyTraitDatabase.FormatGameplayEffects(replaceId)}</color></size>");
        tmp.raycastTarget = false;

        dynamicButtons.Add(btnGo);
    }

    void AppendActiveSlotSummary(SynodLegacyTraitId id)
    {
        bodyText.text += TmpTextSanitizer.Sanitize(
            $"\n<size=12>• {SynodLegacyTraitDatabase.FormatCompactLabel(id)}</size>");
    }

    void ConfirmReplace(SynodLegacyTraitId replaceId)
    {
        SynodLegacyManager.Instance?.ReplaceActiveSlot(replaceId, pendingTrait);
        panelRoot.SetActive(false);
        ClearDynamicButtons();
    }

    void DeclineNewTrait()
    {
        panelRoot.SetActive(false);
        ClearDynamicButtons();
    }

    void ClearDynamicButtons()
    {
        foreach (var btn in dynamicButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        dynamicButtons.Clear();
    }
}
