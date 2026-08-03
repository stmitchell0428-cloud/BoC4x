using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>Clergy roster management  -  view slots, reassign installed clergy, chaplain assignments (R key).</summary>
public class ClergyRosterPanel : MonoBehaviour
{
    public static ClergyRosterPanel Instance { get; private set; }

    GameObject panelRoot;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    City viewedRoot;

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
        if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame)
            return;
        if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen)
            return;
        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen)
            return;
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return;

        Toggle();
    }

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        panelRoot = new GameObject("ClergyRosterPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var rect = panelRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        var box = new GameObject("Box");
        box.transform.SetParent(panelRoot.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(560f, 520f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        titleText = CreateLabel(box.transform, "Title", "Clergy roster", new Vector2(0f, -12f), 22f, FontStyles.Bold);
        bodyText = CreateLabel(box.transform, "Body", "", new Vector2(0f, -52f), 14f, FontStyles.Normal);
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 300f);

        CreateButton(box.transform, "Assign selected here", new Vector2(-170f, 56f), AssignSelected);
        CreateButton(box.transform, "Parish ministry", new Vector2(170f, 56f), AssignChaplainParish);
        CreateButton(box.transform, "Escort selected unit", new Vector2(-170f, 12f), AssignChaplainEscort);
        CreateButton(box.transform, "Hospital ministry", new Vector2(170f, 12f), AssignChaplainHospital);
        CreateButton(box.transform, "Close", new Vector2(0f, -32f), Hide);
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Vector2 pos, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(520f, 300f);
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        CopyFont(tmp);
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        tmp.text = TmpTextSanitizer.Sanitize(text);
        return tmp;
    }

    static void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
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
        img.color = new Color(0.22f, 0.34f, 0.48f, 1f);
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
        tmp.fontSize = 13f;
        tmp.text = TmpTextSanitizer.Sanitize(label);
    }

    static void CopyFont(TextMeshProUGUI tmp)
    {
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) tmp.font = existing.font;
        tmp.color = new Color(0.92f, 0.9f, 0.85f);
    }

    City ResolveViewRoot()
    {
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected != null && selected.Faction == FactionId.LutheranSynod)
        {
            var near = CityManager.Instance?.GetNearestPlayerCity(selected.HexPosition);
            if (near != null)
                return ClergyRoster.GetControllingRoot(near);
        }

        var primary = CityManager.Instance?.GetPrimaryPlayerCity();
        return primary != null ? ClergyRoster.GetControllingRoot(primary) : null;
    }

    Unit SelectedChaplain()
    {
        var unit = TurnManager.Instance?.SelectedUnit;
        return unit != null && unit.IsAlive && unit.Type == UnitType.Chaplain ? unit : null;
    }

    public void Toggle()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        viewedRoot = ResolveViewRoot();
        if (viewedRoot == null)
        {
            Debug.Log("Clergy roster: found no synod city cluster.");
            return;
        }

        RefreshBody();

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    void RefreshBody()
    {
        if (titleText != null && viewedRoot != null)
            titleText.text = TmpTextSanitizer.Sanitize($"Clergy  -  {viewedRoot.CityName}");
        if (bodyText != null && viewedRoot != null)
        {
            var detail = ClergyRoster.FormatRosterDetail(viewedRoot);
            var chaplain = SelectedChaplain();
            if (chaplain != null)
                detail += $"\n\n<size=13><i>Selected chaplain:</i> {ChaplainSpecialty.FormatAssignment(chaplain)}</size>";
            bodyText.text = TmpTextSanitizer.Sanitize(detail);
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void AssignSelected()
    {
        if (viewedRoot == null)
            viewedRoot = ResolveViewRoot();

        var unit = TurnManager.Instance?.SelectedUnit;
        if (unit == null || viewedRoot == null)
        {
            Debug.Log("Select an installed clergy unit, then assign to this cluster.");
            return;
        }

        if (ClergyRoster.TryReassign(unit, viewedRoot))
            OnRosterChanged();
    }

    void AssignChaplainParish()
    {
        var chaplain = SelectedChaplain();
        if (chaplain == null)
        {
            Debug.Log("Select a chaplain to set parish ministry.");
            return;
        }

        if (ChaplainSpecialty.TryAssignParish(chaplain))
            OnRosterChanged();
    }

    void AssignChaplainEscort()
    {
        var chaplain = SelectedChaplain();
        if (chaplain == null)
        {
            Debug.Log("Select a chaplain, then a soldier/slinger/archer/horseman/defender to escort.");
            return;
        }

        var escort = FindEscortCandidate();
        if (escort == null)
        {
            Debug.Log("Select a military unit (same turn) to link as escort.");
            return;
        }

        if (ChaplainSpecialty.TryAssignEscort(chaplain, escort))
            OnRosterChanged();
    }

    void AssignChaplainHospital()
    {
        var chaplain = SelectedChaplain();
        if (chaplain == null)
        {
            Debug.Log("Select a chaplain to install at the hospital.");
            return;
        }

        if (viewedRoot == null)
            viewedRoot = ResolveViewRoot();
        if (viewedRoot == null)
            return;

        if (ChaplainSpecialty.TryAssignHospital(chaplain, viewedRoot))
            OnRosterChanged();
    }

    Unit FindEscortCandidate()
    {
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected != null && ChaplainSpecialty.IsMilitaryUnit(selected))
            return selected;

        if (TurnManager.Instance == null)
            return null;

        foreach (var unit in TurnManager.Instance.GetUnits(FactionId.LutheranSynod))
        {
            if (ChaplainSpecialty.IsMilitaryUnit(unit))
                return unit;
        }

        return null;
    }

    void OnRosterChanged()
    {
        RefreshBody();
        CityScreenPanel.Instance?.RefreshIfOpen();
        FirstSteps.Instance?.RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
    }
}
