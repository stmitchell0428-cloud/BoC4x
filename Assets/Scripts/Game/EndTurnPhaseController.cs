using System.Collections;
using UnityEngine;

/// <summary>Runs player end-turn phases automatically: growth -> migration -> production -> confessional.</summary>
public class EndTurnPhaseController : MonoBehaviour
{
    public static EndTurnPhaseController Instance { get; private set; }

    bool playerEndPhasesRanThisTurn;

    void Awake() => Instance = this;

    void OnEnable()
    {
        TrySubscribeTurnStarted();
    }

    void Start()
    {
        TrySubscribeTurnStarted();
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted -= OnTurnStarted;

        if (Instance == this)
            Instance = null;
    }

    void TrySubscribeTurnStarted()
    {
        if (TurnManager.Instance == null)
            return;

        TurnManager.Instance.TurnStarted -= OnTurnStarted;
        TurnManager.Instance.TurnStarted += OnTurnStarted;
    }

    void OnTurnStarted()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.IsPlayerTurn)
            playerEndPhasesRanThisTurn = false;

        StartCoroutine(ProcessChoiceCardsNextFrame());
    }

    static IEnumerator ProcessChoiceCardsNextFrame()
    {
        yield return null;
        ChoiceCardQueue.ProcessTurnStart();
    }

    public bool TryBeginPhasedEndTurn()
    {
        SanitizeStaleChoicePanels();

        if (TryGetEndTurnBlockReason(out string blockReason))
        {
            // Brief / other modals can sit above the choice card; clear them and re-show.
            SynodBriefPanel.Instance?.Hide();
            CrisisManager.Instance?.EnsurePendingCrisisCardVisible();
            PastoralBriefingManager.Instance?.EnsurePendingBriefingVisible();
            NarrativeEventManager.Instance?.EnsurePendingEventVisible();
            LiturgicalEventManager.Instance?.EnsurePendingEventVisible();
            TestimonyColloquyManager.Instance?.EnsurePendingColloquyVisible();
            SynodicalEmphasisManager.Instance?.EnsurePendingChoiceVisible();
            Tier2EmphasisManager.Instance?.EnsurePendingChoicesVisible();
            CrisisCardPanel.Instance?.BringToFront();
            TurnPhaseBanner.Instance?.Refresh(blockReason);
            Debug.LogWarning($"End Turn blocked: {StripRichText(blockReason)}");
            return false;
        }

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

        SynodicalEmphasisManager.Instance?.EnsurePendingChoiceVisible();
        Tier2EmphasisManager.Instance?.EnsurePendingChoicesVisible();
        CityGrowthManager.Instance?.EnsurePendingDistrictOfferVisible();

        // Pastoral briefings auto-defer; district offers block until Accept / Not now / Decline.
        if (EndTurnDeferHelper.HasAutoDeferChoicePending)
            EndTurnDeferHelper.DeferPendingChoices();

        SanitizeStaleChoicePanels();

        if (TryGetEndTurnBlockReason(out string blockReasonAfterPhases))
        {
            SynodBriefPanel.Instance?.Hide();
            CrisisManager.Instance?.EnsurePendingCrisisCardVisible();
            PastoralBriefingManager.Instance?.EnsurePendingBriefingVisible();
            NarrativeEventManager.Instance?.EnsurePendingEventVisible();
            LiturgicalEventManager.Instance?.EnsurePendingEventVisible();
            TestimonyColloquyManager.Instance?.EnsurePendingColloquyVisible();
            SynodicalEmphasisManager.Instance?.EnsurePendingChoiceVisible();
            Tier2EmphasisManager.Instance?.EnsurePendingChoicesVisible();
            CityGrowthManager.Instance?.EnsurePendingDistrictOfferVisible();
            CrisisCardPanel.Instance?.BringToFront();
            DistrictOfferPanel.Instance?.BringToFront();
            TurnPhaseBanner.Instance?.Refresh(blockReasonAfterPhases);
            Debug.LogWarning($"End Turn blocked after phases: {StripRichText(blockReasonAfterPhases)}");
            return false;
        }

        var tm = TurnManager.Instance;
        if (tm == null)
            return false;

        int turnBefore = tm.TurnNumber;
        bool wasPlayerTurn = tm.IsPlayerTurn;
        tm.EndTurn();

        if (wasPlayerTurn && tm.IsPlayerTurn && tm.TurnNumber == turnBefore)
        {
            TurnPhaseBanner.Instance?.Refresh(
                "<color=#FFAA66><b>End Turn stalled</b>  -  check Console for block reason</color>");
            Debug.LogWarning("End Turn: TurnManager.EndTurn returned without advancing (likely a silent block).");
            return false;
        }

        playerEndPhasesRanThisTurn = false;
        return true;
    }

    public static bool TryGetEndTurnBlockReason(out string reason)
    {
        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
        {
            reason = "<color=#FFAA66><b>Crisis card</b>  -  choose a response (Esc defers if available)</color>";
            return true;
        }

        // Pastoral briefing auto-defers via EndTurnDeferHelper. District offers block.
        if (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible)
        {
            reason = "<color=#AADDFF><b>District offer</b>  -  Accept, Not now, or Decline (Esc = Not now)</color>";
            return true;
        }

        if (CityGrowthManager.Instance != null && CityGrowthManager.Instance.HasPendingOffer)
        {
            reason = "<color=#AADDFF><b>District offer</b>  -  Accept, Not now, or Decline</color>";
            return true;
        }

        if (NarrativeEventManager.Instance != null && NarrativeEventManager.Instance.IsAwaitingPlayerChoice)
        {
            reason = "<color=#DDBB88><b>Narrative chronology</b>  -  choose the synod's witness</color>";
            return true;
        }

        if (LiturgicalEventManager.Instance != null && LiturgicalEventManager.Instance.IsAwaitingPlayerChoice)
        {
            reason = "<color=#DDBB88><b>Church-year feast</b>  -  choose the synod's witness</color>";
            return true;
        }

        if (TestimonyColloquyManager.Instance != null && TestimonyColloquyManager.Instance.IsAwaitingPlayerChoice)
        {
            reason = "<color=#88CCFF><b>Testimony colloquy</b>  -  choose patristic reception</color>";
            return true;
        }

        if (SynodicalEmphasisManager.Instance != null && SynodicalEmphasisManager.Instance.IsAwaitingPlayerChoice)
        {
            reason = "<color=#88CCFF><b>Synodical emphasis</b>  -  choose your path</color>";
            return true;
        }

        if (Tier2EmphasisManager.Instance != null && Tier2EmphasisManager.Instance.IsAwaitingPlayerChoice)
        {
            reason = "<color=#88CCFF><b>Confessions emphasis</b>  -  choose your path (T to reopen)</color>";
            return true;
        }

        if (IdentityPickerPanel.Instance != null && IdentityPickerPanel.Instance.IsVisible)
        {
            reason = "<color=#DDCC88><b>Confessional identity</b>  -  choose the synod's witness</color>";
            return true;
        }

        if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
        {
            reason = "<color=#FFAA66><b>Choice card open</b>  -  pick an option or press Esc</color>";
            return true;
        }

        if (DistrictSpecialtyPickerPanel.Instance != null && DistrictSpecialtyPickerPanel.Instance.IsVisible)
        {
            reason = "<color=#FFDD88><b>District specialty</b>  -  pick Seminary / Garrison / Market / Scholastic</color>";
            return true;
        }

        if (LegacySlotPickerPanel.Instance != null && LegacySlotPickerPanel.Instance.IsVisible)
        {
            reason = "<color=#DDCC88><b>Legacy trait</b>  -  choose a slot</color>";
            return true;
        }

        reason = null;
        return false;
    }

    static void SanitizeStaleChoicePanels()
    {
        // Awaiting with no visible card → clear stuck flags (playtest softlock).
        ClearOrphanedAwaitingFlags();

        if (CrisisCardPanel.Instance == null || !CrisisCardPanel.Instance.IsVisible)
            return;

        if (AnyChoicePresenterAwaiting())
            return;

        Debug.LogWarning("End Turn: hiding stale CrisisCardPanel with no active presenter.");
        CrisisCardPanel.Instance.Hide();
    }

    static void ClearOrphanedAwaitingFlags()
    {
        bool cardVisible = CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible;
        if (cardVisible)
            return;

        if (NarrativeEventManager.Instance != null && NarrativeEventManager.Instance.IsAwaitingPlayerChoice)
        {
            Debug.LogWarning("End Turn: clearing orphaned narrative awaiting flag (no visible card).");
            NarrativeEventManager.Instance.ForceClearAwaiting();
        }

        if (LiturgicalEventManager.Instance != null && LiturgicalEventManager.Instance.IsAwaitingPlayerChoice)
        {
            if (!cardVisible)
                LiturgicalEventManager.Instance.EnsurePendingEventVisible();
        }

        if (TestimonyColloquyManager.Instance != null && TestimonyColloquyManager.Instance.IsAwaitingPlayerChoice)
        {
            if (!cardVisible)
                TestimonyColloquyManager.Instance.EnsurePendingColloquyVisible();
        }

        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice && !cardVisible)
            CrisisManager.Instance.EnsurePendingCrisisCardVisible();
    }

    public static void SanitizeStaleChoicePanelsPublic() => SanitizeStaleChoicePanels();

    static bool AnyChoicePresenterAwaiting() =>
        (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice) ||
        (PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice) ||
        (NarrativeEventManager.Instance != null && NarrativeEventManager.Instance.IsAwaitingPlayerChoice) ||
        (LiturgicalEventManager.Instance != null && LiturgicalEventManager.Instance.IsAwaitingPlayerChoice) ||
        (TestimonyColloquyManager.Instance != null && TestimonyColloquyManager.Instance.IsAwaitingPlayerChoice) ||
        (SynodicalEmphasisManager.Instance != null && SynodicalEmphasisManager.Instance.IsAwaitingPlayerChoice) ||
        (Tier2EmphasisManager.Instance != null && Tier2EmphasisManager.Instance.IsAwaitingPlayerChoice);

    static string StripRichText(string richText) =>
        string.IsNullOrEmpty(richText)
            ? ""
            : richText.Replace("<b>", "").Replace("</b>", "")
                .Replace("<color=#FFAA66>", "").Replace("<color=#88CCFF>", "")
                .Replace("<color=#FFDD88>", "").Replace("<color=#DDCC88>", "")
                .Replace("</color>", "");
}
