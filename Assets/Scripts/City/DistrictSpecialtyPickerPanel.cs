using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Choose a district specialty when a hamlet is founded.</summary>
public class DistrictSpecialtyPickerPanel : MonoBehaviour
{
    public static DistrictSpecialtyPickerPanel Instance { get; private set; }

    GameObject panelRoot;
    City pendingDistrict;

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

        panelRoot = new GameObject("DistrictSpecialtyPickerPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panelRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        var box = new GameObject("Box");
        box.transform.SetParent(panelRoot.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(620f, 420f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        CreateLabel(box.transform, "Specialize this district", new Vector2(0f, -12f), 22f, FontStyles.Bold);
        var hintLabel = CreateLabel(box.transform,
            "Each hamlet serves one role for the parent city. Choose its specialty:",
            new Vector2(0f, -44f), 15f, FontStyles.Normal);
        hintLabel.name = "HintLabel";

        float y = -88f;
        foreach (var def in HamletSpecialtyDatabase.All)
        {
            CreateSpecialtyButton(box.transform, def, new Vector2(0f, y));
            y -= 78f;
        }
    }

    HamletSpecialty suggestedSpecialty = HamletSpecialty.None;

    public void Show(City district, HamletSpecialty suggested = HamletSpecialty.None)
    {
        if (district == null || !district.IsHamlet || district.HasChosenSpecialty)
            return;

        pendingDistrict = district;
        suggestedSpecialty = suggested;
        UpdateSuggestionHint();
        panelRoot.SetActive(true);
    }

    void UpdateSuggestionHint()
    {
        var hint = panelRoot.transform.Find("Box/HintLabel")?.GetComponent<TextMeshProUGUI>();
        if (hint == null) return;

        string baseText = "Each hamlet serves one role for the parent city. Choose its specialty:";
        if (suggestedSpecialty != HamletSpecialty.None)
            hint.text = TmpTextSanitizer.Sanitize(baseText + $"\n<color=#DDEE88>Recommended: {HamletSpecialtyDatabase.DisplayName(suggestedSpecialty)}</color>");
        else
            hint.text = TmpTextSanitizer.Sanitize(baseText);
    }

    void SelectSpecialty(HamletSpecialty specialty)
    {
        if (pendingDistrict == null)
            return;

        pendingDistrict.SetSpecialty(specialty);
        Debug.Log($"{pendingDistrict.SettlementDisplayName()} specialized as {HamletSpecialtyDatabase.DisplayName(specialty)}.");
        panelRoot.SetActive(false);
        pendingDistrict = null;
        CityScreenPanel.Instance?.Refresh();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    void CreateSpecialtyButton(Transform parent, HamletSpecialtyDefinition def, Vector2 pos)
    {
        var btnGo = new GameObject(def.Id.ToString());
        btnGo.transform.SetParent(parent, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(560f, 68f);
        rect.anchoredPosition = pos;

        var img = btnGo.AddComponent<Image>();
        img.color = SpecialtyColor(def.Id);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SelectSpecialty(def.Id));

        var label = CreateLabel(btnGo.transform, $"<b>{def.Name}</b>  -  {def.Subtitle}", Vector2.zero, 15f, FontStyles.Normal);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(12f, 22f);
        label.rectTransform.offsetMax = new Vector2(-12f, -4f);

        var sub = CreateLabel(btnGo.transform, def.Description, Vector2.zero, 12f, FontStyles.Italic);
        sub.color = new Color(0.82f, 0.86f, 0.9f);
        sub.rectTransform.anchorMin = Vector2.zero;
        sub.rectTransform.anchorMax = Vector2.one;
        sub.rectTransform.offsetMin = new Vector2(12f, 4f);
        sub.rectTransform.offsetMax = new Vector2(-12f, -24f);
    }

    static Color SpecialtyColor(HamletSpecialty id) => id switch
    {
        HamletSpecialty.Seminary => new Color(0.22f, 0.28f, 0.48f, 1f),
        HamletSpecialty.Garrison => new Color(0.42f, 0.22f, 0.22f, 1f),
        HamletSpecialty.Market => new Color(0.22f, 0.38f, 0.32f, 1f),
        HamletSpecialty.Scholastic => new Color(0.32f, 0.28f, 0.42f, 1f),
        _ => new Color(0.2f, 0.2f, 0.24f, 1f)
    };

    static TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 pos, float size, FontStyles style)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(560f, 28f);
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) tmp.font = existing.font;
        tmp.text = TmpTextSanitizer.Sanitize(text);
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.richText = true;
        return tmp;
    }
}
