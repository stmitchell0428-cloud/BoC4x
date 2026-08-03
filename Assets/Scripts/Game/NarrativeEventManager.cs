using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Salvation-history and Reformation narrative choice cards keyed to the narrative clock.</summary>
public class NarrativeEventManager : MonoBehaviour, IChoiceCardPresenter
{
    public static NarrativeEventManager Instance { get; private set; }

    const int MinTurn = 1;

    readonly Queue<string> pendingEventIds = new();
    Coroutine deferredPresentRoutine;
    string pendingTitle;
    string pendingBody;
    List<CrisisCardChoice> pendingChoices;
    NarrativeEventEntry pendingEntry;

    public bool IsAwaitingPlayerChoice { get; private set; }

    void Awake() => Instance = this;

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TurnStarted -= OnTurnStarted;
            TurnManager.Instance.TurnStarted += OnTurnStarted;
        }
    }

    void OnEnable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted += OnTurnStarted;
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnStarted -= OnTurnStarted;
        if (Instance == this)
            Instance = null;
        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
    }

    void OnTurnStarted()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return;
        if (IsAwaitingPlayerChoice)
            return;

        int turn = TurnManager.Instance.TurnNumber;
        if (turn < MinTurn)
            return;

        MatchNarrativeChronology.Instance?.AdvanceForTurn(turn);
        QueueDueEvents();
        ChoiceCardQueue.Register(ChoiceCardQueue.OrderNarrative, TryPresentTurnStartCard);
    }

    bool TryPresentTurnStartCard()
    {
        if (ChoiceCardBlocking.BlocksOtherEvents())
            return false;
        if (pendingEventIds.Count == 0)
            return false;

        TryPresentNext();
        return IsAwaitingPlayerChoice;
    }

    void QueueDueEvents()
    {
        if (MatchNarrativeChronology.Instance == null || pendingEventIds.Count > 0)
            return;

        if (MatchNarrativeChronology.Instance.TryGetNextDueEvent(out var entry))
            pendingEventIds.Enqueue(entry.Id);
    }

    void TryPresentNext()
    {
        if (pendingEventIds.Count == 0)
            return;
        if (!NarrativeEventDatabase.TryGetById(pendingEventIds.Peek(), out var entry))
        {
            pendingEventIds.Dequeue();
            TryPresentNext();
            return;
        }

        PresentEvent(entry);
    }

    public void OnChoiceCardDismissed()
    {
        int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;
        if (!string.IsNullOrEmpty(pendingEntry.Id))
        {
            MatchNarrativeChronology.Instance?.ResolveEvent(pendingEntry, turn);
            if (pendingEventIds.Count > 0 && pendingEventIds.Peek() == pendingEntry.Id)
                pendingEventIds.Dequeue();
        }

        IsAwaitingPlayerChoice = false;
        ClearPendingCard();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
    }

    public void OnChoiceCardCancelled()
    {
        ApplyDeferredJudgment();
        IsAwaitingPlayerChoice = false;
        ClearPendingCard();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh("Synod deferred the narrative decision.");
    }

    public void EnsurePendingEventVisible()
    {
        if (!IsAwaitingPlayerChoice || pendingChoices == null)
            return;

        if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
        {
            CrisisCardPanel.Instance.BringToFront();
            return;
        }

        TryShowImmediate(pendingTitle, pendingBody, pendingChoices);
    }

    void ClearPendingCard()
    {
        pendingTitle = null;
        pendingBody = null;
        pendingChoices = null;
        pendingEntry = default;
    }

    void PresentEvent(NarrativeEventEntry entry)
    {
        string title = $"<color=#DDBB88>{entry.Title}</color>";
        string body = NarrativeEventDatabase.FormatBody(entry);
        if (MatchNarrativeChronology.Instance?.Phase == NarrativeChronologyPhase.ChurchYear)
            body = ChurchYearFlavor.EnrichEventBody(body, IsSaturated());
        else
            body = AppendNarrativeClock(body);

        var choices = BuildChoices(entry);

        if (TryShowImmediate(title, body, choices))
        {
            pendingEntry = entry;
            return;
        }

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
        deferredPresentRoutine = StartCoroutine(RetryPresentDeferred(title, body, choices, entry));
    }

    static string AppendNarrativeClock(string body)
    {
        int day = MatchNarrativeChronology.Instance?.NarrativeDay ?? 0;
        return body + $"\n\n<size=12><color=#C9B896><b>Salvation history</b>  ·  narrative day {day}</color></size>";
    }

    List<CrisisCardChoice> BuildChoices(NarrativeEventEntry entry)
    {
        var list = new List<CrisisCardChoice>
        {
            new(entry.ChoiceA.Label, entry.ChoiceA.Description, () => ApplyChoice(entry, entry.ChoiceA)),
            new(entry.ChoiceB.Label, entry.ChoiceB.Description, () => ApplyChoice(entry, entry.ChoiceB))
        };

        if (entry.ChoiceC.HasValue)
        {
            var c = entry.ChoiceC.Value;
            list.Add(new(c.Label, c.Description, () => ApplyChoice(entry, c)));
        }

        return list;
    }

    void ApplyChoice(NarrativeEventEntry entry, NarrativeEventChoice choice)
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        if (choice.ManuscriptCost > 0 && faction.scriptureManuscripts < choice.ManuscriptCost)
        {
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + 3f, 0f, 100f);
            faction.confessionalAdherence = Mathf.Clamp(
                faction.confessionalAdherence + 2f,
                faction.EffectiveMinAdherenceFloor,
                100f);
            Debug.LogWarning($"Narrative event {entry.Id}: insufficient manuscripts — partial response only.");
            return;
        }

        if (choice.ManuscriptCost > 0)
            faction.scriptureManuscripts -= choice.ManuscriptCost;

        faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + choice.LawDelta, 0f, 100f);
        faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + choice.GospelDelta, 0f, 100f);
        faction.confessionalAdherence = Mathf.Clamp(
            faction.confessionalAdherence + choice.AdherenceDelta,
            faction.EffectiveMinAdherenceFloor,
            100f);
        if (choice.FameDelta > 0)
            faction.AddFame(choice.FameDelta);

        Debug.Log($"Narrative event {entry.Id}: {choice.Label}.");
    }

    static void ApplyDeferredJudgment()
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        faction.confessionalAdherence = Mathf.Clamp(
            faction.confessionalAdherence - 2f,
            faction.EffectiveMinAdherenceFloor,
            100f);
    }

    static bool IsSaturated() =>
        SchismaticBlocRegistry.Instance != null &&
        SchismaticBlocRegistry.Instance.ActiveCount >= SchismaticBlocRegistry.MaxBlocs;

    bool TryShowImmediate(string title, string body, IReadOnlyList<CrisisCardChoice> choices)
    {
        if (CrisisCardPanel.Instance == null || CrisisCardPanel.Instance.IsVisible)
            return false;
        if (ChoiceCardBlocking.BlocksOtherEvents())
            return false;
        if (!CrisisCardPanel.Instance.Show(title, body, choices, this))
            return false;

        IsAwaitingPlayerChoice = true;
        pendingTitle = title;
        pendingBody = body;
        pendingChoices = choices is List<CrisisCardChoice> list
            ? list
            : new List<CrisisCardChoice>(choices);
        TurnPhaseBanner.Instance?.Refresh("Narrative chronology — choose the synod's witness.");
        return true;
    }

    IEnumerator RetryPresentDeferred(string title, string body, List<CrisisCardChoice> choices, NarrativeEventEntry entry)
    {
        for (int i = 0; i < 8; i++)
        {
            yield return null;
            if (i == 0)
                yield return new WaitForEndOfFrame();
            if (IsAwaitingPlayerChoice)
                yield break;
            if (TryShowImmediate(title, body, choices))
            {
                pendingEntry = entry;
                deferredPresentRoutine = null;
                yield break;
            }
        }

        deferredPresentRoutine = null;
    }
}
