using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Smalcald Catalog and orthodox patristic testimony colloquies at tech unlock.</summary>
public class TestimonyColloquyManager : MonoBehaviour, IChoiceCardPresenter
{
    public static TestimonyColloquyManager Instance { get; private set; }

    readonly HashSet<ConfessionTechId> resolvedTechColloquies = new();
    readonly HashSet<string> resolvedOneShotKeys = new();
    readonly Queue<ConfessionTechId> pendingTechColloquies = new();
    string pendingLibraryCityName;
    bool libraryColloquyResolved;
    int libraryPatristicBriefingScheduledTurn = -1;
    int libraryPatristicBriefingByTurn = -1;
    bool libraryPatristicBriefingPending;
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

        ChoiceCardQueue.Register(ChoiceCardQueue.OrderTestimony, TryPresentTurnStartColloquy);
    }

    bool TryPresentTurnStartColloquy()
    {
        if (BlocksOtherEvents())
            return false;

        TryPresentPending();
        return IsAwaitingPlayerChoice;
    }

    public void OnTechUnlocked(ConfessionTechId id)
    {
        if (id == ConfessionTechId.SmalcaldArticles)
            QueueTechColloquy(id);
        else if (id == ConfessionTechId.MartinChemnitz)
            QueueTechColloquy(id);
        else if (id == ConfessionTechId.JohannGerhard)
            QueueTechColloquy(id);
    }

    public void OnLibraryBuilt(City city)
    {
        if (city == null || city.Faction != FactionId.LutheranSynod)
            return;
        if (resolvedOneShotKeys.Contains("library"))
            return;

        pendingLibraryCityName = city.CityName;
    }

    void QueueTechColloquy(ConfessionTechId id)
    {
        if (resolvedTechColloquies.Contains(id))
            return;

        pendingTechColloquies.Enqueue(id);
    }

    void TryPresentPending()
    {
        if (IsAwaitingPlayerChoice || BlocksOtherEvents())
            return;

        if (!string.IsNullOrEmpty(pendingLibraryCityName) && !resolvedOneShotKeys.Contains("library"))
        {
            if (TurnManager.Instance?.IsPlayerTurn == true)
            {
                PresentLibraryArchive(pendingLibraryCityName);
                pendingLibraryCityName = null;
                return;
            }
        }

        while (pendingTechColloquies.Count > 0)
        {
            var id = pendingTechColloquies.Dequeue();
            if (resolvedTechColloquies.Contains(id))
                continue;

            if (PresentTechColloquy(id))
                return;
        }
    }

    static bool BlocksOtherEvents() => ChoiceCardBlocking.BlocksOtherEvents();

    bool PresentTechColloquy(ConfessionTechId id)
    {
        switch (id)
        {
            case ConfessionTechId.SmalcaldArticles:
                PresentSmalcaldCatalog(id);
                return true;
            case ConfessionTechId.MartinChemnitz:
                PresentChemnitzTestimony(id);
                return true;
            case ConfessionTechId.JohannGerhard:
                PresentGerhardTestimony(id);
                return true;
            default:
                return false;
        }
    }

    public void OnChoiceCardDismissed()
    {
        IsAwaitingPlayerChoice = false;
        pendingTitle = null;
        pendingBody = null;
        pendingChoices = null;
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
    }

    public void OnChoiceCardCancelled()
    {
        var faction = FirstSteps.Instance;
        if (faction != null)
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - 2f, faction.EffectiveMinAdherenceFloor, 100f);

        IsAwaitingPlayerChoice = false;
        pendingTitle = null;
        pendingBody = null;
        pendingChoices = null;
        TurnPhaseBanner.Instance?.Refresh("Synod deferred the testimony colloquy.");
    }

    public void EnsurePendingColloquyVisible()
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

    void PresentSmalcaldCatalog(ConfessionTechId id)
    {
        string body =
            "Luther appended the <i>Catalog of Testimonies</i> to the Smalcald Articles — patristic witnesses " +
            "on Trinity, person of Christ, and justification.\n\n" +
            "<i>\"We do not recite the fathers to replace Scripture, but to show the church has always confessed thus.\"</i>\n\n" +
            "<size=13><color=#AABBCC>— Martin Luther (1483-1546)</color></size>";
        body = TestimonyCitation.Append(body, TestimonyCitation.SmalcaldCatalog);

        var choices = new List<CrisisCardChoice>
        {
            new(
                "Foreground Augustine on grace",
                "Gospel +8, adherence +6, Law -4 · " + TestimonyCitation.AugustineSpiritLetter,
                () => ApplyTestimony(-4f, 8f, 6f, fame: 2, mssCost: 0, "Augustine on grace")),
            new(
                "Foreground Chrysostom on preaching",
                "Law +6, Gospel +4, adherence +5 · " + TestimonyCitation.ChrysostomJohn,
                () => ApplyTestimony(6f, 4f, 5f, fame: 1, mssCost: 0, "Chrysostom on preaching"))
        };

        ShowColloquy("<color=#88CCFF>Catalog of Testimonies</color>", body, id, null, choices);
    }

    void PresentChemnitzTestimony(ConfessionTechId id)
    {
        string body =
            "Chemnitz receives the fathers as witnesses under Scripture, not judges over it.\n\n" +
            "<i>\"The fathers are not the foundation, but they are faithful witnesses when they speak from the Word.\"</i>\n\n" +
            "<size=13><color=#AABBCC>— Martin Chemnitz (1524-1586)</color></size>";
        body = TestimonyCitation.Append(body, TestimonyCitation.ChemnitzTrent);

        var choices = new List<CrisisCardChoice>
        {
            new(
                "Answer Rome with patristic proof",
                "Adherence +8, Law +4, -2 mss",
                () => ApplyTestimony(4f, 2f, 8f, fame: 0, mssCost: 2, "Chemnitz patristic proof")),
            new(
                "Study colloquy (4 mss)",
                "Gospel +6, adherence +10, -4 mss",
                () => ApplyTestimony(-2f, 6f, 10f, fame: 2, mssCost: 4, "Chemnitz study colloquy"))
        };

        ShowColloquy("<color=#88CCFF>Patristic Reception — Chemnitz</color>", body, id, null, choices);
    }

    void PresentGerhardTestimony(ConfessionTechId id)
    {
        string body =
            "Gerhard's <i>Loci</i> weave patristic citations into systematic exposition — testimony in service of the loci.\n\n" +
            "<i>\"The church's teachers are read with gratitude and tested by the prophetic and apostolic writings.\"</i>\n\n" +
            "<size=13><color=#AABBCC>— Johann Gerhard (1582-1637)</color></size>";
        body = TestimonyCitation.Append(body, TestimonyCitation.GerhardLoci);

        var choices = new List<CrisisCardChoice>
        {
            new(
                "Archive patristic loci",
                "Adherence +10, Gospel +4 · Gerhard's Loci Guard (softens legalism crises, −15% Law/Gospel drift)",
                () => ApplyGerhardArchivePatristicLoci()),
            new(
                "Bind catechism to fathers (3 mss)",
                "Civic Restraint (Law) +5, adherence +8, −3 mss",
                () => ApplyTestimony(5f, 3f, 8f, fame: 1, mssCost: 3, "Gerhard catechism binding"))
        };

        ShowColloquy("<color=#88CCFF>Patristic Loci — Gerhard</color>", body, id, null, choices);
    }

    void PresentLibraryArchive(string cityName)
    {
        string body =
            $"{cityName}'s <i>Confessional Library</i> opens — loci, confessions, and patristic sources shelved for study.\n\n" +
            "<i>\"Ignorance of Scripture is ignorance of Christ, but the fathers teach us how the church read Scripture in earlier ages.\"</i>\n\n" +
            "<size=13><color=#AABBCC>— St. Jerome (347-420), via Gerhard's loci tradition</color></size>";
        body = TestimonyCitation.Append(body, TestimonyCitation.JeromeScripture);

        var choices = new List<CrisisCardChoice>
        {
            new(
                "Catalog patristic testimonies",
                "Adherence +6; patristic briefings unlocked",
                () =>
                {
                    ApplyTestimony(2f, 3f, 6f, fame: 2, mssCost: 0, "Library patristic catalog");
                    ResolveLibraryColloquy();
                    ScheduleLibraryPatristicBriefing();
                }),
            new(
                "Study colloquy (3 mss)",
                "Adherence +10, Gospel +4, -3 mss",
                () =>
                {
                    ApplyTestimony(-1f, 4f, 10f, fame: 1, mssCost: 3, "Library study colloquy");
                    ResolveLibraryColloquy();
                    ScheduleLibraryPatristicBriefing();
                })
        };

        ShowColloquy("<color=#88CCFF>Confessional Library — Patristic Archive</color>", body, null, "library", choices);
    }

    void ApplyGerhardArchivePatristicLoci()
    {
        ApplyTestimony(-2f, 4f, 10f, fame: 3, mssCost: 0, "Gerhard patristic loci");
        if (SynodLegacyManager.Instance?.TryAward(SynodLegacyTraitId.GerhardLegacy) == true)
            Debug.Log("Gerhard's Loci Guard legacy awarded via testimony colloquy.");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
    }

    void ResolveLibraryColloquy() => libraryColloquyResolved = true;

    void ScheduleLibraryPatristicBriefing()
    {
        int turn = TurnManager.Instance?.TurnNumber ?? 1;
        libraryPatristicBriefingScheduledTurn = turn;
        libraryPatristicBriefingByTurn = turn + 3;
        libraryPatristicBriefingPending = true;
    }

    public bool ShouldOfferLibraryPatristicBriefing(int turn)
    {
        if (!libraryPatristicBriefingPending)
            return false;
        return turn > libraryPatristicBriefingScheduledTurn && turn <= libraryPatristicBriefingByTurn;
    }

    public void ConsumeLibraryPatristicBriefing() => libraryPatristicBriefingPending = false;

    void ShowColloquy(
        string title,
        string body,
        ConfessionTechId? techId,
        string oneShotKey,
        List<CrisisCardChoice> choices)
    {
        body = ChurchYearFlavor.EnrichEventBody(body, saturatedEmphasis: false);
        if (TryShowImmediate(title, body, choices))
        {
            if (techId.HasValue)
                resolvedTechColloquies.Add(techId.Value);
            if (!string.IsNullOrEmpty(oneShotKey))
                resolvedOneShotKeys.Add(oneShotKey);
            return;
        }

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
        deferredPresentRoutine = StartCoroutine(RetryPresentDeferred(title, body, choices, techId, oneShotKey));
    }

    void ApplyTestimony(float law, float gospel, float adherence, int fame, int mssCost, string label)
    {
        var faction = FirstSteps.Instance;
        if (faction == null)
            return;

        if (mssCost > 0 && faction.scriptureManuscripts < mssCost)
        {
            faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + 4f, 0f, 100f);
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 3f, faction.EffectiveMinAdherenceFloor, 100f);
            Debug.LogWarning($"Testimony colloquy {label}: insufficient manuscripts — partial reception only.");
            return;
        }

        if (mssCost > 0)
            faction.scriptureManuscripts -= mssCost;

        faction.civicRestraint = Mathf.Clamp(faction.civicRestraint + law, 0f, 100f);
        faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + gospel, 0f, 100f);
        faction.confessionalAdherence = Mathf.Clamp(
            faction.confessionalAdherence + adherence,
            faction.EffectiveMinAdherenceFloor,
            100f);
        if (fame > 0)
            faction.AddFame(fame);

        Debug.Log($"Testimony colloquy: {label}.");
    }

    bool TryShowImmediate(string title, string body, IReadOnlyList<CrisisCardChoice> choices)
    {
        if (CrisisCardPanel.Instance == null || CrisisCardPanel.Instance.IsVisible)
            return false;
        if (BlocksOtherEvents())
            return false;
        if (!CrisisCardPanel.Instance.Show(title, body, choices, this))
            return false;

        IsAwaitingPlayerChoice = true;
        pendingTitle = title;
        pendingBody = body;
        pendingChoices = choices is List<CrisisCardChoice> list
            ? list
            : new List<CrisisCardChoice>(choices);
        TurnPhaseBanner.Instance?.Refresh("Testimony colloquy — choose patristic reception.");
        return true;
    }

    IEnumerator RetryPresentDeferred(
        string title,
        string body,
        List<CrisisCardChoice> choices,
        ConfessionTechId? techId,
        string oneShotKey)
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
                if (techId.HasValue)
                    resolvedTechColloquies.Add(techId.Value);
                if (!string.IsNullOrEmpty(oneShotKey))
                    resolvedOneShotKeys.Add(oneShotKey);
                deferredPresentRoutine = null;
                yield break;
            }
        }

        deferredPresentRoutine = null;
    }

    public static bool PatristicTestimonyUnlocked()
    {
        var research = ConfessionResearchManager.Instance;
        if (research == null)
            return false;

        return research.IsTechUnlocked(ConfessionTechId.SmalcaldArticles) ||
               research.IsTechUnlocked(ConfessionTechId.MartinChemnitz) ||
               research.IsTechUnlocked(ConfessionTechId.JohannGerhard) ||
               (Instance != null && Instance.libraryColloquyResolved);
    }
}
