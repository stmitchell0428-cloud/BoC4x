using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Staged crises with interactive cards before schism (Decisions 5/19).</summary>
public class CrisisManager : MonoBehaviour
{
    public static CrisisManager Instance { get; private set; }

    const float DriftRumblings = 58f;
    const float DriftTension = 52f;
    const float DriftBreaking = 46f;
    const int BreakingTurnsBeforeForcedSchism = 2;
    const int SchismPressurePerTurn = 5;
    const int SchismPressureThreshold = 70;

    int schismPressure;
    int breakingTurnsUnresolved;

    public CrisisType ActiveCrisis { get; private set; } = CrisisType.None;
    public CrisisStage Stage { get; private set; } = CrisisStage.None;
    public int StageTurns { get; private set; }
    public bool IsAwaitingPlayerChoice { get; private set; }

    bool cardShownThisStage;
    CrisisStage pendingStageForCard;
    Coroutine deferredPresentRoutine;

    struct PendingCardPresentation
    {
        public string Title;
        public string Body;
        public List<CrisisCardChoice> Choices;
    }

    PendingCardPresentation? pendingCard;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
    }

    public void NotifyCardDismissed() => IsAwaitingPlayerChoice = false;

    public void CancelPendingCardChoice()
    {
        IsAwaitingPlayerChoice = false;
        cardShownThisStage = false;
        CrisisCardPanel.Instance?.Hide();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
    }

    public void EnsurePendingCrisisCardVisible()
    {
        if (IsAwaitingPlayerChoice || ActiveCrisis == CrisisType.None)
            return;

        if (cardShownThisStage)
            return;

        switch (ActiveCrisis)
        {
            case CrisisType.Legalism:
                PresentLegalismCard();
                break;
            case CrisisType.Antinomian:
                PresentAntinomianCard();
                break;
            case CrisisType.DoctrinalDrift when Stage != CrisisStage.None:
                TryPresentDriftCard(Stage);
                break;
        }
    }

    bool TryPresentCard(string title, string body, List<CrisisCardChoice> choices)
    {
        if (CrisisCardPanel.Instance == null)
        {
            Debug.LogError("Crisis card could not open  -  CrisisCardPanel missing.");
            cardShownThisStage = false;
            return false;
        }

        if (TryShowCardImmediate(title, body, choices))
            return true;

        pendingCard = new PendingCardPresentation
        {
            Title = title,
            Body = body,
            Choices = choices
        };

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
        deferredPresentRoutine = StartCoroutine(RetryPresentCardDeferred());

        return false;
    }

    bool TryShowCardImmediate(string title, string body, IReadOnlyList<CrisisCardChoice> choices)
    {
        if (CrisisCardPanel.Instance == null)
            return false;

        if (!CrisisCardPanel.Instance.Show(title, body, choices))
            return false;

        IsAwaitingPlayerChoice = true;
        cardShownThisStage = true;
        pendingCard = null;
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
        return true;
    }

    IEnumerator RetryPresentCardDeferred()
    {
        var pending = pendingCard;
        if (!pending.HasValue)
            yield break;

        for (int i = 0; i < 8; i++)
        {
            yield return null;
            if (i == 0)
                yield return new WaitForEndOfFrame();

            if (IsAwaitingPlayerChoice || !pending.HasValue)
                yield break;

            var card = pending.Value;
            if (TryShowCardImmediate(card.Title, card.Body, card.Choices))
            {
                cardShownThisStage = true;
                deferredPresentRoutine = null;
                yield break;
            }
        }

        deferredPresentRoutine = null;
        cardShownThisStage = false;
        pendingCard = null;
        Debug.LogError("Crisis card could not open after deferred retries.");
    }

    public void OnPlayerTurnEnded()
    {
        if (IsAwaitingPlayerChoice)
            return;

        if (ActiveCrisis is CrisisType.Legalism or CrisisType.Antinomian)
            return;

        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;
        bool hasCapital = CityManager.Instance?.GetPrimaryPlayerCity() != null;
        if (hasCapital || turn >= 10)
            EvaluateDoctrinalDrift(faction);

        TickSchismPressure(faction, hasCapital);
    }

    void TickSchismPressure(FirstSteps faction, bool hasCapital)
    {
        if (SchismManager.Instance != null && SchismManager.Instance.HasSchismed)
            return;

        schismPressure += hasCapital ? SchismPressurePerTurn : 3;
        if (schismPressure < SchismPressureThreshold)
            return;

        schismPressure = 0;
        if (IsAwaitingPlayerChoice || ActiveCrisis != CrisisType.None)
            return;

        NudgeTowardCrisis(faction);
        TryTriggerPressureCrisis(faction);
    }

    void TryTriggerPressureCrisis(FirstSteps faction)
    {
        if (IsAwaitingPlayerChoice || ActiveCrisis != CrisisType.None)
            return;

        if (faction.civicRestraint > 68f && faction.spiritualComfort < 45f)
            QueueLegalismCrisis();
        else if (faction.spiritualComfort > 62f && faction.confessionalAdherence < 68f)
            QueueAntinomianCrisis();
        else if (faction.ConfessionalAdherence <= DriftRumblings)
            EvaluateDoctrinalDrift(faction);
    }

    void NudgeTowardCrisis(FirstSteps faction)
    {
        float floor = faction.EffectiveMinAdherenceFloor;
        if (faction.spiritualComfort >= faction.civicRestraint)
        {
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + 10f, 0f, 100f);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 8f, floor, 100f);
        }
        else
        {
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + 10f, 0f, 100f);
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort - 8f, 0f, 100f);
        }

        Debug.LogWarning("Synod tension peaked  -  dissent pressure forces a crisis.");
    }

    public void QueueLegalismCrisis()
    {
        if (IsAwaitingPlayerChoice)
            return;

        if (ActiveCrisis == CrisisType.Legalism)
        {
            if (!cardShownThisStage)
                PresentLegalismCard();
            return;
        }

        if (ActiveCrisis != CrisisType.None)
            return;

        SetCrisis(CrisisType.Legalism, CrisisStage.Breaking);
        PresentLegalismCard();
    }

    public void QueueAntinomianCrisis()
    {
        if (IsAwaitingPlayerChoice)
            return;

        if (ActiveCrisis == CrisisType.Antinomian)
        {
            if (!cardShownThisStage)
                PresentAntinomianCard();
            return;
        }

        if (ActiveCrisis != CrisisType.None)
            return;

        SetCrisis(CrisisType.Antinomian, CrisisStage.Breaking);
        PresentAntinomianCard();
    }

    public void HandleLegalismCrisis(bool hadGuard)
    {
        if (hadGuard)
        {
            SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.GerhardLegacy);
            FirstSteps.Instance?.AddFame(8);
            ClearCrisis();
            return;
        }

        if (ActiveCrisis != CrisisType.None)
            return;

        QueueLegalismCrisis();
    }

    public void HandleAntinomianCrisis(bool hadGuard)
    {
        if (hadGuard)
        {
            SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.ConcordLegacy);
            FirstSteps.Instance?.AddFame(8);
            ClearCrisis();
            return;
        }

        if (ActiveCrisis != CrisisType.None)
            return;

        QueueAntinomianCrisis();
    }

    void EvaluateDoctrinalDrift(FirstSteps faction)
    {
        if (ActiveCrisis is CrisisType.Legalism or CrisisType.Antinomian)
            return;

        if (faction.ConfessionalAdherence > DriftRumblings)
        {
            if (ActiveCrisis == CrisisType.DoctrinalDrift)
            {
                if (Stage >= CrisisStage.Tension)
                    SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.CrisisSurvivor);
                ClearCrisis();
            }
            return;
        }

        var newStage = CrisisStage.Rumblings;
        if (faction.ConfessionalAdherence <= DriftBreaking)
            newStage = CrisisStage.Breaking;
        else if (faction.ConfessionalAdherence <= DriftTension)
            newStage = CrisisStage.Tension;

        if (ActiveCrisis != CrisisType.DoctrinalDrift)
        {
            SetCrisis(CrisisType.DoctrinalDrift, newStage);
            TryPresentDriftCard(newStage);
            return;
        }

        if (newStage > Stage)
        {
            Stage = newStage;
            StageTurns = 0;
            breakingTurnsUnresolved = 0;
            cardShownThisStage = false;
            TryPresentDriftCard(newStage);
        }

        if (IsAwaitingPlayerChoice)
            return;

        StageTurns++;
        ApplyStagePressure(faction, Stage);

        if (Stage == CrisisStage.Breaking)
        {
            breakingTurnsUnresolved++;
            if (breakingTurnsUnresolved >= BreakingTurnsBeforeForcedSchism)
            {
                ResolveSchism(
                    PickHeresy(CrisisType.DoctrinalDrift),
                    "Delegates departed while the synod hesitated  -  doctrinal dissent schisms.");
                return;
            }
        }

        if (Stage == CrisisStage.Breaking && !IsAwaitingPlayerChoice && !cardShownThisStage)
            TryPresentDriftCard(CrisisStage.Breaking);
    }

    void TryPresentDriftCard(CrisisStage stage)
    {
        if (IsAwaitingPlayerChoice)
            return;

        if (cardShownThisStage && pendingStageForCard == stage)
            return;

        pendingStageForCard = stage;
        PresentDriftCard(stage);
    }

    void PresentLegalismCard()
    {
        var choices = new List<CrisisCardChoice>
        {
            new("Concede discipline", "Pop -2, Law -10, Gospel +8  -  hold the synod",
                () => ResolveLegalismConcede()),
            new("Public debate", "+6 adherence, -5 comfort  -  65% schism risk",
                () => ResolveLegalismDebate()),
            new("Ignore complaints", "Pharisaic party breaks away",
                () => ResolveSchism(HeresyType.Legalism, "Legalistic preaching drove a dissenting party from the synod."))
        };

        TryPresentCard(
            "<color=#FF8866>Crisis  -  Legalism</color>",
            "Civic restraint has crushed gospel comfort. Pastors report empty pews and harsh catechism classes.\n\n" +
            "<i>How will the synod respond?</i>",
            choices);
    }

    void PresentAntinomianCard()
    {
        var choices = new List<CrisisCardChoice>
        {
            new("Pastoral counsel", "Pop halved, +12 adherence, comfort reset  -  painful reunion",
                () => ResolveAntinomianCounsel()),
            new("Synod rebuke", "+8 adherence, fame +4  -  55% schism risk",
                () => ResolveAntinomianRebuke()),
            new("Let them depart", "Libertine congregation schisms",
                () => ResolveSchism(HeresyType.Antinomian, "Antinomian fracture  -  a schismatic party broke away."))
        };

        TryPresentCard(
            "<color=#FF8866>Crisis  -  Antinomian drift</color>",
            "Spiritual comfort runs high while confessional adherence collapses. Some preach grace without repentance.\n\n" +
            "<i>How will the synod respond?</i>",
            choices);
    }

    void PresentDriftCard(CrisisStage stage)
    {
        if (stage == CrisisStage.Rumblings)
        {
            var choices = new List<CrisisCardChoice>
            {
                new("Catechism review", "+4 adherence, +5 restraint",
                    () => ResolveDriftRecovery(4f, 5f, 0f)),
                new("Open forum", "+3 comfort, -2 restraint",
                    () => ResolveDriftRecovery(2f, -2f, 3f)),
                new("Press on", "Drift continues",
                    () => ResolveDriftContinue())
            };

            TryPresentCard(
                "<color=#FFAA66>Crisis  -  Doctrinal rumblings</color>",
                "Whispers in the assembly: \"Is our confession still pure?\" Adherence is slipping.\n\n" +
                "<i>Address the rumblings now, or risk escalation.</i>",
                choices);
            return;
        }

        if (stage == CrisisStage.Tension)
        {
            var choices = new List<CrisisCardChoice>
            {
                new("Synod assembly", "+6 adherence, -3 comfort",
                    () => ResolveDriftRecovery(6f, 0f, -3f)),
                new("Mission emphasis", "+5 comfort, +2 adherence",
                    () => ResolveDriftRecovery(2f, -3f, 5f)),
                new("Do nothing", "Pressure mounts (-1 adherence/turn)",
                    () => ResolveDriftContinue())
            };

            TryPresentCard(
                "<color=#FF8844>Crisis  -  Doctrinal tension</color>",
                "Factional letters circulate. District pastors pick sides. The synod teeters.\n\n" +
                "<i>One strong move may steady the church  -  or deepen the rift.</i>",
                choices);
            return;
        }

        var breakingChoices = new List<CrisisCardChoice>
        {
            new("Final appeal", "+6 adherence  -  35% chance to reunite",
                () => ResolveDriftFinalAppeal()),
            new("Let them go", "Controlled schism  -  smaller split",
                () => ResolveControlledSchism()),
            new("Ignore the split", "Full schism at breaking point",
                () => ResolveSchism(
                    PickHeresy(CrisisType.DoctrinalDrift),
                    $"Doctrinal drift reached breaking point (adherence {FirstSteps.Instance?.ConfessionalAdherence:F0}%)."))
        };

        TryPresentCard(
            "<color=#FF6644>Crisis  -  Breaking point</color>",
            "The synod cannot hold. Delegates pack their bags. A dissenting capital will be founded unless you act now.\n\n" +
            "<i>This is the last chance before schism.</i>",
            breakingChoices);
    }

    void ResolveLegalismConcede()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            PopulationSync.ApplyDeltaToPrimaryCity(-2);
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint - 10f, 0f, 100f);
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + 8f, 0f, 100f);
        }
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveLegalismDebate()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 6f, 0f, 100f);
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort - 5f, 0f, 100f);
        }

        if (Random.value < 0.65f)
            ResolveSchism(HeresyType.Legalism, "Debate over law and gospel failed  -  Pharisaic synod schisms.");
        else
        {
            schismPressure = Mathf.Max(0, schismPressure - 20);
            SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.CrisisSurvivor);
            ClearCrisis();
        }

        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveAntinomianCounsel()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            int loss = Mathf.Max(1, PopulationSync.SumSynodPopulation() / 2);
            PopulationSync.ApplyLossAcrossPlayerCities(loss);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 12f, 0f, 100f);
            faction.spiritualComfort = 40f;
        }
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveAntinomianRebuke()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 8f, 0f, 100f);

        if (Random.value < 0.55f)
            ResolveSchism(HeresyType.Antinomian, "Synod rebuke provoked antinomian schism.");
        else
        {
            schismPressure = Mathf.Max(0, schismPressure - 20);
            FirstSteps.Instance?.AddFame(4);
            ClearCrisis();
        }

        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveDriftRecovery(float adherence, float restraint, float comfort)
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + adherence, 0f, 100f);
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + restraint, 0f, 100f);
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + comfort, 0f, 100f);
        }

        if (faction != null && faction.ConfessionalAdherence > DriftRumblings)
        {
            schismPressure = Mathf.Max(0, schismPressure - 15);
            ClearCrisis();
        }
        else
            cardShownThisStage = true;

        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveDriftContinue()
    {
        schismPressure += 4;
        cardShownThisStage = true;
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveDriftFinalAppeal()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 6f, 0f, 100f);

        if (Random.value < 0.35f)
        {
            schismPressure = Mathf.Max(0, schismPressure - 20);
            SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.CrisisSurvivor);
            ClearCrisis();
        }
        else
        {
            ResolveSchism(
                PickHeresy(CrisisType.DoctrinalDrift),
                "Final appeal failed  -  doctrinal dissent schisms.");
        }

        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveControlledSchism()
    {
        var heresy = PickHeresy(CrisisType.DoctrinalDrift);
        SchismManager.Instance?.TryTriggerSchism(
            heresy,
            "Controlled separation  -  dissenting party withdrew with less turmoil.",
            controlledSplit: true);
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveSchism(HeresyType heresy, string reason)
    {
        SchismManager.Instance?.TryTriggerSchism(heresy, reason);
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    HeresyType PickHeresy(CrisisType crisis)
    {
        bool isRepeat = SchismManager.Instance != null && SchismManager.Instance.SchismCount > 0;
        return SchismaticBlocRegistry.Instance?.PickHeresyForCrisis(crisis, isRepeat)
               ?? HeresyDatabase.ForCrisis(crisis);
    }

    void ApplyStagePressure(FirstSteps faction, CrisisStage stage)
    {
        switch (stage)
        {
            case CrisisStage.Rumblings:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 1f, 0f, 100f);
                break;
            case CrisisStage.Tension:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 2.5f, 0f, 100f);
                break;
            case CrisisStage.Breaking:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 4f, 0f, 100f);
                break;
        }
    }

    void SetCrisis(CrisisType type, CrisisStage stage)
    {
        ActiveCrisis = type;
        Stage = stage;
        StageTurns = 0;
        cardShownThisStage = false;
        pendingStageForCard = stage;
        Debug.LogWarning($"Crisis: {type}  -  stage {stage}");
    }

    void ClearCrisis()
    {
        ActiveCrisis = CrisisType.None;
        Stage = CrisisStage.None;
        StageTurns = 0;
        breakingTurnsUnresolved = 0;
        cardShownThisStage = false;
        IsAwaitingPlayerChoice = false;
        CrisisCardPanel.Instance?.Hide();
    }

    public string FormatCrisisLine()
    {
        if (IsAwaitingPlayerChoice)
            return "<color=#FFAA66><b>Crisis card pending  -  choose a response</b></color>";

        if (ActiveCrisis == CrisisType.None || Stage == CrisisStage.None)
            return "";

        return ActiveCrisis switch
        {
            CrisisType.DoctrinalDrift =>
                $"<color=#FFAA66>Crisis ({Stage}): doctrinal drift  -  preach and guard adherence</color>",
            CrisisType.Legalism =>
                "<color=#FF6644>Crisis: legalism schism</color>",
            CrisisType.Antinomian =>
                "<color=#FF6644>Crisis: antinomian schism</color>",
            _ => ""
        };
    }
}
