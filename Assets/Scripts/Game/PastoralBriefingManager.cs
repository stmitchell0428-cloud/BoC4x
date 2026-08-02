using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Occasional Law/Gospel pastoral choices with historical quotes at turn start.</summary>
public class PastoralBriefingManager : MonoBehaviour, IChoiceCardPresenter
{
    public static PastoralBriefingManager Instance { get; private set; }

    const int MinTurn = 6;
    const int CooldownTurns = 4;
    const int PeriodicInterval = 7;

    readonly HashSet<int> recentlyUsedIndices = new();
    int lastBriefingTurn = -999;
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

        if (CrisisManager.Instance != null && CrisisManager.Instance.IsAwaitingPlayerChoice)
            return;

        if (ChoiceCardBlocking.BlocksOtherEvents())
            return;

        TryOfferBriefing();
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
        TurnPhaseBanner.Instance?.Refresh();
    }

    public void EnsurePendingBriefingVisible()
    {
        if (!IsAwaitingPlayerChoice)
            return;

        if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
        {
            CrisisCardPanel.Instance.BringToFront();
            return;
        }

        if (pendingChoices != null && pendingChoices.Count > 0)
            TryShowImmediate(pendingTitle, pendingBody, pendingChoices);
    }

    void ClearPendingCard()
    {
        pendingTitle = null;
        pendingBody = null;
        pendingChoices = null;
    }

    public string FormatStatusLine()
    {
        if (!IsAwaitingPlayerChoice)
            return "";

        return "<color=#88CCFF><b>Pastoral briefing</b>  -  choose Law/Gospel emphasis (Esc defers)</color>";
    }

    void TryOfferBriefing()
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;
        if (turn < MinTurn)
            return;

        if (turn - lastBriefingTurn < CooldownTurns &&
            (TestimonyColloquyManager.Instance == null ||
             !TestimonyColloquyManager.Instance.ShouldOfferLibraryPatristicBriefing(turn)))
            return;

        if (CrisisManager.Instance != null &&
            CrisisManager.Instance.ActiveCrisis != CrisisType.None &&
            CrisisManager.Instance.IsAwaitingPlayerChoice)
            return;

        if (LiturgicalEventManager.Instance != null && LiturgicalEventManager.Instance.IsAwaitingPlayerChoice)
            return;

        if (TestimonyColloquyManager.Instance != null && TestimonyColloquyManager.Instance.IsAwaitingPlayerChoice)
            return;

        if (!ShouldOfferBriefing(turn, faction))
            return;

        var situation = ClassifySituation(faction, turn);
        var entry = PastoralBriefingDatabase.PickForSituation(situation, recentlyUsedIndices, out int entryIndex, turn);
        recentlyUsedIndices.Add(entryIndex);
        if (recentlyUsedIndices.Count > 6)
            recentlyUsedIndices.Clear();

        PresentBriefing(entry, turn);
    }

    bool ShouldOfferBriefing(int turn, FirstSteps faction)
    {
        float imbalance = Mathf.Abs(faction.civicRestraint - faction.spiritualComfort);
        bool urgent = imbalance > 18f || faction.confessionalAdherence < 62f;
        bool periodic = turn - lastBriefingTurn >= PeriodicInterval;
        bool saturated = SchismaticBlocRegistry.Instance != null &&
                         SchismaticBlocRegistry.Instance.ActiveCount >= SchismaticBlocRegistry.MaxBlocs;
        bool feastWindow = LiturgicalEventManager.Instance != null &&
                           LiturgicalEventManager.Instance.HasRecentFeastSpawn(turn);

        if (TestimonyColloquyManager.Instance != null &&
            TestimonyColloquyManager.Instance.ShouldOfferLibraryPatristicBriefing(turn))
            return true;

        if (feastWindow && Random.value < 0.78f)
            return true;

        if (saturated && Random.value < 0.45f)
            return true;

        if (urgent)
            return Random.value < 0.82f;

        if (periodic)
            return Random.value < 0.58f;

        return false;
    }

    static PastoralBriefingSituation ClassifySituation(FirstSteps faction, int turn)
    {
        if (TestimonyColloquyManager.Instance != null &&
            TestimonyColloquyManager.Instance.ShouldOfferLibraryPatristicBriefing(turn))
            return PastoralBriefingSituation.PatristicWitness;

        if (ChurchYearCalendar.TryGetMartyrInTurnWindow(turn, out _) &&
            ChurchYearCalendar.IsChurchYearActive)
            return PastoralBriefingSituation.MartyrFeast;

        if (TestimonyColloquyManager.PatristicTestimonyUnlocked() && Random.value < 0.35f)
            return PastoralBriefingSituation.PatristicWitness;

        if (SchismaticBlocRegistry.Instance != null &&
            SchismaticBlocRegistry.Instance.ActiveCount >= SchismaticBlocRegistry.MaxBlocs)
            return PastoralBriefingSituation.SchismSaturation;

        if (CityManager.Instance?.GetPrimaryPlayerCity() == null)
            return PastoralBriefingSituation.Nomadic;

        var fieldUnit = faction.GetFieldSynodUnit();
        if (fieldUnit != null &&
            HexGridMap.Instance != null &&
            HexGridMap.Instance.TryGetTile(fieldUnit.HexPosition, out var tile) &&
            !TerrainRules.IsWater(tile.Terrain) &&
            HexGridMap.GameplayTerrainCategory(tile.Terrain) == "Wilderness")
            return PastoralBriefingSituation.Wilderness;

        if (faction.confessionalAdherence < 62f)
            return PastoralBriefingSituation.AdherenceLow;

        float law = faction.civicRestraint;
        float gospel = faction.spiritualComfort;
        if (law - gospel > 14f)
            return PastoralBriefingSituation.LawHeavy;
        if (gospel - law > 14f)
            return PastoralBriefingSituation.GospelHeavy;

        return PastoralBriefingSituation.Balanced;
    }

    void PresentBriefing(PastoralBriefingEntry entry, int turn)
    {
        var choices = new List<CrisisCardChoice>
        {
            new(
                entry.ChoiceA.Label,
                entry.ChoiceA.Description,
                () => ApplyChoice(entry.ChoiceA, turn)),
            new(
                entry.ChoiceB.Label,
                entry.ChoiceB.Description,
                () => ApplyChoice(entry.ChoiceB, turn))
        };

        string title = $"<color=#88CCFF>Pastoral Briefing  -  {entry.Author}</color>";
        string body = PastoralBriefingDatabase.FormatBody(entry);

        if (TryShowImmediate(title, body, choices))
            return;

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
        deferredPresentRoutine = StartCoroutine(RetryPresentDeferred(title, body, choices, turn));
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
        lastBriefingTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh("Pastoral briefing — choose Law/Gospel emphasis.");
        return true;
    }

    IEnumerator RetryPresentDeferred(string title, string body, List<CrisisCardChoice> choices, int turn)
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
                deferredPresentRoutine = null;
                yield break;
            }
        }

        deferredPresentRoutine = null;
        Debug.LogWarning("Pastoral briefing could not open after deferred retries.");
    }

    void ApplyChoice(PastoralBriefingChoice choice, int turn)
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        if (choice.ManuscriptCost > 0 && faction.scriptureManuscripts < choice.ManuscriptCost)
        {
            TurnPhaseBanner.Instance?.Refresh("Not enough manuscripts for colloquy — hard rebuke instead.");
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + 8f, 0f, 100f);
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort - 5f, 0f, 100f);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 3f, faction.EffectiveMinAdherenceFloor, 100f);
        }
        else
        {
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

            if (choice.ReinforceRivalBloc)
                SchismManager.Instance?.ReinforceWeakestBloc("Pastoral concession fed dissent abroad.");
        }

        Debug.Log(
            $"Turn {turn}: Pastoral briefing — {choice.Label}. " +
            $"Law {faction.civicRestraint:F0} | Gospel {faction.spiritualComfort:F0} | Adherence {faction.confessionalAdherence:F0}%");
    }

    void ApplyDeferredJudgment()
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 2f, faction.EffectiveMinAdherenceFloor, 100f);
        TurnPhaseBanner.Instance?.Refresh("Synod deferred judgment — adherence slipped.");
        Debug.LogWarning("Pastoral briefing dismissed without choice — synod deferred judgment.");
    }

}
