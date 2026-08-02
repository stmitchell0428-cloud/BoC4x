using System.Linq;
using UnityEngine;

public enum MatchResult
{
    InProgress,
    SynodVictory,
    SchismaticVictory,
    SynodDefeat
}

public class MatchController : MonoBehaviour
{
    public static MatchController Instance { get; private set; }

    const float AdherenceWinPercent = 100f;
    const int AdherenceWinTurns = 5;
    const int FameWinThreshold = 120;

    public float AdherenceWinTarget => AdherenceWinPercent;
    public int AdherenceWinTurnsRequired => AdherenceWinTurns;
    public int FameWinTarget => FameWinThreshold;
    public int AdherenceWinStreak => adherenceWinStreak;

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
                $"Confessional adherence held at {AdherenceWinPercent:F0}% for {AdherenceWinTurns} turns.");
            return;
        }

        EvaluateConditions();
    }

    public void ForceSchismaticVictory(string detail)
    {
        if (IsMatchOver) return;
        EndMatch(MatchResult.SchismaticVictory, detail);
    }

    public void EvaluateConditions()
    {
        if (IsMatchOver) return;

        if (TurnManager.Instance == null || CityManager.Instance == null) return;

        bool playerHasUnits = TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1).Any(u => u.IsAlive);
        bool playerHasCities = CityManager.Instance.GetSynodPlayerCities(SynodPlayerId.Player1).Count > 0;

        var wittenberg = CityManager.Instance.GetCityByName("Wittenberg");

        if (!playerHasUnits && !playerHasCities)
        {
            EndMatch(MatchResult.SynodDefeat, "All synod units were destroyed and no cities remain.");
            return;
        }

        if (!playerHasUnits && playerHasCities)
        {
            TurnPhaseBanner.Instance?.Refresh(
                "<color=#FFAA88><b>Army lost</b></color>  -  train new units at your city (C) before the synod is overrun");
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
            bool hasSynodCities = CityManager.Instance.GetSynodPlayerCities(SynodPlayerId.Player1).Count > 0;
            if (hasSynodCities && faction.population <= 0)
            {
                EndMatch(MatchResult.SynodDefeat, "The synod's population collapsed.");
                return;
            }

            if (faction.confessionalAdherence <= 0f)
            {
                EndMatch(MatchResult.SynodDefeat, "Confessional adherence was lost entirely.");
                return;
            }
        }
    }

    void EndMatch(MatchResult matchResult, string detail)
    {
        if (IsMatchOver) return;
        result = matchResult;
        victoryDetail = detail;
        Debug.Log(matchResult switch
        {
            MatchResult.SynodVictory => $"Victory: {detail}",
            MatchResult.SchismaticVictory => $"Schismatic victory: {detail}",
            _ => $"Defeat: {detail}"
        });
        MatchEndPanel.Instance?.Show(matchResult, detail);
        TurnPhaseBanner.Instance?.Refresh();
    }

    public string AdherenceVictoryProgress()
    {
        var faction = FirstSteps.Instance;
        if (faction == null) return "";

        if (adherenceWinStreak > 0 ||
            faction.ConfessionalAdherence >= AdherenceWinPercent - 12f)
        {
            return $"Adherence win {adherenceWinStreak}/{AdherenceWinTurns} @ {AdherenceWinPercent:F0}%";
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

    public string FormatVictoryBriefSection()
    {
        var lines = new System.Collections.Generic.List<string>();
        var faction = FirstSteps.Instance;
        var research = ConfessionResearchManager.Instance;

        lines.Add("<size=13><color=#DDEEAA><b>Synod victory</b></color> (any one path)</size>");

        float adherence = faction != null ? faction.ConfessionalAdherence : 0f;
        string streakColor = adherenceWinStreak > 0 ? "#88FFAA" : "#CCCCCC";
        lines.Add(
            $"<size=13>• <b>Adherence:</b> Hold {AdherenceWinPercent:F0}% for {AdherenceWinTurns} consecutive turns\n" +
            $"  <color={streakColor}>Now {adherence:F1}%  |  Streak {adherenceWinStreak}/{AdherenceWinTurns}</color></size>");

        bool ctcr = research != null && research.IsTechUnlocked(ConfessionTechId.CTCRReports);
        bool nagel = research != null && research.IsTechUnlocked(ConfessionTechId.NormanNagel);
        bool glf = research != null && research.IsTechUnlocked(ConfessionTechId.GlobalLutheranFellowship);
        lines.Add(
            "<size=13>• <b>Doctrine trio:</b> Unlock Tier 6 CTCR Reports, Norman Nagel, and Global Lutheran Fellowship\n" +
            $"  {(ctcr ? "<color=#88FFAA>CTCR ✓</color>" : "<color=#FFCC88>CTCR …</color>")}  |  " +
            $"{(nagel ? "<color=#88FFAA>Nagel ✓</color>" : "<color=#FFCC88>Nagel …</color>")}  |  " +
            $"{(glf ? "<color=#88FFAA>GLF ✓</color>" : "<color=#FFCC88>GLF …</color>")}</size>");

        int fame = faction != null ? faction.ConfessionalFame : 0;
        string fameColor = fame >= FameWinThreshold ? "#88FFAA" : fame >= FameWinThreshold - 15 ? "#FFDD88" : "#CCCCCC";
        lines.Add(
            $"<size=13>• <b>Fame:</b> Reach {FameWinThreshold} confessional fame\n" +
            $"  <color={fameColor}>Now {fame}/{FameWinThreshold}</color></size>");

        lines.Add("");
        lines.Add("<size=12><color=#FFAA88><b>Defeat risks</b></color></size>");
        lines.Add("<size=12>• Army wiped with no cities remaining</size>");
        lines.Add("<size=12>• Synod population collapses to zero</size>");
        lines.Add("<size=12>• Confessional adherence falls to 0%</size>");
        lines.Add("<size=12>• Wittenberg captured by schismatic forces after a schism</size>");

        return string.Join("\n", lines);
    }
}
