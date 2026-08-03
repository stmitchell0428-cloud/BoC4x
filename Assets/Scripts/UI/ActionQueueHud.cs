/// <summary>Prominent HUD formatting for build and research queues.</summary>
public static class ActionQueueHud
{
    public static string FormatDashboardBlock()
    {
        string build = CityManager.Instance != null
            ? CityManager.Instance.FormatProminentBuildQueueBlock()
            : "<size=21><color=#FFAA66><b>BUILD</b></color></size>  unavailable";

        string research = ConfessionResearchManager.Instance != null
            ? ConfessionResearchManager.Instance.FormatProminentResearchBlock()
            : "<size=21><color=#77CCFF><b>SCRIPTURE</b></color></size>  unavailable";

        return build + "\n" + research;
    }

    public static string FormatTurnBannerReminder()
    {
        string build = CityManager.Instance != null
            ? CityManager.Instance.FormatCompactBuildSummary()
            : "build —";

        string research = ConfessionResearchManager.Instance != null
            ? ConfessionResearchManager.Instance.FormatCompactResearchSummary()
            : "research —";

        return $"<color=#FFCC55><b>Build</b></color> {build}  |  <color=#77CCFF><b>Scripture</b></color> {research}";
    }
}
