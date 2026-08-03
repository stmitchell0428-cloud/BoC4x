/// <summary>End-turn choice cards that may defer without blocking the turn advance.</summary>
public static class EndTurnDeferHelper
{
    public static bool HasDeferrableChoicePending =>
        (PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice) ||
        (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible);

    public static void DeferPendingChoices()
    {
        if (PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice)
            PastoralBriefingManager.Instance.DeferForEndTurn();

        if (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible)
            CityGrowthManager.Instance?.DeferPendingOffer();
    }

    public static string FormatDeferredHint()
    {
        if (!HasDeferrableChoicePending)
            return "";

        var parts = new System.Collections.Generic.List<string>();
        if (PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice)
            parts.Add("pastoral briefing");
        if (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible)
            parts.Add("district offer");

        return parts.Count == 0
            ? ""
            : $"<color=#88CCFF><b>Deferred</b></color>  -  {string.Join(", ", parts)} (End Turn continues)";
    }
}
