using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Gate founding Wittenberg until preach, scout survey, and a bound catechism.</summary>
public static class NomadicFoundingGate
{
    public const int RequiredScoutHexes = 10;
    public const int BindCatechismManuscriptCost = 2;

    static bool preachCompleted;
    static readonly HashSet<HexCoordinates> scoutSurveyHexes = new();

    public static bool IsNomadicPhase =>
        CityManager.Instance == null || CityManager.Instance.GetPrimaryPlayerCity() == null;

    public static bool PreachCompleted => preachCompleted;
    public static int ScoutSurveyCount => scoutSurveyHexes.Count;
    public static bool HasBoundCatechism => FirstSteps.Instance != null && FirstSteps.Instance.BoundCatechisms >= 1;

    public static bool RequirementsMet =>
        !IsNomadicPhase ||
        (preachCompleted && ScoutSurveyCount >= RequiredScoutHexes && HasBoundCatechism);

    public static void MarkPreachCompleted()
    {
        if (IsNomadicPhase)
            preachCompleted = true;
    }

    public static void ResetForNewMatch()
    {
        preachCompleted = false;
        scoutSurveyHexes.Clear();
    }

    public static void RecordScoutHex(HexCoordinates hex)
    {
        if (!IsNomadicPhase || HexGridMap.Instance == null)
            return;
        scoutSurveyHexes.Add(HexGridMap.Instance.Wrap(hex));
    }

    public static void RecordScoutPath(IEnumerable<HexCoordinates> hexes)
    {
        if (!IsNomadicPhase || hexes == null)
            return;
        foreach (var hex in hexes)
            RecordScoutHex(hex);
    }

    public static bool TryBindNomadicCatechism()
    {
        if (!IsNomadicPhase)
        {
            Debug.Log("Bind catechism is only available before founding Wittenberg.");
            return false;
        }

        var faction = FirstSteps.Instance;
        if (faction == null)
            return false;

        if (faction.ScriptureManuscripts < BindCatechismManuscriptCost)
        {
            Debug.Log($"Need {BindCatechismManuscriptCost} manuscripts to bind a field catechism ({faction.ScriptureManuscripts} held).");
            return false;
        }

        faction.ScriptureManuscripts -= BindCatechismManuscriptCost;
        faction.AddBoundCatechism(1);
        Debug.Log("Bound a field catechism for the wandering synod (+1 catechism).");
        faction.RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        return true;
    }

    public static string FormatBriefSection()
    {
        if (!IsNomadicPhase)
            return "";

        var lines = new System.Collections.Generic.List<string>
        {
            "<size=13>Found <b>Wittenberg</b> only after the wandering synod completes these steps:</size>",
            "<size=12>1. <b>Preach</b> once  -  Space with the settler selected</size>",
            "<size=12>2. <b>Scout survey</b>  -  move the scout or settler through 10 unique hexes</size>",
            $"<size=12>3. <b>Bind a catechism</b>  -  B key ({BindCatechismManuscriptCost} manuscripts)</size>",
            "<size=12>4. <b>Found the capital</b>  -  F on land with the settler when all checks pass</size>",
            "",
            FormatProgressLine() ?? ""
        };

        return string.Join("\n", lines);
    }

    public static string FormatProgressLine()
    {
        if (!IsNomadicPhase)
            return null;

        var sb = new StringBuilder();
        sb.Append("<b>Found Wittenberg when:</b> ");
        sb.Append(FormatCheck(preachCompleted, "Preach (Space)"));
        sb.Append("  |  ");
        int surveyShown = Mathf.Min(ScoutSurveyCount, RequiredScoutHexes);
        sb.Append(FormatCheck(ScoutSurveyCount >= RequiredScoutHexes,
            $"Scout survey {surveyShown}/{RequiredScoutHexes}"));
        sb.Append("  |  ");
        sb.Append(FormatCheck(HasBoundCatechism, $"Catechism bound (B, {BindCatechismManuscriptCost} mss)"));
        if (RequirementsMet)
            sb.Append("  |  <color=#88FFAA><b>Ready  -  F on settler</b></color>");
        return sb.ToString();
    }

    public static string FormatBlockingReason()
    {
        if (RequirementsMet)
            return "Founding requirements met  -  stand on land with the settler and press F.";

        var parts = new List<string>();
        if (!preachCompleted)
            parts.Add("preach once (Space with settler selected)");
        if (ScoutSurveyCount < RequiredScoutHexes)
        {
            int surveyShown = Mathf.Min(ScoutSurveyCount, RequiredScoutHexes);
            parts.Add($"scout must survey {RequiredScoutHexes} hexes ({surveyShown}/{RequiredScoutHexes})");
        }
        if (!HasBoundCatechism)
            parts.Add($"bind a catechism (B, costs {BindCatechismManuscriptCost} manuscripts)");
        return "Cannot found yet: " + string.Join("; ", parts) + ".";
    }

    public static string FormatProgressShort()
    {
        if (!IsNomadicPhase)
            return "";

        string preach = PreachCompleted ? "OK" : "-";
        string catechism = HasBoundCatechism ? "OK" : "-";
        int surveyShown = Mathf.Min(ScoutSurveyCount, RequiredScoutHexes);
        return $"Founding: Preach {preach} | Survey {surveyShown}/{RequiredScoutHexes} | Catechism {catechism}";
    }

    static string FormatCheck(bool done, string label) =>
        done ? $"<color=#88FFAA>{label} (done)</color>" : $"<color=#FFCC88>{label}</color>";
}
