using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Staged crises with interactive cards before schism (Decisions 5/19).</summary>
public class CrisisManager : MonoBehaviour, IChoiceCardPresenter
{
    public static CrisisManager Instance { get; private set; }

    const float DriftRumblings = 58f;
    const float DriftTension = 52f;
    const float DriftBreaking = 46f;
    const int BreakingTurnsBeforeForcedSchism = 2;
    const int SchismPressurePerTurn = 8;
    const int SchismPressureThreshold = 58;

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

    public void NotifyCardDismissed()
    {
        IsAwaitingPlayerChoice = false;
        pendingCard = null;
    }

    public void OnChoiceCardDismissed() => NotifyCardDismissed();

    public void OnChoiceCardCancelled() => CancelPendingCardChoice();

    public void CancelPendingCardChoice()
    {
        IsAwaitingPlayerChoice = false;
        cardShownThisStage = false;
        pendingCard = null;
        CrisisCardPanel.Instance?.Hide();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
    }

    public void EnsurePendingCrisisCardVisible()
    {
        if (ActiveCrisis == CrisisType.None)
            return;

        // Awaiting but panel hidden (covered / dismissed UI race) — restore from last card.
        if (IsAwaitingPlayerChoice)
        {
            if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
            {
                CrisisCardPanel.Instance.BringToFront();
                return;
            }

            if (pendingCard.HasValue)
            {
                var card = pendingCard.Value;
                TryShowCardImmediate(card.Title, card.Body, card.Choices);
            }

            return;
        }

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

        body = ChurchYearFlavor.EnrichEventBody(body, saturatedEmphasis: IsSchismSaturated);

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

        if (!CrisisCardPanel.Instance.Show(title, body, choices, this))
            return false;

        IsAwaitingPlayerChoice = true;
        cardShownThisStage = true;
        // Keep a copy so End Turn can re-show if another modal covered the panel.
        pendingCard = new PendingCardPresentation
        {
            Title = title,
            Body = body,
            Choices = choices is List<CrisisCardChoice> list
                ? list
                : new List<CrisisCardChoice>(choices)
        };
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
        int activeBlocs = SchismaticBlocRegistry.Instance?.ActiveCount ?? 0;
        if (activeBlocs >= SchismaticBlocRegistry.MaxBlocs)
            return;

        int perTurn = hasCapital ? SchismPressurePerTurn : 3;
        if (activeBlocs > 0)
            perTurn += activeBlocs;

        bool complacent = faction.civicRestraint > 85f && faction.spiritualComfort > 85f;
        if (complacent)
            perTurn += 4;

        if (faction.ConfessionalAdherence > 80f && faction.civicRestraint > 75f && faction.spiritualComfort > 75f)
            perTurn += 3;

        schismPressure += perTurn;

        float floor = faction.EffectiveMinAdherenceFloor;
        if (activeBlocs > 0)
        {
            faction.confessionalAdherence = Mathf.Clamp(
                faction.confessionalAdherence - activeBlocs * 0.65f, floor, 100f);
        }

        if (complacent)
        {
            faction.confessionalAdherence = Mathf.Clamp(
                faction.confessionalAdherence - 1f, floor, 100f);
        }

        int threshold = activeBlocs > 0 ? 85 : SchismPressureThreshold;
        if (schismPressure < threshold)
            return;

        schismPressure = 0;
        if (IsAwaitingPlayerChoice || ActiveCrisis != CrisisType.None)
            return;

        NudgeTowardCrisis(faction);
        TryTriggerPressureCrisis(faction, activeBlocs, complacent);
    }

    void TryTriggerPressureCrisis(FirstSteps faction)
    {
        int activeBlocs = SchismaticBlocRegistry.Instance?.ActiveCount ?? 0;
        bool complacent = faction.civicRestraint > 85f && faction.spiritualComfort > 85f;
        TryTriggerPressureCrisis(faction, activeBlocs, complacent);
    }

    void TryTriggerPressureCrisis(FirstSteps faction, int activeBlocs, bool complacent)
    {
        if (IsAwaitingPlayerChoice || ActiveCrisis != CrisisType.None)
            return;

        if (faction.civicRestraint > 68f && faction.spiritualComfort < 45f)
            QueueLegalismCrisis();
        else if (faction.spiritualComfort > 62f && faction.confessionalAdherence < 68f)
            QueueAntinomianCrisis();
        else if (faction.ConfessionalAdherence <= DriftRumblings)
            EvaluateDoctrinalDrift(faction);
        else if (activeBlocs > 0 && faction.ConfessionalAdherence <= 75f)
            EvaluateDoctrinalDrift(faction);
        else if (activeBlocs > 0)
        {
            if (complacent)
            {
                float floor = faction.EffectiveMinAdherenceFloor;
                faction.confessionalAdherence = Mathf.Clamp(
                    faction.confessionalAdherence - 4f, floor, 100f);
            }

            if (ActiveCrisis == CrisisType.DoctrinalDrift)
            {
                EvaluateDoctrinalDrift(faction);
                return;
            }

            if (faction.ConfessionalAdherence > DriftRumblings)
            {
                SetCrisis(CrisisType.DoctrinalDrift, CrisisStage.Rumblings);
                TryPresentDriftCard(CrisisStage.Rumblings);
            }
            else
                EvaluateDoctrinalDrift(faction);
        }
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

    static bool IsSchismSaturated =>
        SchismaticBlocRegistry.Instance != null &&
        SchismaticBlocRegistry.Instance.ActiveCount >= SchismaticBlocRegistry.MaxBlocs;

    void PresentLegalismCard()
    {
        if (IsSchismSaturated)
        {
            var choices = new List<CrisisCardChoice>
            {
                new("Concede discipline", "Pop -2, Law -10, Gospel +8  -  hold the synod",
                    () => ResolveLegalismConcede()),
                new("Public debate", "+6 adherence, -5 comfort  -  feeds strongest dissent",
                    () => ResolveSaturatedRiskChoice(
                        HeresyType.Legalism,
                        "Debate over law and gospel fed an existing dissenting party.")),
                new("Ignore complaints", "Unrest joins an existing dissenting synod",
                    () => ResolveSchism(HeresyType.Legalism, "Legalistic preaching drove unrest into an existing dissent."))
            };

            TryPresentCard(
                "<color=#FF8866>Crisis  -  Legalism (saturated)</color>",
                "Civic restraint has crushed gospel comfort — and three dissenting synods already stand abroad. " +
                "There is no fourth capital left to found.\n\n" +
                "<i>How will the synod absorb this unrest?</i>",
                choices);
            return;
        }

        var openChoices = new List<CrisisCardChoice>
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
            openChoices);
    }

    void PresentAntinomianCard()
    {
        if (IsSchismSaturated)
        {
            var choices = new List<CrisisCardChoice>
            {
                new("Pastoral counsel", "Pop halved, +12 adherence, comfort reset  -  painful reunion",
                    () => ResolveAntinomianCounsel()),
                new("Synod rebuke", "+8 adherence  -  feeds strongest dissent (no new schism)",
                    () => ResolveSaturatedRiskChoice(
                        HeresyType.Antinomian,
                        "Synod rebuke drove unrest into an existing antinomian party.")),
                new("Let them depart", "Libertines join an existing dissenting synod",
                    () => ResolveSchism(HeresyType.Antinomian, "Antinomian fracture joined an existing dissent."))
            };

            TryPresentCard(
                "<color=#FF8866>Crisis  -  Antinomian drift (saturated)</color>",
                "Grace without repentance stirs again — but the land already bears three dissenting capitals. " +
                "A walkout strengthens sisters in error; it cannot found a fourth.\n\n" +
                "<i>How will the synod respond?</i>",
                choices);
            return;
        }

        var openChoices = new List<CrisisCardChoice>
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
            openChoices);
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

        if (stage == CrisisStage.Breaking)
        {
            if (IsSchismSaturated)
            {
                var saturatedBreaking = new List<CrisisCardChoice>
                {
                    new("Final appeal", "+6 adherence  -  35% calm unrest; else feed dissent",
                        () => ResolveDriftFinalAppeal()),
                    new("Channel the split", "Unrest reinforces an existing dissenting synod",
                        () => ResolveControlledSchism()),
                    new("Ignore the split", "Overflow into existing dissent",
                        () => ResolveSchism(
                            PickHeresy(CrisisType.DoctrinalDrift),
                            $"Doctrinal drift overflowed (adherence {FirstSteps.Instance?.ConfessionalAdherence:F0}%)."))
                };

                TryPresentCard(
                    "<color=#FF6644>Crisis  -  Breaking point (saturated)</color>",
                    "The synod cannot hold — yet three dissenting capitals already stand. " +
                    "This unrest will strengthen sisters in error, not found a fourth synod.\n\n" +
                    "<i>Choose how the overflow is absorbed.</i>",
                    saturatedBreaking);
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
            return;
        }
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

        if (IsSchismSaturated || Random.value < 0.65f)
            ResolveSchism(HeresyType.Legalism, "Debate over law and gospel failed  -  Pharisaic synod schisms.");
        else
        {
            schismPressure = Mathf.Max(0, schismPressure - 20);
            SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.CrisisSurvivor);
            ClearCrisis();
        }

        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveAntinomianRebuke()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 8f, 0f, 100f);

        if (IsSchismSaturated || Random.value < 0.55f)
            ResolveSchism(HeresyType.Antinomian, "Synod rebuke provoked antinomian schism.");
        else
        {
            schismPressure = Mathf.Max(0, schismPressure - 20);
            FirstSteps.Instance?.AddFame(4);
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

    /// <summary>At max blocs: always apply adherence buff then overflow — never a free "no schism" win.</summary>
    void ResolveSaturatedRiskChoice(HeresyType heresy, string reason)
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 8f, 0f, 100f);

        UnionStrifeManager.AddStrife(12);
        ResolveSchism(heresy, reason);
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
        else if (IsSchismSaturated)
        {
            UnionStrifeManager.AddStrife(10);
            ResolveSchism(
                PickHeresy(CrisisType.DoctrinalDrift),
                "Final appeal failed — unrest joined an existing dissent.");
        }
        else
        {
            ResolveSchism(
                PickHeresy(CrisisType.DoctrinalDrift),
                "Final appeal failed  -  doctrinal dissent schisms.");
        }

        FirstSteps.Instance?.RefreshDashboard();
    }

    void PresentDissentOverflowCard(HeresyType heresy, string reason)
    {
        var registry = SchismaticBlocRegistry.Instance;
        var targetBloc = registry?.PickBlocForHeresy(heresy) ?? registry?.PickWeakestBloc();
        string blocLabel = "a dissenting synod";
        if (targetBloc != null && registry != null && registry.TryGetBloc(targetBloc.Value, out var record))
            blocLabel = record.CapitalName;

        var weakest = registry?.PickWeakestBloc();
        bool canReconcile = weakest != null && UnionStrifeManager.CanOfferReconciliation(weakest.Value);

        var choices = new List<CrisisCardChoice>
        {
            new(
                "Colloquy",
                "6 mss: Law +3, Gospel +3, adherence +4; strife eases",
                () => ResolveOverflowColloquy()),
            new(
                $"Feed {blocLabel}",
                "Rival grows near you (+pop, +raid unit); adherence -6",
                () => ResolveOverflowReinforce(targetBloc, reason)),
            new(
                "Internal purge",
                "Pop -3, Law +12, Gospel -8, adherence +3",
                () => ResolveOverflowPurge())
        };

        if (canReconcile)
        {
            string weakName = registry.TryGetBloc(weakest.Value, out var weakRec)
                ? weakRec.CapitalName
                : "weak dissent";
            choices.Add(new(
                $"Reconcile {weakName}",
                "8 mss + adherence check — dissolve weak bloc or they grow stronger",
                () => ResolveOverflowReconcile(weakest.Value)));
        }

        TryPresentCard(
            "<color=#FF8844>Crisis  -  Dissent without schism</color>",
            "Three dissenting synods already stand abroad. The land cannot bear a fourth capital — " +
            "unrest boils within the synod and strengthens sisters in error.\n\n" +
            $"<i>{reason}</i>\n\n" +
            "Choose how the synod absorbs the overflow.",
            choices);
    }

    void ResolveOverflowColloquy()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            if (faction.scriptureManuscripts >= 6)
                faction.scriptureManuscripts -= 6;
            else
            {
                PopulationSync.ApplyDeltaToPrimaryCity(-2);
                faction.scriptureManuscripts = 0;
            }

            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + 3f, 0f, 100f);
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + 3f, 0f, 100f);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 4f, 0f, 100f);
        }

        schismPressure = Mathf.Max(0, schismPressure - 12);
        UnionStrifeManager.AddStrife(-8);
        SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.CrisisSurvivor);
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveOverflowReinforce(SchismaticBlocId? blocId, string reason)
    {
        if (blocId != null)
            SchismManager.Instance?.ReinforceExistingBloc(blocId.Value, reason, nearPlayer: true);
        else
            SchismManager.Instance?.ReinforceWeakestBloc(reason);

        UnionStrifeManager.AddStrife(8);
        var faction = FirstSteps.Instance;
        if (faction != null)
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 6f, 0f, 100f);

        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveOverflowPurge()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
        {
            PopulationSync.ApplyDeltaToPrimaryCity(-3);
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + 12f, 0f, 100f);
            faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort - 8f, 0f, 100f);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 3f, 0f, 100f);
        }

        schismPressure = Mathf.Max(0, schismPressure - 8);
        UnionStrifeManager.AddStrife(6);
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveOverflowReconcile(SchismaticBlocId blocId)
    {
        UnionStrifeManager.TryReconcileBloc(blocId);
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveControlledSchism()
    {
        var heresy = PickHeresy(CrisisType.DoctrinalDrift);
        var registry = SchismaticBlocRegistry.Instance;
        if (registry != null && registry.PickBlocForHeresy(heresy) is SchismaticBlocId existingBloc)
        {
            ResolveSameHeresyReinforcement(
                existingBloc,
                "Controlled separation sought — dissent rejoined an existing party of the same mind.");
            return;
        }

        if (registry != null && registry.ActiveCount >= SchismaticBlocRegistry.MaxBlocs)
        {
            PresentDissentOverflowCard(
                heresy,
                "Controlled separation sought — but no fourth dissent capital remains.");
            return;
        }

        SchismManager.Instance?.TryTriggerSchism(
            heresy,
            "Controlled separation  -  dissenting party withdrew with less turmoil.",
            controlledSplit: true);
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveSameHeresyReinforcement(SchismaticBlocId blocId, string reason)
    {
        SchismManager.Instance?.ReinforceExistingBloc(blocId, reason, nearPlayer: true);
        UnionStrifeManager.AddStrife(8);
        ClearCrisis();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ResolveSchism(HeresyType heresy, string reason)
    {
        var registry = SchismaticBlocRegistry.Instance;
        if (registry != null)
        {
            if (registry.PickBlocForHeresy(heresy) is SchismaticBlocId existingBloc)
            {
                ResolveSameHeresyReinforcement(existingBloc, reason);
                return;
            }

            if (registry.ActiveCount >= SchismaticBlocRegistry.MaxBlocs)
            {
                PresentDissentOverflowCard(heresy, reason);
                return;
            }
        }

        if (SchismManager.Instance?.TryTriggerSchism(heresy, reason) == true)
        {
            ClearCrisis();
            FirstSteps.Instance?.RefreshDashboard();
            return;
        }

        PresentDissentOverflowCard(heresy, reason);
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

        if ((ActiveCrisis == CrisisType.Legalism || ActiveCrisis == CrisisType.Antinomian) &&
            SchismaticBlocRegistry.Instance != null &&
            SchismaticBlocRegistry.Instance.ActiveCount >= SchismaticBlocRegistry.MaxBlocs)
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
