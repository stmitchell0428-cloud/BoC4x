/// <summary>Shared mutual exclusion for narrative choice-card presenters.</summary>
public static class ChoiceCardBlocking
{
    public static bool BlocksOtherEvents()
    {
        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (PastoralBriefingManager.Instance != null && PastoralBriefingManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (NarrativeEventManager.Instance != null && NarrativeEventManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (LiturgicalEventManager.Instance != null && LiturgicalEventManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (TestimonyColloquyManager.Instance != null && TestimonyColloquyManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (SynodicalEmphasisManager.Instance != null && SynodicalEmphasisManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (Tier2EmphasisManager.Instance != null && Tier2EmphasisManager.Instance.IsAwaitingPlayerChoice)
            return true;
        if (IdentityPickerPanel.Instance != null && IdentityPickerPanel.Instance.IsVisible)
            return true;
        if (LoadingScreenPanel.Instance != null && LoadingScreenPanel.Instance.IsVisible)
            return true;
        if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
            return true;
        return false;
    }
}
