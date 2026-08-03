using UnityEngine;

/// <summary>Drives era-evolving visuals when confession tech tiers advance (Decision 23).</summary>
public class ArtEraVisualController : MonoBehaviour
{
    public static ArtEraVisualController Instance { get; private set; }

    VisualArtEra currentEra = VisualArtEra.WoodcutPaper;
    string lastTransitionMessage;

    public static VisualArtEra CurrentEra =>
        Instance != null ? Instance.currentEra : VisualArtEra.WoodcutPaper;

    public string LastTransitionMessage => lastTransitionMessage;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ConfessionResearchManager.Instance != null)
            ConfessionResearchManager.Instance.ResearchChanged += OnResearchChanged;

        if (MatchLobbyController.Instance != null)
            MatchLobbyController.Instance.MatchStartedEvent += OnMatchStarted;

        ResolveEra(forceRefresh: true);
    }

    void OnDestroy()
    {
        if (ConfessionResearchManager.Instance != null)
            ConfessionResearchManager.Instance.ResearchChanged -= OnResearchChanged;

        if (MatchLobbyController.Instance != null)
            MatchLobbyController.Instance.MatchStartedEvent -= OnMatchStarted;

        if (Instance == this)
            Instance = null;
    }

    void OnMatchStarted() => ResolveEra(forceRefresh: true);

    void OnResearchChanged() => ResolveEra(forceRefresh: false);

    void ResolveEra(bool forceRefresh)
    {
        int tier = ConfessionResearchManager.Instance != null
            ? ConfessionResearchManager.Instance.GetHighestUnlockedTier()
            : 1;

        if (MatchNarrativeChronology.Instance != null &&
            MatchNarrativeChronology.Instance.Phase == NarrativeChronologyPhase.SalvationHistory)
            tier = Mathf.Min(tier, 2);

        var nextEra = VisualArtEraResolver.FromTier(tier);

        if (!forceRefresh && nextEra == currentEra)
            return;

        bool transitioned = nextEra != currentEra;
        currentEra = nextEra;

        if (transitioned)
        {
            lastTransitionMessage = $"Visual era: {VisualArtEraResolver.DisplayName(currentEra)}";
            Debug.Log($"Book of Concord: {lastTransitionMessage} (tier {tier}).");
        }
        else
        {
            lastTransitionMessage = null;
        }

        ArtEraSpriteFactory.ClearCache();
        ApplyGlobalVisuals();

        if (transitioned)
        {
            TurnPhaseBanner.Instance?.Refresh(lastTransitionMessage);
            FirstSteps.Instance?.RefreshDashboard();
            // Skip match-start force refresh noise; only announce real flips mid-match.
            if (!forceRefresh)
                ArtEraTransitionPanel.Instance?.Show(currentEra);
        }
    }

    public void ApplyGlobalVisuals()
    {
        if (Camera.main != null)
            Camera.main.backgroundColor = ArtEraPalette.CameraBackground(currentEra);

        if (HexGridMap.Instance != null)
        {
            foreach (var tile in HexGridMap.Instance.AllTiles)
                tile.RefreshArtEraVisuals();
        }

        foreach (var unit in FindObjectsByType<Unit>())
            unit.ApplyArtEraVisuals();

        foreach (var city in FindObjectsByType<City>())
            city.RefreshAppearance();
    }

    public static string FormatEraLabel()
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(ArtEraPalette.UiAccent(CurrentEra))}>" +
               $"Art: {VisualArtEraResolver.DisplayName(CurrentEra)}</color>";
    }
}
