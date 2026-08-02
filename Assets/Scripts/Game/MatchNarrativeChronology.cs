using System.Collections.Generic;
using UnityEngine;

/// <summary>Match narrative clock: salvation history → church year at Ascension.</summary>
public class MatchNarrativeChronology : MonoBehaviour
{
    public const int DefaultDaysPerTurn = 18;
    public const int ForcedAscensionTurn = 40;

    public static MatchNarrativeChronology Instance { get; private set; }

    public NarrativeChronologyPhase Phase { get; private set; } = NarrativeChronologyPhase.SalvationHistory;
    public int NarrativeDay { get; private set; }
    public int ChurchYearStartTurn { get; private set; } = -1;

    readonly HashSet<string> resolvedEventIds = new();
    readonly HashSet<string> unlockedCommemorationKeys = new();
    int lastAdvancedTurn = -1;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsEventResolved(string id) => resolvedEventIds.Contains(id);

    public bool IsCommemorationUnlocked(ChurchYearEntry entry)
    {
        if (Phase != NarrativeChronologyPhase.ChurchYear)
            return false;
        return unlockedCommemorationKeys.Contains(ChurchYearCalendar.FeastKey(entry));
    }

    public void AdvanceForTurn(int turn)
    {
        if (lastAdvancedTurn == turn)
            return;

        NarrativeDay += DefaultDaysPerTurn;
        lastAdvancedTurn = turn;

        if (Phase == NarrativeChronologyPhase.SalvationHistory &&
            turn >= ForcedAscensionTurn &&
            !resolvedEventIds.Contains("ascension"))
        {
            NarrativeDay = Mathf.Max(NarrativeDay, NarrativeEventDatabase.Events[6].TriggerNarrativeDay);
        }
    }

    public bool TryGetNextDueEvent(out NarrativeEventEntry entry)
    {
        entry = default;
        for (int i = 0; i < NarrativeEventDatabase.Events.Length; i++)
        {
            var candidate = NarrativeEventDatabase.Events[i];
            if (resolvedEventIds.Contains(candidate.Id))
                continue;
            if (NarrativeDay < candidate.TriggerNarrativeDay)
                continue;
            if (candidate.ActivatesChurchYear && Phase == NarrativeChronologyPhase.ChurchYear)
                continue;

            entry = candidate;
            return true;
        }

        return false;
    }

    public void ResolveEvent(NarrativeEventEntry entry, int turn)
    {
        if (!string.IsNullOrEmpty(entry.Id))
            resolvedEventIds.Add(entry.Id);

        if (entry.DaysAdvanceOnResolve > 0)
            NarrativeDay += entry.DaysAdvanceOnResolve;

        UnlockCommemorations(entry.UnlockNameFragments);

        if (entry.ActivatesChurchYear && Phase == NarrativeChronologyPhase.SalvationHistory)
            ActivateChurchYear(turn);
    }

    public void ActivateChurchYear(int turn)
    {
        Phase = NarrativeChronologyPhase.ChurchYear;
        ChurchYearStartTurn = turn;
        UnlockAllPrincipalFeasts();
        UnlockCommemorations(new[] { "Pentecost", "All Saints", "Reformation" });
        Debug.Log($"Narrative chronology: Church Year begins on match turn {turn} (narrative day {NarrativeDay}).");
    }

    public void UnlockCommemorations(IEnumerable<string> nameFragments)
    {
        if (nameFragments == null)
            return;

        foreach (var fragment in nameFragments)
            UnlockCommemorationMatching(fragment);
    }

    public void UnlockCommemorationMatching(string nameFragment)
    {
        if (string.IsNullOrEmpty(nameFragment))
            return;

        foreach (var pair in ChurchYearCalendar.AllByDate)
        {
            foreach (var entry in pair.Value)
            {
                if (entry.Name == null ||
                    !entry.Name.Contains(nameFragment, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                unlockedCommemorationKeys.Add(ChurchYearCalendar.FeastKey(entry));
            }
        }
    }

    void UnlockAllPrincipalFeasts()
    {
        foreach (var pair in ChurchYearCalendar.AllByDate)
        {
            foreach (var entry in pair.Value)
            {
                if (entry.IsPrincipalFeast ||
                    entry.Kind == ChurchYearEntryKind.FeastOrFestival)
                    unlockedCommemorationKeys.Add(ChurchYearCalendar.FeastKey(entry));
            }
        }
    }

    public static string FormatDashboardLine()
    {
        if (Instance == null)
            return "<color=#C9B896>Chronology:</color> Salvation history";

        if (Instance.Phase == NarrativeChronologyPhase.ChurchYear)
        {
            string church = ChurchYearFlavor.FormatDashboardLine();
            return $"<color=#C9B896>Chronology:</color> Church Year  ·  {church}";
        }

        if (NarrativeEventDatabase.TryGetById(GetCurrentEraHintId(), out var next))
            return $"<color=#C9B896>Chronology:</color> {next.EraLabel}  ·  day {Instance.NarrativeDay}";

        return $"<color=#C9B896>Chronology:</color> Salvation history  ·  day {Instance.NarrativeDay}";
    }

    static string GetCurrentEraHintId()
    {
        if (Instance == null)
            return null;

        for (int i = NarrativeEventDatabase.Events.Length - 1; i >= 0; i--)
        {
            var e = NarrativeEventDatabase.Events[i];
            if (Instance.IsEventResolved(e.Id))
                return e.Id;
        }

        return null;
    }

    public static string FormatCompactObservance()
    {
        if (Instance == null || Instance.Phase == NarrativeChronologyPhase.SalvationHistory)
        {
            int day = Instance?.NarrativeDay ?? 0;
            return $"Salvation history (day {day})";
        }

        return ChurchYearFlavor.FormatChurchYearCompactObservance();
    }
}
