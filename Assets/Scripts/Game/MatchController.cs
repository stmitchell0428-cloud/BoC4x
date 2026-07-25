using System.Linq;
using UnityEngine;

public enum MatchResult
{
    InProgress,
    SynodVictory,
    SchismaticVictory
}

public class MatchController : MonoBehaviour
{
    public static MatchController Instance { get; private set; }

    const float AdherenceWinPercent = 95f;
    const int AdherenceWinTurns = 5;
    const int FameWinThreshold = 75;

    MatchResult result = MatchResult.InProgress;
    int adherenceWinStreak;
    string victoryDetail = "";

    public MatchResult Result => result;
    public bool IsMatchOver => result != MatchResult.InProgress;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnPlayerTurnEnded()
    {
        if (IsMatchOver) return;

        var faction = FirstSteps.Instance;
        if (faction != null && faction.ConfessionalAdherence >= AdherenceWinPercent)
            adherenceWinStreak++;
        else
            adherenceWinStreak = 0;

        if (adherenceWinStreak >= AdherenceWinTurns)
        {
            EndMatch(MatchResult.SynodVictory,
                $"Confessional adherence held at {AdherenceWinPercent:F0}%+ for {AdherenceWinTurns} turns.");
            return;
        }

        EvaluateConditions();
    }

    public void EvaluateConditions()
    {
        if (IsMatchOver) return;

        if (TurnManager.Instance == null || CityManager.Instance == null) return;

        bool playerHasUnits = TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1).Any(u => u.IsAlive);

        var wittenberg = CityManager.Instance.GetCityByName("Wittenberg");

        if (!playerHasUnits)
        {
            EndMatch(MatchResult.SchismaticVictory, "All synod units were destroyed.");
            return;
        }

        if (wittenberg != null && wittenberg.Faction == FactionId.Schismatic &&
            SchismManager.Instance != null && SchismManager.Instance.HasSchismed)
        {
            EndMatch(MatchResult.SchismaticVictory, "Wittenberg fell to schismatic forces.");
            return;
        }

        if (ConfessionResearchManager.Instance != null &&
            ConfessionResearchManager.Instance.HasDoctrineTrioVictory)
        {
            EndMatch(MatchResult.SynodVictory,
                "The synod completed the Tier 6 doctrine trio  -  CTCR guidance, Nagel's preaching, and global Lutheran fellowship united under Scripture.");
            return;
        }

        if (FirstSteps.Instance != null && FirstSteps.Instance.ConfessionalFame >= FameWinThreshold)
        {
            EndMatch(MatchResult.SynodVictory,
                $"Confessional fame reached {FameWinThreshold}  -  the synod's witness reshaped the land.");
            return;
        }

        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            if (faction.population <= 0)
            {
                EndMatch(MatchResult.SchismaticVictory, "The synod's population collapsed.");
                return;
            }

            if (faction.confessionalAdherence <= 0f)
            {
                EndMatch(MatchResult.SchismaticVictory, "Confessional adherence was lost entirely.");
                return;
            }
        }
    }

    void EndMatch(MatchResult matchResult, string detail)
    {
        if (IsMatchOver) return;
        result = matchResult;
        victoryDetail = detail;
        Debug.Log(matchResult == MatchResult.SynodVictory
            ? $"Victory: {detail}"
            : $"Defeat: {detail}");
        MatchEndPanel.Instance?.Show(matchResult, detail);
        TurnPhaseBanner.Instance?.Refresh();
    }

    public string AdherenceVictoryProgress()
    {
        var faction = FirstSteps.Instance;
        if (faction == null) return "";

        if (adherenceWinStreak > 0 ||
            faction.ConfessionalAdherence >= AdherenceWinPercent - 10f)
        {
            return $"Adherence win {adherenceWinStreak}/{AdherenceWinTurns} @ {AdherenceWinPercent:F0}%+";
        }

        return "";
    }

    public string DoctrineTrioVictoryProgress()
    {
        var research = ConfessionResearchManager.Instance;
        if (research == null || research.HasDoctrineTrioVictory)
            return "";

        bool ctcr = research.IsTechUnlocked(ConfessionTechId.CTCRReports);
        bool nagel = research.IsTechUnlocked(ConfessionTechId.NormanNagel);
        bool glf = research.IsTechUnlocked(ConfessionTechId.GlobalLutheranFellowship);
        if (!ctcr && !nagel && !glf)
            return "";

        return $"Doctrine win: {(ctcr ? "CTCR OK" : "CTCR (...)")} | {(nagel ? "Nagel OK" : "Nagel (...)")} | {(glf ? "GLF OK" : "GLF (...)")}";
    }

    public string TheologyScienceVictoryProgress() => DoctrineTrioVictoryProgress();

    public string VictoryProgressLabel()
    {
        var parts = new System.Collections.Generic.List<string>();
        string adherence = AdherenceVictoryProgress();
        if (!string.IsNullOrEmpty(adherence))
            parts.Add(adherence);

        string doctrineTrio = DoctrineTrioVictoryProgress();
        if (!string.IsNullOrEmpty(doctrineTrio))
            parts.Add(doctrineTrio);

        var faction = FirstSteps.Instance;
        if (faction != null && faction.ConfessionalFame >= FameWinThreshold - 15)
            parts.Add($"Fame win {faction.ConfessionalFame}/{FameWinThreshold}");

        return parts.Count > 0 ? string.Join("  |  ", parts) : "";
    }
}
