using UnityEngine;
using TMPro;

public class TurnPhaseBanner : MonoBehaviour
{
    public static TurnPhaseBanner Instance { get; private set; }

    RectTransform bannerRect;
    TextMeshProUGUI bannerText;

    const float RightInset = 16f;
    const float BannerHeight = 52f;

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
        ApplyHudClearanceFromLayout();
        Refresh();
    }

    void ApplyHudClearanceFromLayout()
    {
        if (GameHUD.Instance != null)
            ApplyHudClearance(GameHUD.Instance.QueuePanelRightEdge + 8f, GameHUD.Instance.topPadding);
        else
            ApplyHudClearance(424f, 12f);
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
        bannerRect = go.AddComponent<RectTransform>();

        bannerText = go.AddComponent<TextMeshProUGUI>();
        var existing = FindAnyObjectByType<TextMeshProUGUI>();
        if (existing != null) bannerText.font = existing.font;
        bannerText.fontSize = 18f;
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.richText = true;
        bannerText.color = new Color(0.95f, 0.92f, 0.82f);
        bannerText.raycastTarget = false;
        bannerText.textWrappingMode = TextWrappingModes.Normal;
        bannerText.overflowMode = TextOverflowModes.Overflow;
    }

    /// <summary>Keep the banner in the top-center band, to the right of the queue panel.</summary>
    public void ApplyHudClearance(float leftInset, float topOffset = 8f)
    {
        if (bannerRect == null)
            return;

        bannerRect.anchorMin = new Vector2(0f, 1f);
        bannerRect.anchorMax = new Vector2(1f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.offsetMin = new Vector2(leftInset, -topOffset - BannerHeight);
        bannerRect.offsetMax = new Vector2(-RightInset, -topOffset);
    }

    void OnTurnStarted()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.IsPlayerTurn)
            Refresh(ActionQueueHud.FormatTurnBannerReminder());
        else
            Refresh();
    }

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
        string turnBit = $"Turn {TurnManager.Instance.TurnNumber}";
        string watch = ChurchYearFlavor.FormatWatchBannerLine();

        if (!string.IsNullOrEmpty(extra))
        {
            bannerText.text = TmpTextSanitizer.Sanitize(
                string.IsNullOrEmpty(watch)
                    ? $"{phase}  |  {turnBit}\n{extra}"
                    : $"{phase}  |  {turnBit}  |  {watch}\n{extra}");
        }
        else if (!string.IsNullOrEmpty(watch))
        {
            bannerText.text = TmpTextSanitizer.Sanitize(
                !string.IsNullOrEmpty(progress)
                    ? $"{phase}  |  {turnBit}  |  {watch}\n{progress}  |  {artEra}"
                    : $"{phase}  |  {turnBit}  |  {watch}  |  {artEra}");
        }
        else if (!string.IsNullOrEmpty(progress))
        {
            bannerText.text = TmpTextSanitizer.Sanitize($"{phase}  |  {turnBit}  |  {progress}  |  {artEra}");
        }
        else
        {
            bannerText.text = TmpTextSanitizer.Sanitize($"{phase}  |  {turnBit}  |  {artEra}");
        }
    }
}
