using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Principal-feast and martyr commemoration decision cards keyed to the church-year clock.</summary>
public class LiturgicalEventManager : MonoBehaviour, IChoiceCardPresenter
{
    public static LiturgicalEventManager Instance { get; private set; }

    const int MinTurn = 4;

    readonly HashSet<string> resolvedFeasts = new();
    readonly Dictionary<string, ChurchYearEntry> spawnedFeasts = new();
    readonly Dictionary<string, int> feastSpawnTurn = new();
    Coroutine deferredPresentRoutine;
    string pendingTitle;
    string pendingBody;
    List<CrisisCardChoice> pendingChoices;

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
        if (BlocksOtherEvents())
            return;

        int turn = TurnManager.Instance.TurnNumber;
        if (turn < MinTurn)
            return;

        if (MatchNarrativeChronology.Instance != null &&
            MatchNarrativeChronology.Instance.Phase != NarrativeChronologyPhase.ChurchYear)
            return;

        RegisterSpawnsForTurn(turn);

        if (TryPresentDecisionForSpawnedFeast(turn))
            return;
    }

    /// <summary>True once this feast has appeared on the dashboard calendar this match.</summary>
    public static bool HasFeastSpawned(ChurchYearEntry feast)
    {
        if (Instance == null)
            return false;
        return Instance.spawnedFeasts.ContainsKey(ChurchYearCalendar.FeastKey(feast));
    }

    /// <summary>True if a feast spawned on this turn or the prior turn (decision / briefing window).</summary>
    public bool HasRecentFeastSpawn(int turn)
    {
        foreach (var pair in feastSpawnTurn)
        {
            if (pair.Value >= turn - 1 && pair.Value <= turn)
                return true;
        }

        return false;
    }

    static bool BlocksOtherEvents() => ChoiceCardBlocking.BlocksOtherEvents();

    void RegisterSpawnsForTurn(int turn)
    {
        foreach (var feast in ChurchYearCalendar.DecisionFeastsForTurn(turn))
        {
            string key = ChurchYearCalendar.FeastKey(feast);
            if (spawnedFeasts.ContainsKey(key))
                continue;

            spawnedFeasts[key] = feast;
            feastSpawnTurn[key] = turn;
            Debug.Log($"Church Year spawn: {feast.Name} ({feast.Month}/{feast.Day}) on turn {turn}.");
        }
    }

    bool TryPresentDecisionForSpawnedFeast(int turn)
    {
        ChurchYearEntry? candidate = null;
        string candidateKey = null;
        int bestSpawnTurn = -1;

        foreach (var pair in spawnedFeasts)
        {
            string key = pair.Key;
            if (resolvedFeasts.Contains(key))
                continue;
            if (!feastSpawnTurn.TryGetValue(key, out int spawnTurn) || spawnTurn >= turn)
                continue;

            if (spawnTurn > bestSpawnTurn)
            {
                bestSpawnTurn = spawnTurn;
                candidate = pair.Value;
                candidateKey = key;
            }
        }

        if (!candidate.HasValue || candidateKey == null)
            return false;

        PresentFeastCard(candidate.Value, turn, candidateKey);
        return true;
    }

    public void OnChoiceCardDismissed()
    {
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
        TurnPhaseBanner.Instance?.Refresh("Synod deferred the feast decision.");
    }

    public void EnsurePendingEventVisible()
    {
        if (!IsAwaitingPlayerChoice || pendingChoices == null || pendingChoices.Count == 0)
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
    }

    void PresentFeastCard(ChurchYearEntry feast, int turn, string feastKey)
    {
        var choices = BuildChoices(feast, turn);
        if (choices == null || choices.Count == 0)
            return;

        string title = $"<color=#DDBB88>Church Year — {feast.Name}</color>";
        string body = ChurchYearFlavor.EnrichEventBody(FormatBody(feast), saturatedEmphasis: IsSaturated());

        if (TryShowImmediate(title, body, choices))
        {
            resolvedFeasts.Add(feastKey);
            return;
        }

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
        deferredPresentRoutine = StartCoroutine(RetryPresentDeferred(title, body, choices, feastKey));
    }

    static bool IsSaturated() =>
        SchismaticBlocRegistry.Instance != null &&
        SchismaticBlocRegistry.Instance.ActiveCount >= SchismaticBlocRegistry.MaxBlocs;

    static string FormatBody(ChurchYearEntry feast)
    {
        string prompt = ChurchYearCalendar.IsMartyrCommemoration(feast)
            ? $"The synod remembers {feast.Name}. How will you honor costly fidelity this season?"
            : ChurchYearCalendar.IsBiblicalCommemoration(feast)
                ? $"Scripture and calendar converge on {feast.Name}. What witness will the synod choose?"
                : $"The church calendar turns to {feast.Name}. How will the synod respond?";

        return $"{prompt}\n\n<size=12><color=#8899AA>{feast.KindLabel} · fixed date {feast.Month}/{feast.Day}</color></size>";
    }

    List<CrisisCardChoice> BuildChoices(ChurchYearEntry feast, int turn)
    {
        bool martyr = ChurchYearCalendar.IsMartyrCommemoration(feast);
        bool biblical = ChurchYearCalendar.IsBiblicalCommemoration(feast);
        bool saturated = IsSaturated();

        if (martyr)
        {
            return new List<CrisisCardChoice>
            {
                new(
                    "Public commemoration",
                    "Adherence +8, fame +2, Gospel +4",
                    () => ApplyFeastEffects(law: -2f, gospel: 4f, adherence: 8f, fame: 2, turn, feast)),
                new(
                    saturated ? "Colloquy under pressure (4 mss)" : "Quiet prayer & alms",
                    saturated ? "Law +4, Gospel +4, adherence +6, -4 mss" : "Gospel +6, adherence +4",
                    () =>
                    {
                        if (saturated && (FirstSteps.Instance?.scriptureManuscripts ?? 0) < 4)
                            ApplyFeastEffects(law: 2f, gospel: 5f, adherence: 3f, fame: 0, turn, feast);
                        else
                        {
                            if (saturated)
                                FirstSteps.Instance.ScriptureManuscripts -= 4;
                            ApplyFeastEffects(law: saturated ? 4f : -1f, gospel: saturated ? 4f : 6f,
                                adherence: saturated ? 6f : 4f, fame: saturated ? 1 : 0, turn, feast);
                        }
                    })
            };
        }

        if (biblical)
        {
            return new List<CrisisCardChoice>
            {
                new(
                    "Preach the narrative",
                    "Gospel +8, adherence +5",
                    () => ApplyFeastEffects(law: -3f, gospel: 8f, adherence: 5f, fame: 1, turn, feast)),
                new(
                    "Discipline the synod",
                    "Law +8, adherence +4",
                    () => ApplyFeastEffects(law: 8f, gospel: -2f, adherence: 4f, fame: 0, turn, feast))
            };
        }

        return new List<CrisisCardChoice>
        {
            new(
                "Keep the feast",
                "Gospel +5, adherence +3",
                () => ApplyFeastEffects(law: -1f, gospel: 5f, adherence: 3f, fame: 1, turn, feast)),
            new(
                "Press the mission",
                "Law +4, adherence +2, fame +1",
                () => ApplyFeastEffects(law: 4f, gospel: -1f, adherence: 2f, fame: 1, turn, feast))
        };
    }

    static void ApplyFeastEffects(
        float law, float gospel, float adherence, int fame, int turn, ChurchYearEntry feast)
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + law, 0f, 100f);
        faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + gospel, 0f, 100f);
        faction.confessionalAdherence = Mathf.Clamp(
            faction.confessionalAdherence + adherence,
            faction.EffectiveMinAdherenceFloor,
            100f);
        if (fame > 0)
            faction.AddFame(fame);

        Debug.Log($"Turn {turn}: Church-year decision for {feast.Name}.");
    }

    void ApplyDeferredJudgment()
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        faction.confessionalAdherence = Mathf.Clamp(
            faction.confessionalAdherence - 2f,
            faction.EffectiveMinAdherenceFloor,
            100f);
    }

    bool TryShowImmediate(string title, string body, IReadOnlyList<CrisisCardChoice> choices)
    {
        if (CrisisCardPanel.Instance == null)
            return false;
        if (!CrisisCardPanel.Instance.Show(title, body, choices, this))
            return false;

        IsAwaitingPlayerChoice = true;
        pendingTitle = title;
        pendingBody = body;
        pendingChoices = choices is List<CrisisCardChoice> list
            ? list
            : new List<CrisisCardChoice>(choices);
        TurnPhaseBanner.Instance?.Refresh("Church-year feast — choose the synod's witness.");
        return true;
    }

    IEnumerator RetryPresentDeferred(string title, string body, List<CrisisCardChoice> choices, string feastKey)
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
                resolvedFeasts.Add(feastKey);
                deferredPresentRoutine = null;
                yield break;
            }
        }

        deferredPresentRoutine = null;
    }
}
