using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Info card when Decision 23 visual art era flips (woodcut → stained glass → modern).</summary>
public class ArtEraTransitionPanel : MonoBehaviour
{
    public static ArtEraTransitionPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    Image illustrationImage;
    Texture2D illustrationTex;

    void Awake()
    {
        Instance = this;
        BuildUI();
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (illustrationTex != null)
            Destroy(illustrationTex);
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Dismiss();
    }

    void BuildUI()
    {
        var canvas = GameUiRoot.GetModalCanvas();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        panelRoot = new GameObject("ArtEraTransitionPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panelRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        var box = new GameObject("Box");
        box.transform.SetParent(panelRoot.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(620f, 420f);
        box.AddComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.98f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(box.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-32f, 48f);
        titleRect.anchoredPosition = new Vector2(0f, -14f);
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        CopyFont(titleText);
        titleText.fontSize = 24f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        var artGo = new GameObject("Illustration");
        artGo.transform.SetParent(box.transform, false);
        var artRect = artGo.AddComponent<RectTransform>();
        artRect.anchorMin = new Vector2(0.5f, 1f);
        artRect.anchorMax = new Vector2(0.5f, 1f);
        artRect.pivot = new Vector2(0.5f, 1f);
        artRect.sizeDelta = new Vector2(220f, 120f);
        artRect.anchoredPosition = new Vector2(0f, -72f);
        illustrationImage = artGo.AddComponent<Image>();
        illustrationImage.color = Color.white;
        illustrationImage.preserveAspect = true;

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(box.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(28f, 64f);
        bodyRect.offsetMax = new Vector2(-28f, -210f);
        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        CopyFont(bodyText);
        bodyText.fontSize = 16f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.richText = true;

        CreateButton(box.transform, "Continue", new Vector2(0f, 22f), Dismiss);
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null)
            tmp.font = existing.font;
        tmp.color = new Color(0.92f, 0.9f, 0.85f);
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
        rect.sizeDelta = new Vector2(180f, 40f);
        rect.anchoredPosition = pos;

        var img = btnGo.AddComponent<Image>();
        img.color = ArtEraPalette.UiAccent(ArtEraVisualController.CurrentEra);
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
        tmp.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        tmp.text = TmpTextSanitizer.Sanitize(label);
        tmp.raycastTarget = false;
    }

    public void Show(VisualArtEra era)
    {
        if (panelRoot == null || titleText == null || bodyText == null)
            return;

        string accent = ColorUtility.ToHtmlStringRGB(ArtEraPalette.UiAccent(era));
        titleText.text = TmpTextSanitizer.Sanitize(
            $"<color=#{accent}>Visual era  -  {VisualArtEraResolver.DisplayName(era)}</color>");
        bodyText.text = TmpTextSanitizer.Sanitize(BodyFor(era));
        ApplyIllustration(era);
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }

    static string BodyFor(VisualArtEra era) => era switch
    {
        VisualArtEra.WoodcutPaper =>
            "<b>Woodcut & paper</b>\n\n" +
            "The synod's early confession is cut in bold lines on warm paper. " +
            "Terrain and units take a parchment tint  -  readable, humble, and ready for the press.\n\n" +
            "<color=#AABBCC>Map colors will shift again as higher confession tiers unlock stained glass, then modern confession.</color>",
        VisualArtEra.StainedGlass =>
            "<b>Stained glass</b>\n\n" +
            "Light through confession: jewels of color in the parish windows. " +
            "The map grows more saturated  -  water deeper blue, land more vivid  -  as doctrine and hymnody take form.\n\n" +
            "<color=#AABBCC>This is intentional art direction (Decision 23), not fog or a bug.</color>",
        VisualArtEra.Modern =>
            "<b>Modern confession</b>\n\n" +
            "Confessional Art and late civic science cool the palette. " +
            "Terrain softens slightly  -  clearer, cleaner lines for a synod that has entered the modern age.\n\n" +
            "<color=#AABBCC>If the map looks muted after unlocking Confessional Art, that is this era change.</color>",
        _ => VisualArtEraResolver.DisplayName(era)
    };

    void ApplyIllustration(VisualArtEra era)
    {
        if (illustrationImage == null)
            return;

        if (illustrationTex != null)
            Destroy(illustrationTex);

        illustrationTex = BuildIllustration(era);
        illustrationImage.sprite = Sprite.Create(
            illustrationTex,
            new Rect(0, 0, illustrationTex.width, illustrationTex.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    static Texture2D BuildIllustration(VisualArtEra era)
    {
        const int w = 220;
        const int h = 120;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = era == VisualArtEra.Modern ? FilterMode.Bilinear : FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color bg = ArtEraPalette.CameraBackground(era);
        Color accent = ArtEraPalette.UiAccent(era);
        Color land = ArtEraPalette.TerrainColor(TerrainType.Pasture, era);
        Color water = ArtEraPalette.TerrainColor(TerrainType.Ocean, era);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)(w - 1);
                float ny = y / (float)(h - 1);
                Color c = Color.Lerp(bg, land, 0.55f + 0.2f * ny);

                // Simple horizon water band.
                if (ny < 0.38f)
                    c = Color.Lerp(water, bg, 0.25f);

                // Era glyph: cross / diamond / soft circle.
                float cx = 0.5f;
                float cy = 0.58f;
                float dx = nx - cx;
                float dy = ny - cy;
                bool mark = era switch
                {
                    VisualArtEra.WoodcutPaper =>
                        (Mathf.Abs(dx) < 0.035f && Mathf.Abs(dy) < 0.28f) ||
                        (Mathf.Abs(dy) < 0.035f && Mathf.Abs(dx) < 0.18f),
                    VisualArtEra.StainedGlass =>
                        Mathf.Abs(dx) + Mathf.Abs(dy) < 0.22f && Mathf.Abs(dx) + Mathf.Abs(dy) > 0.08f,
                    _ => dx * dx + dy * dy < 0.045f
                };
                if (mark)
                    c = Color.Lerp(c, accent, 0.85f);

                // Woodcut edge hatch.
                if (era == VisualArtEra.WoodcutPaper && ((x + y) % 7 == 0) && ny > 0.4f)
                    c = Color.Lerp(c, new Color(0.2f, 0.16f, 0.12f), 0.35f);

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }

    void Dismiss()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
