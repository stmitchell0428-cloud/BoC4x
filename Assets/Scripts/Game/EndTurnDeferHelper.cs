/// <summary>End-turn choice cards that may defer without blocking the turn advance.</summary>
public static class EndTurnDeferHelper
{
    /// <summary>Pastoral briefings only — district offers must be answered (they block End Turn).</summary>
    public static bool HasAutoDeferChoicePending =>
        PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice;

    public static bool HasDeferrableChoicePending =>
        HasAutoDeferChoicePending ||
        (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible);

    public static void DeferPendingChoices()
    {
        if (PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice)
            PastoralBriefingManager.Instance.DeferForEndTurn();
    }

    public static string FormatDeferredHint()
    {
        if (!HasAutoDeferChoicePending)
            return "";

        return "<color=#88CCFF><b>Deferred</b></color>  -  pastoral briefing (End Turn continues)";
    }
}
