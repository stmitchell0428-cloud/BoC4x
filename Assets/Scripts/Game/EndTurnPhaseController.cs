using UnityEngine;

/// <summary>Runs player end-turn phases automatically: growth -> migration -> production -> confessional.</summary>
public class EndTurnPhaseController : MonoBehaviour
{
    public static EndTurnPhaseController Instance { get; private set; }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryBeginPhasedEndTurn()
    {
        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
            return false;

        if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
            return false;

        if (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible)
            return false;

        var faction = FirstSteps.Instance;
        if (faction == null)
            return false;

        faction.OnPlayerTurnEnded();
        TurnPhaseBanner.Instance?.Refresh();
        TurnManager.Instance?.EndTurn();
        return true;
    }
}
