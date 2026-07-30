using UnityEngine;

/// <summary>
/// Post–3rd-schism pressure: strife meter, raids from strongest heresy, reconciliation.
/// </summary>
public static class UnionStrifeManager
{
    public const int RaidThreshold = 40;
    public const int ReconciliationMinStrife = 15;
    public const int ReconciliationManuscriptCost = 8;

    static int strife;
    static int turnsSinceRaid;
    static int citiesLostToSchismWhileSaturated;

    public static int Strife => strife;

    public static bool IsSaturated =>
        SchismaticBlocRegistry.Instance != null &&
        SchismaticBlocRegistry.Instance.ActiveCount >= SchismaticBlocRegistry.MaxBlocs;

    public static void ResetForNewMatch()
    {
        strife = 0;
        turnsSinceRaid = 0;
        citiesLostToSchismWhileSaturated = 0;
    }

    public static void AddStrife(int delta) =>
        strife = Mathf.Clamp(strife + delta, 0, 100);

    public static void NotifyPlayerCityLostToSchism()
    {
        if (!IsSaturated)
            return;

        citiesLostToSchismWhileSaturated++;
        AddStrife(15);
        if (citiesLostToSchismWhileSaturated >= 2)
        {
            MatchController.Instance?.ForceSchismaticVictory(
                "Two synod cities fell while three dissenting synods pressed the land.");
        }
    }

    public static void ProcessPlayerEndTurn()
    {
        if (!IsSaturated)
        {
            strife = Mathf.Max(0, strife - 2);
            return;
        }

        AddStrife(4);
        turnsSinceRaid++;

        if (strife >= RaidThreshold && turnsSinceRaid >= 4)
        {
            turnsSinceRaid = 0;
            LaunchRaidFromStrongest();
        }
    }

    public static bool CanOfferReconciliation(SchismaticBlocId blocId)
    {
        if (!IsSaturated || strife < ReconciliationMinStrife)
            return false;

        var city = CityManager.Instance?.GetAiCity(blocId);
        return city != null && city.IsCapital && city.Population <= 12;
    }

    public static void TryReconcileBloc(SchismaticBlocId blocId)
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        if (faction.scriptureManuscripts < ReconciliationManuscriptCost)
        {
            PopulationSync.ApplyDeltaToPrimaryCity(-2);
            SchismManager.Instance?.ReinforceExistingBloc(
                blocId,
                "Reconciliation failed for lack of manuscripts — the party grew bolder.",
                nearPlayer: true);
            AddStrife(10);
            TurnPhaseBanner.Instance?.Refresh(
                "<color=#FF8866>Reconciliation failed</color> — dissent grew stronger.");
            return;
        }

        faction.scriptureManuscripts -= ReconciliationManuscriptCost;

        // Need enough adherence and a roll to dissolve.
        bool success = faction.confessionalAdherence >= 55f && Random.value < 0.55f;
        if (!success)
        {
            SchismManager.Instance?.ReinforceExistingBloc(
                blocId,
                "Reconciliation colloquy failed — the party dug in.",
                nearPlayer: false);
            AddStrife(8);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 4f, 0f, 100f);
            TurnPhaseBanner.Instance?.Refresh(
                "<color=#FF8866>Reconciliation failed</color> — sisters in error remain.");
            return;
        }

        if (SchismaticBlocRegistry.Instance?.TryDissolveBloc(blocId) == true)
        {
            AddStrife(-25);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 6f, 0f, 100f);
            SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.CrisisSurvivor);
            TurnPhaseBanner.Instance?.Refresh(
                "<color=#88EEAA><b>Reconciliation</b></color> — a dissenting capital yielded; a schism slot opens.");
            Debug.LogWarning($"Union strife: reconciled and dissolved bloc {blocId}.");
        }
        else
        {
            SchismManager.Instance?.ReinforceExistingBloc(blocId, "Reconciliation stalled.", nearPlayer: false);
        }
    }

    static void LaunchRaidFromStrongest()
    {
        var registry = SchismaticBlocRegistry.Instance;
        if (registry == null || CityManager.Instance == null)
            return;

        SchismaticBlocId? strongest = null;
        int bestPop = -1;
        foreach (var record in registry.ActiveBlocs.Values)
        {
            var city = CityManager.Instance.GetAiCity(record.BlocId);
            int pop = city != null ? city.Population : 0;
            if (pop > bestPop)
            {
                bestPop = pop;
                strongest = record.BlocId;
            }
        }

        if (strongest == null)
            return;

        SchismManager.Instance?.ReinforceExistingBloc(
            strongest.Value,
            "Union strife — strongest dissent launched a raid toward the synod.",
            nearPlayer: true);
        AddStrife(-10);
        string observance = ChurchYearFlavor.FormatCompactObservance();
        TurnPhaseBanner.Instance?.Refresh(
            $"<color=#FFCC66><b>Union strife</b></color> — a dissenting raid advances on the synod! " +
            $"<size=12>({observance})</size>");
        Debug.LogWarning($"Union strife raid from {strongest.Value} (strife was {strife + 10}). {observance}");
    }

    public static string FormatStatusLine()
    {
        if (!IsSaturated || strife <= 0)
            return "";
        return $"<color=#EEAA66>Union strife: {strife}/100</color>" +
               (strife >= RaidThreshold ? "  -  raid pressure" : "") +
               $"  |  {ChurchYearFlavor.FormatCompactObservance()}";
    }
}
