using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Modal shown when the synod splits  -  reason, heresy flavor, map ping.</summary>
public class SchismEventPanel : MonoBehaviour
{
    public static SchismEventPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;

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

        panelRoot = new GameObject("SchismEventPanel");
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
        boxRect.sizeDelta = new Vector2(560f, 340f);
        box.AddComponent<Image>().color = new Color(0.14f, 0.08f, 0.08f, 0.98f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(box.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-32f, 52f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        CopyFont(titleText);
        titleText.fontSize = 26f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(box.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(24f, 64f);
        bodyRect.offsetMax = new Vector2(-24f, -72f);
        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        CopyFont(bodyText);
        bodyText.fontSize = 17f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.richText = true;

        CreateButton(box.transform, "Continue", new Vector2(0f, 24f), Dismiss);
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) tmp.font = existing.font;
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
        img.color = new Color(0.45f, 0.22f, 0.18f, 1f);
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

    public void Show(SchismRecord record, string reason)
    {
        if (bodyText == null) return;

        var profile = record.Profile;
        titleText.text = TmpTextSanitizer.Sanitize($"<color=#EE7766>Schism  -  {profile.DisplayName}</color>");

        string axisNote = profile.AxisTweak switch
        {
            HeresyAxisTweak.Appeal => "This dissent draws migrants through gospel appeal  -  comfort over confession.",
            HeresyAxisTweak.Restraint => "This dissent enforces rigid civic restraint  -  law without gospel comfort.",
            HeresyAxisTweak.Doctrine => "This dissent challenges confessional doctrine  -  adherence fractures first.",
            _ => ""
        };

        int schismNum = SchismManager.Instance?.SchismCount ?? 1;
        string repeatNote = schismNum > 1
            ? $"\n\n<color=#FFCC88>This is schism #{schismNum}  -  up to 3 dissent blocs may coexist.</color>"
            : "";

        bodyText.text = TmpTextSanitizer.Sanitize(
            $"<b>{reason}</b>\n\n" +
            $"A dissenting party has withdrawn and founded <color=#EE7766><b>{profile.CapitalSuffix}</b></color> " +
            $"with soldiers and missionaries. {axisNote}\n\n" +
            "<color=#FFDD88>Schismatic turns now alternate with yours.</color>" +
            repeatNote);

        panelRoot.SetActive(true);

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.PanToHex(record.CapitalHex, holdSeconds: 5f);
    }

    public void Show(string reason, HexCoordinates dissentHex)
    {
        var record = new SchismRecord(
            SchismaticBlocId.Bloc1,
            HeresyType.DoctrinalDrift,
            reason,
            dissentHex,
            TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1);
        Show(record, reason);
    }

    void Dismiss()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        CameraFollow.Instance?.RecenterOnActiveUnit();
    }
}
