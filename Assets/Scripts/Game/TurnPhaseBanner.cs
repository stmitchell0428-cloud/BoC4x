using UnityEngine;
using TMPro;

public class TurnPhaseBanner : MonoBehaviour
{
    public static TurnPhaseBanner Instance { get; private set; }

    TextMeshProUGUI bannerText;

    void Awake()
    {
        Instance = this;
        BuildUI();
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted += OnTurnStarted;
    }

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TurnStarted -= OnTurnStarted;
            TurnManager.Instance.TurnStarted += OnTurnStarted;
        }
        Refresh();
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted -= OnTurnStarted;
        if (Instance == this)
            Instance = null;
    }

    void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("TurnPhaseBanner");
        go.transform.SetParent(canvas.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(900f, 36f);

        bannerText = go.AddComponent<TextMeshProUGUI>();
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) bannerText.font = existing.font;
        bannerText.fontSize = 20f;
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.richText = true;
        bannerText.color = new Color(0.95f, 0.92f, 0.82f);
        bannerText.raycastTarget = false;
    }

    void OnTurnStarted() => Refresh();

    public void Refresh(string extra = null)
    {
        if (bannerText == null || TurnManager.Instance == null) return;

        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
        {
            bannerText.text = TmpTextSanitizer.Sanitize("Match ended");
            return;
        }

        string phase;
        if (TurnManager.Instance.IsPlayerTurn)
        {
            phase = SchismManager.Instance != null && SchismManager.Instance.HasSchismed
                ? "<color=#6699EE><b>Your turn</b></color>  -  Lutheran Synod"
                : "<color=#6699EE><b>Your turn</b></color>  -  Lutheran Synod (united)";
        }
        else
        {
            var blocId = TurnManager.Instance.ActiveSchismaticBloc;
            string blocName = blocId != SchismaticBlocId.None && SchismaticBlocRegistry.Instance != null
                ? SchismaticBlocRegistry.Instance.ProfileForBloc(blocId).DisplayName
                : "Dissent";
            phase = $"<color=#EE7766><b>Schismatic turn</b></color>  -  {blocName}";
        }

        string progress = MatchController.Instance?.VictoryProgressLabel() ?? "";
        string artEra = ArtEraVisualController.FormatEraLabel();
        if (!string.IsNullOrEmpty(extra))
            bannerText.text = TmpTextSanitizer.Sanitize($"{phase}  |  {extra}");
        else if (!string.IsNullOrEmpty(progress))
            bannerText.text = TmpTextSanitizer.Sanitize($"{phase}  |  {progress}  |  {artEra}");
        else
            bannerText.text = TmpTextSanitizer.Sanitize($"{phase}  |  Turn {TurnManager.Instance.TurnNumber}  |  {artEra}");
    }
}
