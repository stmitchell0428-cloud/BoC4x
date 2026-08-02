using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tier 2 confessional + culture emphasis with match-conditioned card options.
/// </summary>
public class Tier2EmphasisManager : MonoBehaviour, IChoiceCardPresenter
{
    public static Tier2EmphasisManager Instance { get; private set; }

    public const float SecondaryPotency = 0.5f;
    public const float TertiaryPotency = 0.5f;
    public const float IntegrationPotencyBoost = 0.25f;

    public static int SecondaryManuscriptCost => EraBranchRules.ColloquyCostForTier(2);
    public static int IntegrationManuscriptCost =>
        EraBranchRules.ColloquyCostForTier(
            ConfessionTechDatabase.Get(ConfessionalIntegrationUnlockTech).Tier);

    public static ConfessionTechId ConfessionalSecondaryUnlockTech => ConfessionTechId.LargeCatechism;
    public static ConfessionTechId ConfessionalIntegrationUnlockTech => ConfessionTechId.SynodicalGovernance;
    public static ConfessionTechId CultureIntegrationUnlockTech => ConfessionTechId.CTCRReports;

    ConfessionalEmphasisChoice confessionalPrimary = ConfessionalEmphasisChoice.None;
    ConfessionalEmphasisChoice confessionalSecondary = ConfessionalEmphasisChoice.None;
    ConfessionalEmphasisChoice confessionalTertiary = ConfessionalEmphasisChoice.None;
    ConfessionsCultureEmphasisChoice culturePrimary = ConfessionsCultureEmphasisChoice.None;
    ConfessionsCultureEmphasisChoice cultureSecondary = ConfessionsCultureEmphasisChoice.None;
    ConfessionsCultureEmphasisChoice cultureTertiary = ConfessionsCultureEmphasisChoice.None;

    bool confessionalIntegrated;
    bool cultureIntegrated;

    int confessionalSecondaryCooldownUntilTurn = -1;
    int cultureSecondaryCooldownUntilTurn = -1;
    int confessionalIntegrationCooldownUntilTurn = -1;
    int cultureIntegrationCooldownUntilTurn = -1;
    int confessionalTertiaryCooldownUntilTurn = -1;

    bool pendingConfessionalChoice;
    bool pendingCultureChoice;
    string pendingTitle;
    string pendingBody;
    List<CrisisCardChoice> pendingChoices;
    bool pendingIsConfessional;
    Coroutine deferredPresentRoutine;

    public bool IsAwaitingPlayerChoice => pendingConfessionalChoice || pendingCultureChoice;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
    }

    public void OnChoiceCardDismissed()
    {
        // Keep awaiting + stored card so End Turn / T can re-show (same pattern as synodical).
        pendingConfessionalChoice = false;
        pendingCultureChoice = false;
        ClearPendingCard();
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
    }

    public void OnChoiceCardCancelled()
    {
        if (pendingConfessionalChoice && confessionalPrimary == ConfessionalEmphasisChoice.None)
            ApplyConfessionalPrimary(ConfessionalEmphasisChoice.InternalFormula, deferred: true);

        if (pendingCultureChoice && culturePrimary == ConfessionsCultureEmphasisChoice.None)
            ApplyCulturePrimary(ConfessionsCultureEmphasisChoice.ChoraleLiturgy, deferred: true);

        pendingConfessionalChoice = false;
        pendingCultureChoice = false;
        ClearPendingCard();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh("Synod deferred — default emphasis applied.");
    }

    public void OnTechUnlocked(ConfessionTechId id)
    {
        if (id == ConfessionTechId.ConfessionalEmphasis)
            PresentConfessionalPrimaryChoice();
        else if (id == ConfessionTechId.ConfessionsCultureEmphasis)
            PresentCulturePrimaryChoice();
        else if (id == ConfessionalSecondaryUnlockTech)
            TryPresentConfessionalSecondaryChoice();
        else if (id == ConfessionTechId.ChoraleTradition || id == ConfessionTechId.PaulGerhardt)
            TryPresentCultureSecondaryChoice();
        else if (id == ConfessionalIntegrationUnlockTech)
            TryPresentConfessionalIntegrationChoice();
        else if (id == CultureIntegrationUnlockTech)
            TryPresentCultureIntegrationChoice();
    }

    public void EnsurePendingChoicesVisible()
    {
        if (IsAwaitingPlayerChoice)
        {
            if (CrisisCardPanel.Instance != null && CrisisCardPanel.Instance.IsVisible)
            {
                CrisisCardPanel.Instance.BringToFront();
                SynodBriefPanel.Instance?.Hide();
                return;
            }

            if (pendingChoices != null && pendingChoices.Count > 0 &&
                !string.IsNullOrEmpty(pendingTitle))
            {
                TryShowImmediate(pendingTitle, pendingBody, pendingChoices, pendingIsConfessional, secondary: false);
                SynodBriefPanel.Instance?.Hide();
                return;
            }

            // Stale awaiting with no stored card — clear and re-offer.
            pendingConfessionalChoice = false;
            pendingCultureChoice = false;
            ClearPendingCard();
        }

        var research = ConfessionResearchManager.Instance;
        if (research != null)
        {
            if (confessionalPrimary == ConfessionalEmphasisChoice.None &&
                research.IsTechUnlocked(ConfessionTechId.ConfessionalEmphasis))
            {
                PresentConfessionalPrimaryChoice();
                return;
            }

            if (culturePrimary == ConfessionsCultureEmphasisChoice.None &&
                research.IsTechUnlocked(ConfessionTechId.ConfessionsCultureEmphasis))
            {
                PresentCulturePrimaryChoice();
                return;
            }

            if (research.IsTechUnlocked(ConfessionalSecondaryUnlockTech))
                TryPresentConfessionalSecondaryChoice();

            if (research.IsTechUnlocked(ConfessionTechId.ChoraleTradition) ||
                research.IsTechUnlocked(ConfessionTechId.PaulGerhardt))
                TryPresentCultureSecondaryChoice();
        }

        TryPresentConfessionalIntegrationChoice();
        TryPresentCultureIntegrationChoice();
        TryPresentConfessionalTertiaryChoice();
    }

    void ClearPendingCard()
    {
        pendingTitle = null;
        pendingBody = null;
        pendingChoices = null;
    }

    public bool OwnsConfessionalEmphasis(ConfessionalEmphasisChoice choice) =>
        confessionalPrimary == choice ||
        confessionalSecondary == choice ||
        confessionalTertiary == choice;

    public bool OwnsCultureEmphasis(ConfessionsCultureEmphasisChoice choice) =>
        culturePrimary == choice ||
        cultureSecondary == choice ||
        cultureTertiary == choice;

    public bool IsConfessionalIntegrated => confessionalIntegrated;
    public bool IsCultureIntegrated => cultureIntegrated;

    float ConfessionalSecondaryPotency =>
        confessionalIntegrated
            ? Mathf.Min(1f, SecondaryPotency + IntegrationPotencyBoost)
            : SecondaryPotency;

    float CultureSecondaryPotency =>
        cultureIntegrated
            ? Mathf.Min(1f, SecondaryPotency + IntegrationPotencyBoost)
            : SecondaryPotency;

    public ConfessionModifiers GetEmphasisModifiers(float adherencePotency)
    {
        var combined = new ConfessionModifiers();
        MergeConfessionalChoice(combined, confessionalPrimary, 1f, adherencePotency);
        MergeConfessionalChoice(combined, confessionalSecondary, ConfessionalSecondaryPotency, adherencePotency);
        MergeConfessionalChoice(combined, confessionalTertiary, TertiaryPotency, adherencePotency);
        MergeCultureChoice(combined, culturePrimary, 1f, adherencePotency);
        MergeCultureChoice(combined, cultureSecondary, CultureSecondaryPotency, adherencePotency);
        MergeCultureChoice(combined, cultureTertiary, TertiaryPotency, adherencePotency);
        return combined;
    }

    public string FormatStatusLine()
    {
        if (IsAwaitingPlayerChoice)
            return "<color=#88CCFF><b>Confessions emphasis</b>  -  choose your path</color>";

        var parts = new List<string>();
        if (confessionalPrimary != ConfessionalEmphasisChoice.None)
        {
            string confession = $"Confession: {ConfessionalLabel(confessionalPrimary)} ({FormatPotency(1f)})";
            if (confessionalSecondary != ConfessionalEmphasisChoice.None)
            {
                string secondaryTag = confessionalIntegrated ? "integrated" : "secondary";
                confession += $" + {ConfessionalLabel(confessionalSecondary)} ({FormatPotency(ConfessionalSecondaryPotency)}, {secondaryTag})";
            }

            if (confessionalTertiary != ConfessionalEmphasisChoice.None)
                confession += $" + {ConfessionalLabel(confessionalTertiary)} ({FormatPotency(TertiaryPotency)}, tertiary)";

            parts.Add(confession);
        }

        if (culturePrimary != ConfessionsCultureEmphasisChoice.None)
        {
            string culture = $"Culture: {CultureLabel(culturePrimary)} ({FormatPotency(1f)})";
            if (cultureSecondary != ConfessionsCultureEmphasisChoice.None)
            {
                string secondaryTag = cultureIntegrated ? "integrated" : "secondary";
                culture += $" + {CultureLabel(cultureSecondary)} ({FormatPotency(CultureSecondaryPotency)}, {secondaryTag})";
            }

            if (cultureTertiary != ConfessionsCultureEmphasisChoice.None)
                culture += $" + {CultureLabel(cultureTertiary)} ({FormatPotency(TertiaryPotency)}, tertiary)";

            parts.Add(culture);
        }

        string forkLine = ConfessionResearchManager.Instance?.FormatEraForkStatusLine();

        if (parts.Count == 0 && string.IsNullOrEmpty(forkLine))
            return "";

        var lines = new List<string>();
        if (parts.Count > 0)
            lines.Add($"<color=#AABBCC>Emphasis  -  {string.Join("  |  ", parts)}</color>");
        if (!string.IsNullOrEmpty(forkLine))
            lines.Add(forkLine);

        return string.Join("\n", lines);
    }

    static bool IsBeforeTurn(int cooldownUntilTurn) =>
        TurnManager.Instance != null && TurnManager.Instance.TurnNumber < cooldownUntilTurn;

    static void DeferColloquy(ref int cooldownUntilTurn)
    {
        int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;
        cooldownUntilTurn = turn + EraBranchRules.ColloquyDeferTurns;
    }

    static string NotNowLabel =>
        $"Not now (offered again in {EraBranchRules.ColloquyDeferTurns} turns)";

    void PresentConfessionalPrimaryChoice()
    {
        if (confessionalPrimary != ConfessionalEmphasisChoice.None)
            return;

        var choices = new List<CrisisCardChoice>
        {
            new(
                "Internal — Formula",
                "Antinomian guard; bind disputes at home",
                () => ApplyConfessionalPrimary(ConfessionalEmphasisChoice.InternalFormula))
        };

        string bodyExtra = "";
        var history = MatchHistory.Instance;
        bool hasSchism = SchismaticBlocRegistry.Instance != null && SchismaticBlocRegistry.Instance.HasAnySchism;
        bool offerAugsburg = history != null && history.CanOfferAugsburgConfessionalEmphasis();
        bool offerSmalcald = history != null && history.CanOfferSmalcaldConfessionalEmphasis();

        if (offerAugsburg)
        {
            choices.Add(new(
                "External — Augsburg",
                "Civic restraint (Law) +8%; public evangelical confession",
                () => ApplyConfessionalPrimary(ConfessionalEmphasisChoice.AugsburgPublic)));
        }

        if (offerSmalcald)
        {
            choices.Add(new(
                "External — Smalcald",
                "Wilderness +1 manuscript; polemic against dissent",
                () => ApplyConfessionalPrimary(ConfessionalEmphasisChoice.SmalcaldPolemic)));
        }

        if (!hasSchism)
        {
            bodyExtra =
                "\n\n<size=12><color=#AABBCC><i>No schismatic bloc has broken away yet — external confession can wait.</i></color></size>";
        }
        else if (!offerAugsburg && !offerSmalcald)
        {
            bodyExtra =
                "\n\n<size=12><color=#AABBCC><i>Dissent exists abroad. <b>Augsburg</b> emphasis awaits scout contact; " +
                "<b>Smalcald</b> awaits battle with a schismatic bloc.</i></color></size>";
        }
        else if (!offerAugsburg)
        {
            bodyExtra =
                "\n\n<size=12><color=#AABBCC><i><b>Augsburg</b> emphasis awaits scout sight of dissent " +
                "(visible unit or explored capital).</i></color></size>";
        }
        else if (!offerSmalcald)
        {
            bodyExtra =
                "\n\n<size=12><color=#AABBCC><i><b>Smalcald</b> emphasis awaits combat with a schismatic bloc.</i></color></size>";
        }

        var catechismName = ConfessionTechDatabase.Get(ConfessionalSecondaryUnlockTech).Name;
        PresentCard(
            "<color=#88CCFF>Confessional Emphasis</color>",
            "Which confession leads the synod in this era?\n\n" +
            "<b>All confession techs remain researchable.</b> This sets which emphasis receives full bonuses.\n\n" +
            $"<size=12><color=#AABBCC>After <b>{catechismName}</b>, other emphases can be adopted for " +
            $"{ConfessionalUiVocabulary.FormatColloquySecondaryCost(SecondaryManuscriptCost)}</color></size>" +
            bodyExtra,
            choices,
            confessional: true);
    }

    void PresentCulturePrimaryChoice()
    {
        if (culturePrimary != ConfessionsCultureEmphasisChoice.None)
            return;

        var choices = new List<CrisisCardChoice>
        {
            new(
                "Chorale — liturgical order",
                "Settlement adherence decay -20%",
                () => ApplyCulturePrimary(ConfessionsCultureEmphasisChoice.ChoraleLiturgy))
        };

        string bodyExtra = "";
        if (MatchHistory.Instance != null && MatchHistory.Instance.HasPlayerCombat)
        {
            choices.Add(new(
                "Gerhardt — cross & comfort",
                "+5 spiritual comfort each turn",
                () => ApplyCulturePrimary(ConfessionsCultureEmphasisChoice.GerhardtCross)));
        }
        else
        {
            bodyExtra =
                "\n\n<size=12><color=#AABBCC><i>The synod has not yet borne battle — hymnody stays ordered " +
                "until war or schism teaches the cross.</i></color></size>";
        }

        PresentCard(
            "<color=#88CCFF>Confessions Culture Emphasis</color>",
            "How shall the synod sing through this era?\n\n" +
            "<b>Sacred Hymnody and Chorale Tradition remain researchable.</b>\n\n" +
            $"<size=12><color=#AABBCC>After <b>Chorale Tradition</b> or <b>Sacred Hymnody</b>, the other emphasis " +
            $"can be adopted for {ConfessionalUiVocabulary.FormatColloquySecondaryCost(SecondaryManuscriptCost)}</color></size>" +
            bodyExtra,
            choices,
            confessional: false);
    }

    void TryPresentConfessionalSecondaryChoice()
    {
        if (confessionalPrimary == ConfessionalEmphasisChoice.None ||
            confessionalSecondary != ConfessionalEmphasisChoice.None ||
            IsAwaitingPlayerChoice ||
            IsBeforeTurn(confessionalSecondaryCooldownUntilTurn))
            return;

        var pool = BuildConfessionalSecondaryPool();
        if (pool.Count == 0)
            return;

        var choices = new List<CrisisCardChoice>();
        foreach (var option in pool)
        {
            choices.Add(new(
                ConfessionalLabel(option) + " (secondary)",
                ConfessionalDescription(option) + $"  —  {ConfessionalUiVocabulary.FormatColloquySecondaryCost(SecondaryManuscriptCost)}",
                () => ApplyConfessionalSecondary(option)));
        }

        choices.Add(new(NotNowLabel, "Keep current emphasis only", () => DeferColloquy(ref confessionalSecondaryCooldownUntilTurn)));

        PresentCard(
            "<color=#88CCFF>Confessional Colloquy</color>",
            $"Having completed <b>{ConfessionTechDatabase.Get(ConfessionalSecondaryUnlockTech).Name}</b>, " +
            "the synod may integrate another confessional emphasis at reduced strength.\n\n" +
            $"<size=12><color=#AABBCC><i>Dismissing hides this card for {EraBranchRules.ColloquyDeferTurns} turns.</i></color></size>",
            choices,
            confessional: true,
            secondary: true);
    }

    void TryPresentCultureSecondaryChoice()
    {
        if (culturePrimary == ConfessionsCultureEmphasisChoice.None ||
            cultureSecondary != ConfessionsCultureEmphasisChoice.None ||
            IsAwaitingPlayerChoice ||
            IsBeforeTurn(cultureSecondaryCooldownUntilTurn))
            return;

        var pool = BuildCultureSecondaryPool();
        if (pool.Count == 0)
            return;

        var choices = new List<CrisisCardChoice>();
        foreach (var option in pool)
        {
            choices.Add(new(
                CultureLabel(option) + " (secondary)",
                CultureDescription(option) + $"  —  {ConfessionalUiVocabulary.FormatColloquySecondaryCost(SecondaryManuscriptCost)}",
                () => ApplyCultureSecondary(option)));
        }

        choices.Add(new(NotNowLabel, "Keep current culture emphasis", () => DeferColloquy(ref cultureSecondaryCooldownUntilTurn)));

        PresentCard(
            "<color=#88CCFF>Culture Colloquy</color>",
            "Parish song and hymnody deepen — the synod may weave in the other tradition at reduced strength.\n\n" +
            $"<size=12><color=#AABBCC><i>Dismissing hides this card for {EraBranchRules.ColloquyDeferTurns} turns.</i></color></size>",
            choices,
            confessional: false,
            secondary: true);
    }

    void TryPresentConfessionalIntegrationChoice()
    {
        if (confessionalPrimary == ConfessionalEmphasisChoice.None ||
            confessionalSecondary == ConfessionalEmphasisChoice.None ||
            confessionalIntegrated ||
            IsAwaitingPlayerChoice ||
            IsBeforeTurn(confessionalIntegrationCooldownUntilTurn))
            return;

        if (ConfessionResearchManager.Instance == null ||
            !ConfessionResearchManager.Instance.IsTechUnlocked(ConfessionalIntegrationUnlockTech))
            return;

        var unlockName = ConfessionTechDatabase.Get(ConfessionalIntegrationUnlockTech).Name;
        int cost = EraBranchRules.ColloquyCostForTier(
            ConfessionTechDatabase.Get(ConfessionalIntegrationUnlockTech).Tier);
        var choices = new List<CrisisCardChoice>
        {
            new(
                "Integrate confessions",
                $"Deepen secondary reception to {FormatPotency(SecondaryPotency + IntegrationPotencyBoost)}; " +
                $"{ConfessionalUiVocabulary.FormatReopenEraForkSiblings()}; adopt tertiary emphasis  —  {cost} mss",
                ApplyConfessionalIntegration),
            new(NotNowLabel, "Keep secondary reception unchanged", () => DeferColloquy(ref confessionalIntegrationCooldownUntilTurn))
        };

        PresentCard(
            "<color=#88CCFF>Confessional Integration</color>",
            $"Having completed <b>{unlockName}</b>, the synod may bind primary and secondary confessional emphases " +
            "into one living voice. Weaker lines deepen by 25% (cap full pastoral weight).\n\n" +
            $"Deferred era paths in the same track reopen for <b>{ConfessionalUiVocabulary.PartialReception}</b>. " +
            $"Beginning research on a deferred path requires a study colloquy ({ConfessionalUiVocabulary.DeepenedReception}). " +
            "Completing both paths in the same era branch grants full reception. " +
            $"The third confessional emphasis may be adopted as {ConfessionalUiVocabulary.SecondaryReception}.\n\n" +
            $"<size=12><color=#AABBCC><i>Dismissing hides this card for {EraBranchRules.ColloquyDeferTurns} turns.</i></color></size>",
            choices,
            confessional: true,
            secondary: true);
    }

    void TryPresentCultureIntegrationChoice()
    {
        if (culturePrimary == ConfessionsCultureEmphasisChoice.None ||
            cultureSecondary == ConfessionsCultureEmphasisChoice.None ||
            cultureIntegrated ||
            IsAwaitingPlayerChoice ||
            IsBeforeTurn(cultureIntegrationCooldownUntilTurn))
            return;

        if (ConfessionResearchManager.Instance == null ||
            !ConfessionResearchManager.Instance.IsTechUnlocked(CultureIntegrationUnlockTech))
            return;

        var unlockName = ConfessionTechDatabase.Get(CultureIntegrationUnlockTech).Name;
        int cost = EraBranchRules.ColloquyCostForTier(
            ConfessionTechDatabase.Get(CultureIntegrationUnlockTech).Tier);
        var choices = new List<CrisisCardChoice>
        {
            new(
                "Integrate hymnody",
                $"Deepen secondary reception to {FormatPotency(SecondaryPotency + IntegrationPotencyBoost)}; " +
                $"{ConfessionalUiVocabulary.FormatReopenEraForkSiblings()}  —  {cost} mss",
                ApplyCultureIntegration),
            new(NotNowLabel, "Keep secondary reception unchanged", () => DeferColloquy(ref cultureIntegrationCooldownUntilTurn))
        };

        PresentCard(
            "<color=#88CCFF>Culture Integration</color>",
            $"Having completed <b>{unlockName}</b>, chorale and cross-comfort may be sung as one parish life. " +
            "Weaker emphasis lines deepen by 25% (cap full pastoral weight).\n\n" +
            $"Deferred culture-era paths reopen for <b>{ConfessionalUiVocabulary.PartialReception}</b>. " +
            "Study colloquy at research start deepens reception; completing both paths grants full reception.\n\n" +
            $"<size=12><color=#AABBCC><i>Dismissing hides this card for {EraBranchRules.ColloquyDeferTurns} turns.</i></color></size>",
            choices,
            confessional: false,
            secondary: true);
    }

    List<ConfessionalEmphasisChoice> BuildConfessionalSecondaryPool()
    {
        var pool = new List<ConfessionalEmphasisChoice>();
        TryAddConfessionalSecondary(pool, ConfessionalEmphasisChoice.InternalFormula);
        if (MatchHistory.Instance != null)
        {
            if (MatchHistory.Instance.CanOfferAugsburgConfessionalEmphasis())
                TryAddConfessionalSecondary(pool, ConfessionalEmphasisChoice.AugsburgPublic);
            if (MatchHistory.Instance.CanOfferSmalcaldConfessionalEmphasis())
                TryAddConfessionalSecondary(pool, ConfessionalEmphasisChoice.SmalcaldPolemic);
        }

        return pool;
    }

    List<ConfessionsCultureEmphasisChoice> BuildCultureSecondaryPool()
    {
        var pool = new List<ConfessionsCultureEmphasisChoice>();
        TryAddCultureSecondary(pool, ConfessionsCultureEmphasisChoice.ChoraleLiturgy);
        if (MatchHistory.Instance != null && MatchHistory.Instance.HasPlayerCombat)
            TryAddCultureSecondary(pool, ConfessionsCultureEmphasisChoice.GerhardtCross);
        return pool;
    }

    void TryAddConfessionalSecondary(List<ConfessionalEmphasisChoice> pool, ConfessionalEmphasisChoice choice)
    {
        if (choice == confessionalPrimary ||
            choice == confessionalSecondary ||
            choice == confessionalTertiary)
            return;

        pool.Add(choice);
    }

    void TryAddCultureSecondary(List<ConfessionsCultureEmphasisChoice> pool, ConfessionsCultureEmphasisChoice choice)
    {
        if (choice == culturePrimary ||
            choice == cultureSecondary ||
            choice == cultureTertiary)
            return;

        pool.Add(choice);
    }

    List<ConfessionalEmphasisChoice> BuildConfessionalTertiaryPool()
    {
        var pool = new List<ConfessionalEmphasisChoice>();
        TryAddConfessionalTertiary(pool, ConfessionalEmphasisChoice.InternalFormula);
        if (MatchHistory.Instance != null)
        {
            if (MatchHistory.Instance.CanOfferAugsburgConfessionalEmphasis())
                TryAddConfessionalTertiary(pool, ConfessionalEmphasisChoice.AugsburgPublic);
            if (MatchHistory.Instance.CanOfferSmalcaldConfessionalEmphasis())
                TryAddConfessionalTertiary(pool, ConfessionalEmphasisChoice.SmalcaldPolemic);
        }

        return pool;
    }

    void TryAddConfessionalTertiary(List<ConfessionalEmphasisChoice> pool, ConfessionalEmphasisChoice choice)
    {
        if (choice == confessionalPrimary ||
            choice == confessionalSecondary ||
            choice == confessionalTertiary)
            return;

        pool.Add(choice);
    }

    void TryPresentConfessionalTertiaryChoice()
    {
        if (!confessionalIntegrated ||
            confessionalTertiary != ConfessionalEmphasisChoice.None ||
            IsAwaitingPlayerChoice ||
            IsBeforeTurn(confessionalTertiaryCooldownUntilTurn))
            return;

        var pool = BuildConfessionalTertiaryPool();
        if (pool.Count == 0)
            return;

        var choices = new List<CrisisCardChoice>();
        foreach (var option in pool)
        {
            choices.Add(new(
                ConfessionalLabel(option) + " (tertiary)",
                ConfessionalDescription(option) + $"  —  {ConfessionalUiVocabulary.SecondaryReception}",
                () => ApplyConfessionalTertiary(option)));
        }

        choices.Add(new(NotNowLabel, "Skip tertiary emphasis for now", () => DeferColloquy(ref confessionalTertiaryCooldownUntilTurn)));

        PresentCard(
            "<color=#88CCFF>Confessional Integration — Third Emphasis</color>",
            "Having bound the synod's confessional voice, a third emphasis line may be adopted as " +
            $"{ConfessionalUiVocabulary.SecondaryReception}.\n\n" +
            $"<size=12><color=#AABBCC><i>Dismissing hides this card for {EraBranchRules.ColloquyDeferTurns} turns.</i></color></size>",
            choices,
            confessional: true,
            secondary: true);
    }

    void ApplyConfessionalPrimary(ConfessionalEmphasisChoice choice, bool deferred = false)
    {
        confessionalPrimary = choice;
        if (!deferred)
            TurnPhaseBanner.Instance?.Refresh($"Confessional emphasis: {ConfessionalLabel(choice)}.");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ApplyConfessionalSecondary(ConfessionalEmphasisChoice choice)
    {
        if (!TryPaySecondaryCost())
            return;

        confessionalSecondary = choice;
        TurnPhaseBanner.Instance?.Refresh(
            $"Secondary confessional emphasis: {ConfessionalLabel(choice)} ({FormatPotency(SecondaryPotency)} bonuses).");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ApplyCulturePrimary(ConfessionsCultureEmphasisChoice choice, bool deferred = false)
    {
        culturePrimary = choice;
        if (!deferred)
            TurnPhaseBanner.Instance?.Refresh($"Culture emphasis: {CultureLabel(choice)}.");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ApplyCultureSecondary(ConfessionsCultureEmphasisChoice choice)
    {
        if (!TryPaySecondaryCost())
            return;

        cultureSecondary = choice;
        TurnPhaseBanner.Instance?.Refresh(
            $"Secondary culture emphasis: {CultureLabel(choice)} ({FormatPotency(SecondaryPotency)} bonuses).");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ApplyConfessionalIntegration()
    {
        if (!TryPayIntegrationCost(ConfessionalIntegrationUnlockTech))
            return;

        confessionalIntegrated = true;
        ConfessionResearchManager.Instance?.UnlockIntegratedForkSiblings(EraForkIntegrationTrack.Confessional);
        TurnPhaseBanner.Instance?.Refresh(
            $"Confessional emphases integrated — secondary deepened to {FormatPotency(ConfessionalSecondaryPotency)}; deferred era paths reopened for {ConfessionalUiVocabulary.PartialReception}.");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
        TryPresentConfessionalTertiaryChoice();
    }

    void ApplyCultureIntegration()
    {
        if (!TryPayIntegrationCost(CultureIntegrationUnlockTech))
            return;

        cultureIntegrated = true;
        ConfessionResearchManager.Instance?.UnlockIntegratedForkSiblings(EraForkIntegrationTrack.Culture);
        TurnPhaseBanner.Instance?.Refresh(
            $"Culture emphases integrated — secondary deepened to {FormatPotency(CultureSecondaryPotency)}; deferred era paths reopened for {ConfessionalUiVocabulary.PartialReception}.");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ApplyConfessionalTertiary(ConfessionalEmphasisChoice choice)
    {
        confessionalTertiary = choice;
        TurnPhaseBanner.Instance?.Refresh(
            $"Tertiary confessional emphasis: {ConfessionalLabel(choice)} ({FormatPotency(TertiaryPotency)} bonuses).");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    bool TryPayIntegrationCost(ConfessionTechId unlockTech)
    {
        int cost = EraBranchRules.ColloquyCostForTier(ConfessionTechDatabase.Get(unlockTech).Tier);
        var faction = FirstSteps.Instance;
        if (faction == null || faction.scriptureManuscripts < cost)
        {
            TurnPhaseBanner.Instance?.Refresh(
                $"Need {cost} manuscripts for integration — choice postponed.");
            return false;
        }

        faction.scriptureManuscripts -= cost;
        return true;
    }

    bool TryPaySecondaryCost()
    {
        var faction = FirstSteps.Instance;
        if (faction == null || faction.scriptureManuscripts < SecondaryManuscriptCost)
        {
            TurnPhaseBanner.Instance?.Refresh(
                $"Need {SecondaryManuscriptCost} manuscripts for the colloquy — choice postponed.");
            return false;
        }

        faction.scriptureManuscripts -= SecondaryManuscriptCost;
        return true;
    }

    static void MergeConfessionalChoice(
        ConfessionModifiers combined,
        ConfessionalEmphasisChoice choice,
        float emphasisPotency,
        float adherencePotency)
    {
        if (choice == ConfessionalEmphasisChoice.None)
            return;

        var scaled = ConfessionModifiers.Scaled(ConfessionalModifiersFor(choice), emphasisPotency);
        combined.Merge(ConfessionModifiers.Scaled(scaled, adherencePotency));
    }

    static void MergeCultureChoice(
        ConfessionModifiers combined,
        ConfessionsCultureEmphasisChoice choice,
        float emphasisPotency,
        float adherencePotency)
    {
        if (choice == ConfessionsCultureEmphasisChoice.None)
            return;

        var scaled = ConfessionModifiers.Scaled(CultureModifiersFor(choice), emphasisPotency);
        combined.Merge(ConfessionModifiers.Scaled(scaled, adherencePotency));
    }

    static ConfessionModifiers ConfessionalModifiersFor(ConfessionalEmphasisChoice choice) => choice switch
    {
        ConfessionalEmphasisChoice.InternalFormula => new ConfessionModifiers { AntinomianGuard = true },
        ConfessionalEmphasisChoice.SmalcaldPolemic => new ConfessionModifiers { WildernessManuscriptBonus = 1 },
        ConfessionalEmphasisChoice.AugsburgPublic => new ConfessionModifiers { CivicRestraintGrowthMultiplier = 1.08f },
        _ => new ConfessionModifiers()
    };

    static ConfessionModifiers CultureModifiersFor(ConfessionsCultureEmphasisChoice choice) => choice switch
    {
        ConfessionsCultureEmphasisChoice.ChoraleLiturgy =>
            new ConfessionModifiers { SettlementAdherenceDecayMultiplier = 0.8f },
        ConfessionsCultureEmphasisChoice.GerhardtCross =>
            new ConfessionModifiers { SpiritualComfortTurnBonus = 5f },
        _ => new ConfessionModifiers()
    };

    static string ConfessionalLabel(ConfessionalEmphasisChoice choice) => choice switch
    {
        ConfessionalEmphasisChoice.InternalFormula => "Formula (internal)",
        ConfessionalEmphasisChoice.SmalcaldPolemic => "Smalcald (polemic)",
        ConfessionalEmphasisChoice.AugsburgPublic => "Augsburg (public)",
        _ => "none"
    };

    static string ConfessionalDescription(ConfessionalEmphasisChoice choice) => choice switch
    {
        ConfessionalEmphasisChoice.InternalFormula => "Antinomian guard",
        ConfessionalEmphasisChoice.SmalcaldPolemic => "Wilderness +1 mss",
        ConfessionalEmphasisChoice.AugsburgPublic => "Civic restraint (Law) +8%",
        _ => ""
    };

    static string CultureLabel(ConfessionsCultureEmphasisChoice choice) => choice switch
    {
        ConfessionsCultureEmphasisChoice.ChoraleLiturgy => "Chorale liturgy",
        ConfessionsCultureEmphasisChoice.GerhardtCross => "Gerhardt cross",
        _ => "none"
    };

    static string CultureDescription(ConfessionsCultureEmphasisChoice choice) => choice switch
    {
        ConfessionsCultureEmphasisChoice.ChoraleLiturgy => "Settlement adherence decay -20%",
        ConfessionsCultureEmphasisChoice.GerhardtCross => "+5 comfort/turn",
        _ => ""
    };

    static string FormatPotency(float potency) => $"{potency * 100f:F0}%";

    void PresentCard(
        string title,
        string body,
        List<CrisisCardChoice> choices,
        bool confessional,
        bool secondary = false)
    {
        if (TryShowImmediate(title, body, choices, confessional, secondary))
            return;

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
        deferredPresentRoutine = StartCoroutine(RetryPresentDeferred(title, body, choices, confessional, secondary));
    }

    bool TryShowImmediate(
        string title,
        string body,
        IReadOnlyList<CrisisCardChoice> choices,
        bool confessional,
        bool secondary)
    {
        if (CrisisCardPanel.Instance == null)
            return false;

        pendingTitle = title;
        pendingBody = body;
        pendingChoices = choices is List<CrisisCardChoice> list
            ? list
            : new List<CrisisCardChoice>(choices);
        pendingIsConfessional = confessional;

        if (!CrisisCardPanel.Instance.Show(title, body, pendingChoices, this))
            return false;

        pendingConfessionalChoice = confessional;
        pendingCultureChoice = !confessional;
        SynodBriefPanel.Instance?.Hide();

        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
        return true;
    }

    IEnumerator RetryPresentDeferred(
        string title,
        string body,
        List<CrisisCardChoice> choices,
        bool confessional,
        bool secondary)
    {
        for (int i = 0; i < 8; i++)
        {
            yield return null;
            if (i == 0)
                yield return new WaitForEndOfFrame();

            if (IsAwaitingPlayerChoice)
                yield break;

            if (TryShowImmediate(title, body, choices, confessional, secondary))
            {
                deferredPresentRoutine = null;
                yield break;
            }
        }

        deferredPresentRoutine = null;
        Debug.LogWarning("Tier 2 emphasis card could not open after deferred retries.");
    }
}
