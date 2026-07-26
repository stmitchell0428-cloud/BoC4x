using UnityEngine;

/// <summary>Runs player end-turn phases automatically: growth -> migration -> production -> confessional.</summary>
public class EndTurnPhaseController : MonoBehaviour
{
    public static EndTurnPhaseController Instance { get; private set; }

    bool playerEndPhasesRanThisTurn;

    void Awake() => Instance = this;

    void OnEnable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted += OnTurnStarted;
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted -= OnTurnStarted;

        if (Instance == this)
            Instance = null;
    }

    void OnTurnStarted()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.IsPlayerTurn)
            playerEndPhasesRanThisTurn = false;
    }

    public bool TryBeginPhasedEndTurn()
    {
        if (BlocksEndTurn())
            return false;

        var faction = FirstSteps.Instance;
        if (faction == null)
            return false;

        if (!playerEndPhasesRanThisTurn)
        {
            faction.OnPlayerTurnEnded();
            TurnPhaseBanner.Instance?.Refresh();
            playerEndPhasesRanThisTurn = true;
        }
        else
            CrisisManager.Instance?.EnsurePendingCrisisCardVisible();

        if (BlocksEndTurn())
            return false;

        var tm = TurnManager.Instance;
        if (tm == null)
            return false;

        int turnBefore = tm.TurnNumber;
        tm.EndTurn();

        if (tm.IsPlayerTurn && tm.TurnNumber == turnBefore)
            return false;

        playerEndPhasesRanThisTurn = false;
        return true;
    }

    static bool BlocksEndTurn()
    {
        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
            return true;
        if (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible)
            return true;
        return false;
    }
}
