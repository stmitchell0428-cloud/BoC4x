using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>War and truce with rival Lutheran synods (player vs AI lobby slots).</summary>
public class DiplomacyPanel : MonoBehaviour
{
    public static DiplomacyPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI bodyText;
    Transform rowContainer;
    readonly List<GameObject> rowObjects = new();

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        Instance = this;
        BuildUI();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsVisible || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            Hide();
    }

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        panelRoot = new GameObject("DiplomacyPanel");
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
        boxRect.sizeDelta = new Vector2(560f, 420f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

        CreateLabel(box.transform, "Title", "<color=#AADDFF><b>Synod diplomacy</b></color>", new Vector2(0f, -10f), 22f, FontStyles.Bold);
        CreateLabel(
            box.transform,
            "Subtitle",
            "Rival Lutheran synods may truce or war. Schismatic blocs are always hostile.",
            new Vector2(0f, -38f),
            14f,
            FontStyles.Normal);

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(box.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 1f);
        bodyRect.anchorMax = new Vector2(0.5f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.sizeDelta = new Vector2(520f, 48f);
        bodyRect.anchoredPosition = new Vector2(0f, -62f);
        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        CopyFont(bodyText);
        bodyText.fontSize = 13f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.richText = true;
        bodyText.color = new Color(0.85f, 0.9f, 0.95f);

        var rowsGo = new GameObject("Rows");
        rowsGo.transform.SetParent(box.transform, false);
        var rowsRect = rowsGo.AddComponent<RectTransform>();
        rowsRect.anchorMin = new Vector2(0.5f, 1f);
        rowsRect.anchorMax = new Vector2(0.5f, 1f);
        rowsRect.pivot = new Vector2(0.5f, 1f);
        rowsRect.sizeDelta = new Vector2(520f, 240f);
        rowsRect.anchoredPosition = new Vector2(0f, -118f);
        rowContainer = rowsGo.transform;

        CreateButton(box.transform, "Close (D / Esc)", new Vector2(0f, 24f), new Color(0.22f, 0.26f, 0.32f, 1f), Hide);
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
        if (SynodDiplomacyManager.Instance == null || !SynodDiplomacyManager.Instance.HasRivals)
        {
            Debug.Log("Diplomacy unavailable — start a lobby match with 2+ players for rival synods.");
            return;
        }

        Refresh();
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Refresh()
    {
        var diplomacy = SynodDiplomacyManager.Instance;
        if (diplomacy == null)
            return;

        bodyText.text = TmpTextSanitizer.Sanitize(
            $"<b>Truce offer:</b> {SynodDiplomacyManager.TruceManuscriptCost} manuscripts, " +
            $"{SynodDiplomacyManager.TruceDurationTurns} turns without synod-vs-synod combat.\n" +
            "<size=12><color=#99AABB>Declare War ends a truce immediately. Schismatic factions ignore diplomacy.</color></size>");

        foreach (var row in rowObjects)
        {
            if (row != null)
                Destroy(row);
        }
        rowObjects.Clear();

        int index = 0;
        foreach (var rivalId in diplomacy.ActiveRivals)
        {
            BuildRivalRow(rivalId, index);
            index++;
        }
    }

    void BuildRivalRow(SynodPlayerId rivalId, int index)
    {
        var diplomacy = SynodDiplomacyManager.Instance;
        float y = -index * 72f;

        var row = new GameObject($"Rival_{rivalId}");
        row.transform.SetParent(rowContainer, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(520f, 64f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowObjects.Add(row);

        row.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(row.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(250f, 52f);
        labelRect.anchoredPosition = new Vector2(12f, 0f);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        CopyFont(label);
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.richText = true;
        label.text = TmpTextSanitizer.Sanitize(
            $"<b>{SynodPlayerDatabase.DisplayName(rivalId)}</b>\n{diplomacy.FormatStatusLabel(rivalId)}");

        if (!diplomacy.IsTruceActive(rivalId))
        {
            CreateButton(row.transform, "Propose truce", new Vector2(300f, 0f), new Color(0.18f, 0.34f, 0.28f, 1f), () =>
            {
                diplomacy.TryProposeTruce(rivalId);
                Refresh();
            });
        }

        CreateButton(row.transform, "Declare war", new Vector2(420f, 0f), new Color(0.38f, 0.2f, 0.2f, 1f), () =>
        {
            diplomacy.DeclareWar(rivalId);
            Refresh();
        });
    }

    static void CreateLabel(Transform parent, string name, string text, Vector2 pos, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(520f, 28f);
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.richText = true;
        tmp.color = Color.white;
        tmp.text = TmpTextSanitizer.Sanitize(text);
    }

    static void CreateButton(Transform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(108f, 34f);
        rect.anchoredPosition = pos;

        go.AddComponent<Image>().color = color;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = 11f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;
    }

    static void CopyFont(TextMeshProUGUI target)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null)
            target.font = existing.font;
    }
}
