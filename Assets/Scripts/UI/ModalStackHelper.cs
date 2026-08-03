/// <summary>Counts open modal panels for the turn banner stack indicator.</summary>
public static class ModalStackHelper
{
    public static int OpenModalCount
    {
        get
        {
            int count = 0;
            if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
                count++;
            if (DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible)
                count++;
            if (DistrictSpecialtyPickerPanel.Instance != null && DistrictSpecialtyPickerPanel.Instance.IsVisible)
                count++;
            if (LegacySlotPickerPanel.Instance != null && LegacySlotPickerPanel.Instance.IsVisible)
                count++;
            if (SynodBriefPanel.Instance != null && SynodBriefPanel.Instance.IsVisible)
                count++;
            if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen)
                count++;
            if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen)
                count++;
            if (DiplomacyPanel.Instance != null && DiplomacyPanel.Instance.IsVisible)
                count++;
            if (ClergyRosterPanel.Instance != null && ClergyRosterPanel.Instance.IsVisible)
                count++;
            if (IdentityPickerPanel.Instance != null && IdentityPickerPanel.Instance.IsVisible)
                count++;
            if (MatchEndPanel.Instance != null && MatchEndPanel.Instance.IsVisible)
                count++;
            return count;
        }
    }

    public static string FormatBannerSuffix()
    {
        int count = OpenModalCount;
        return count <= 0
            ? ""
            : $"<color=#AABBCC>[{count} panel{(count == 1 ? "" : "s")} open]</color>";
    }
}
