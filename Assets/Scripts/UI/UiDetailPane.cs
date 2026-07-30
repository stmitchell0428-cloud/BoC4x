using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UiDetailPane
{
    public const float SidebarWidth = 288f;
    public const float BottomButtonAreaHeight = 96f;

    public static TextMeshProUGUI CreateSidebar(
        Transform panelRoot,
        out ScrollRect scrollRect,
        string placeholder,
        TMP_FontAsset font)
    {
        var sidebarGo = new GameObject("DetailSidebar");
        sidebarGo.transform.SetParent(panelRoot, false);

        var sidebarRect = sidebarGo.AddComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(1f, 0f);
        sidebarRect.anchorMax = new Vector2(1f, 1f);
        sidebarRect.pivot = new Vector2(1f, 0.5f);
        sidebarRect.offsetMin = new Vector2(-SidebarWidth - 8f, BottomButtonAreaHeight + 8f);
        sidebarRect.offsetMax = new Vector2(-8f, -44f);

        var bg = sidebarGo.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.11f, 0.98f);

        var scrollGo = new GameObject("Scroll");
        scrollGo.transform.SetParent(sidebarGo.transform, false);
        var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(8f, 8f);
        scrollRectTransform.offsetMax = new Vector2(-8f, -8f);

        // ScrollRect only receives wheel/drag when a Graphic can be raycast.
        var scrollHit = scrollGo.AddComponent<Image>();
        scrollHit.color = Color.clear;
        scrollHit.raycastTarget = true;

        scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 28f;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        var vpHit = viewport.AddComponent<Image>();
        vpHit.color = Color.clear;
        vpHit.raycastTarget = true;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var body = content.AddComponent<TextMeshProUGUI>();
        if (font != null) body.font = font;
        body.fontSize = 17f;
        body.lineSpacing = 4f;
        body.color = new Color(0.92f, 0.91f, 0.86f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.richText = true;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.raycastTarget = false;
        body.text = placeholder;

        scrollRect.viewport = vpRect;
        scrollRect.content = contentRect;

        SetDetailText(body, scrollRect, placeholder);
        return body;
    }

    public static void SetDetailText(TextMeshProUGUI body, ScrollRect scroll, string text)
    {
        if (body == null) return;

        body.text = SanitizeForFont(text ?? "");
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;

        var contentRect = body.rectTransform;
        float width = ResolveContentWidth(scroll, contentRect);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        body.ForceMeshUpdate(true);
        float preferredHeight = body.GetPreferredValues(body.text, width, 0f).y;
        if (preferredHeight < 1f)
            preferredHeight = body.preferredHeight;
        if (preferredHeight < 8f && !string.IsNullOrEmpty(body.text))
        {
            // Fallback when TMP font metrics are unavailable (e.g. EditMode without font asset).
            int lines = Mathf.Max(1, body.text.Split('\n').Length);
            float approxWrapped = body.text.Length / Mathf.Max(8f, width / (body.fontSize * 0.55f));
            preferredHeight = Mathf.Max(lines, approxWrapped) * (body.fontSize + body.lineSpacing) * 1.2f;
        }

        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(preferredHeight, 1f));
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.velocity = Vector2.zero;
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    static float ResolveContentWidth(ScrollRect scroll, RectTransform contentRect)
    {
        if (scroll != null && scroll.viewport != null)
        {
            float viewportWidth = scroll.viewport.rect.width;
            if (viewportWidth > 1f)
                return viewportWidth;
        }

        if (contentRect != null && contentRect.rect.width > 1f)
            return contentRect.rect.width;

        // Sidebar padding: 8px each side inside the scroll area.
        return Mathf.Max(8f, SidebarWidth - 32f);
    }

    /// <summary>Replace glyphs missing from the default TMP font (LiberationSans SDF).</summary>
    public static string SanitizeForFont(string text) => TmpTextSanitizer.Sanitize(text);

    public static Button CreateSidebarActionButton(
        Transform panelRoot,
        string name,
        string label,
        float yFromBottom,
        UnityEngine.Events.UnityAction onClick,
        TMP_FontAsset font,
        Color background)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(panelRoot, false);
        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-16f, yFromBottom);
        rect.sizeDelta = new Vector2(SidebarWidth - 16f, 36f);

        var img = btnGo.AddComponent<Image>();
        img.color = background;

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
        if (font != null) tmp.font = font;
        tmp.text = TmpTextSanitizer.Sanitize(label);
        tmp.fontSize = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }
}
