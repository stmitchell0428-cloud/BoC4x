/// <summary>Salvation-history calendar line for dashboard and intro cards.</summary>
public static class SalvationHistoryFlavor
{
    public static int CurrentNarrativeDay { get; private set; } = 18;

    public static void SetNarrativeDay(int day) =>
        CurrentNarrativeDay = day < 1 ? 1 : day;

    public static void SyncFromIntroBeat(int beatIndex) =>
        SetNarrativeDay(SalvationHistoryDatabase.NarrativeDayForIntroBeat(beatIndex));

    public static string FormatDashboardLine() =>
        $"<color=#C9B896>Salvation day</color> {CurrentNarrativeDay}";

    public static string FormatCardFooter(int narrativeDay) =>
        $"<size=12><color=#AABBCC>Salvation history | narrative day {narrativeDay}</color></size>";

    public static string FormatWaltherHint()
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return "";

        float law = faction.CivicRestraint;
        float gospel = faction.SpiritualComfort;
        string hint = law > gospel + 8f
            ? "Law runs ahead of Gospel — consider a Gospel-leaning choice."
            : gospel > law + 8f
                ? "Gospel runs ahead of Law — consider a Law-leaning choice."
                : "Law and Gospel are near balance (50% at start).";

        return $"<size=12><color=#AABBCC>Now: Law {law:F0}% | Gospel {gospel:F0}%  —  {hint}</color></size>";
    }
}
