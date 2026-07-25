using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Offer to found an organic district when Two Kingdoms balance draws settlers.</summary>
public class DistrictOfferPanel : MonoBehaviour
{
    public static DistrictOfferPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI bodyText;
    CityGrowthSystem.DistrictSiteOffer currentOffer;

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

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        panelRoot = new GameObject("DistrictOfferPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panelRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var box = new GameObject("Box");
        box.transform.SetParent(panelRoot.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(580f, 320f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

        CreateLabel(box.transform, "<color=#AADDFF><b>District forming</b></color>", new Vector2(0f, -12f), 22f, FontStyles.Bold);

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(box.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 1f);
        bodyRect.anchorMax = new Vector2(0.5f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.sizeDelta = new Vector2(540f, 150f);
        bodyRect.anchoredPosition = new Vector2(0f, -48f);
        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) bodyText.font = existing.font;
        bodyText.fontSize = 14f;
        bodyText.color = Color.white;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.richText = true;

        CreateButton(box.transform, "Accept district", new Vector2(-150f, 24f), () =>
        {
            CityGrowthManager.Instance?.AcceptPendingOffer();
            Hide();
        }, new Color(0.18f, 0.34f, 0.28f, 1f));

        CreateButton(box.transform, "Not now", new Vector2(0f, 24f), () =>
            CityGrowthManager.Instance?.DeferPendingOffer(), new Color(0.22f, 0.26f, 0.32f, 1f));

        CreateButton(box.transform, "Decline", new Vector2(150f, 24f), () =>
            CityGrowthManager.Instance?.DeclinePendingOffer(), new Color(0.34f, 0.2f, 0.2f, 1f));
    }

    public void Show(CityGrowthSystem.DistrictSiteOffer offer)
    {
        currentOffer = offer;
        string terrain = "land";
        if (HexGridMap.Instance != null && HexGridMap.Instance.TryGetTile(offer.Hex, out var tile))
            terrain = HexGridMap.TerrainDisplayName(tile.Terrain);

        string spec = HamletSpecialtyDatabase.DisplayName(offer.SuggestedSpecialty);
        bodyText.text = TmpTextSanitizer.Sanitize(
            $"{offer.FlavorReason}\n\n" +
            $"<b>{offer.Parent.CityName}</b>  -  new district on {terrain} hex {offer.Hex}\n" +
            $"Suggested specialty: <color=#DDEE88>{spec}</color>\n" +
            "<size=12><color=#99AABB>Accept to found the district (you may confirm or change specialty). Colonists can still force a district elsewhere.</color></size>");

        panelRoot.SetActive(true);
        CameraFollow.Instance?.PanToHex(offer.Hex, 6f);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick, Color color)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(140f, 36f);
        rect.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var tmp = CreateLabel(go.transform, label, Vector2.zero, 13f, FontStyles.Normal);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.anchorMin = Vector2.zero;
        tmp.rectTransform.anchorMax = Vector2.one;
        tmp.rectTransform.offsetMin = Vector2.zero;
        tmp.rectTransform.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 pos, float size, FontStyles style)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(540f, 28f);
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
