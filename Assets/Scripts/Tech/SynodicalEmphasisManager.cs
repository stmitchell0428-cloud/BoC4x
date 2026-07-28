using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Synodical Emphasis: pick Walther (pastoral) or Pieper (dogmatic) at full bonus.
/// After Johann Gerhard (Loci Theologici), adopt the other emphasis at cost with half bonuses.
/// </summary>
public class SynodicalEmphasisManager : MonoBehaviour, IChoiceCardPresenter
{
    public static SynodicalEmphasisManager Instance { get; private set; }

    public static int SecondaryManuscriptCost => EraBranchRules.ColloquyCostForTier(4);
    public const float SecondaryPotency = 0.5f;
    public const float IntegrationPotencyBoost = 0.25f;

    public static ConfessionTechId SecondaryUnlockTech => ConfessionTechId.JohannGerhard;
    public static ConfessionTechId IntegrationUnlockTech => ConfessionTechId.SynodicalGovernance;

    SynodicalEmphasisId primaryEmphasis = SynodicalEmphasisId.None;
    SynodicalEmphasisId secondaryEmphasis = SynodicalEmphasisId.None;
    bool synodicalIntegrated;
    int secondaryCooldownUntilTurn = -1;
    int integrationCooldownUntilTurn = -1;
    Coroutine deferredPresentRoutine;

    public bool IsAwaitingPlayerChoice { get; private set; }
    public SynodicalEmphasisId PrimaryEmphasis => primaryEmphasis;
    public SynodicalEmphasisId SecondaryEmphasis => secondaryEmphasis;
    public bool HasSecondaryEmphasis => secondaryEmphasis != SynodicalEmphasisId.None;
    public bool IsSynodicalIntegrated => synodicalIntegrated;

    public bool OwnsSynodicalEmphasis(SynodicalEmphasisId emphasis) =>
        primaryEmphasis == emphasis || secondaryEmphasis == emphasis;

    float SecondaryPotencyEffective =>
        synodicalIntegrated
            ? Mathf.Min(1f, SecondaryPotency + IntegrationPotencyBoost)
            : SecondaryPotency;

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
        IsAwaitingPlayerChoice = false;
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
    }

    public void OnChoiceCardCancelled()
    {
        // Emphasis is required — default to Walther if dismissed.
        if (primaryEmphasis == SynodicalEmphasisId.None)
            ApplyPrimary(SynodicalEmphasisId.WaltherPastoral, deferred: true);
        IsAwaitingPlayerChoice = false;
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh("Synod deferred — Walther pastoral emphasis applied by default.");
    }

    public void OnSynodicalEmphasisUnlocked()
    {
        if (primaryEmphasis != SynodicalEmphasisId.None)
            return;

        PresentPrimaryChoice();
    }

    public void OnTechUnlocked(ConfessionTechId id)
    {
        if (id == ConfessionTechId.SynodicalEmphasis)
            OnSynodicalEmphasisUnlocked();
        else if (id == SecondaryUnlockTech)
            TryPresentSecondaryChoice();
        else if (id == IntegrationUnlockTech)
            TryPresentIntegrationChoice();
    }

    public ConfessionModifiers GetEmphasisModifiers(float adherencePotency)
    {
        var combined = new ConfessionModifiers();
        if (primaryEmphasis != SynodicalEmphasisId.None)
            combined.Merge(ScaledEmphasis(ModifiersFor(primaryEmphasis), 1f, adherencePotency));

        if (secondaryEmphasis != SynodicalEmphasisId.None)
            combined.Merge(ScaledEmphasis(ModifiersFor(secondaryEmphasis), SecondaryPotencyEffective, adherencePotency));

        return combined;
    }

    public string FormatStatusLine()
    {
        if (IsAwaitingPlayerChoice)
            return "<color=#88CCFF><b>Synodical emphasis</b>  -  choose your path</color>";

        if (primaryEmphasis == SynodicalEmphasisId.None)
            return "";

        string primary = primaryEmphasis == SynodicalEmphasisId.WaltherPastoral
            ? "Walther (pastoral)"
            : "Pieper (dogmatic)";

        if (secondaryEmphasis == SynodicalEmphasisId.None)
        {
            var gerhard = ConfessionTechDatabase.Get(SecondaryUnlockTech);
            return $"<color=#AABBCC>Synodical: {primary} ({FormatPotency(1f)})  |  {gerhard.Name} unlocks secondary</color>";
        }

        string secondary = secondaryEmphasis == SynodicalEmphasisId.WaltherPastoral
            ? "Walther"
            : "Pieper";
        string tag = synodicalIntegrated ? "integrated" : "secondary";
        return $"<color=#AABBCC>Synodical: {primary} ({FormatPotency(1f)}) + {secondary} ({FormatPotency(SecondaryPotencyEffective)}, {tag})</color>";
    }

    static string FormatPotency(float potency) => $"{potency * 100f:F0}%";

    void PresentPrimaryChoice()
    {
        var choices = new List<CrisisCardChoice>
        {
            new(
                "Walther — pastoral",
                "Law/Gospel drift halved; pastoral preaching steers crises",
                () => ApplyPrimary(SynodicalEmphasisId.WaltherPastoral)),
            new(
                "Pieper — dogmatic",
                "Preach +10 adherence; 25% manuscript refund on preach",
                () => ApplyPrimary(SynodicalEmphasisId.PieperDogmatic))
        };

        string gerhardName = ConfessionTechDatabase.Get(SecondaryUnlockTech).Name;
        PresentCard(
            "<color=#88CCFF>Synodical Emphasis</color>",
            "The synod must decide what leads the church in this era: Walther's pastoral Law/Gospel " +
            "or Pieper's systematic dogmatics.\n\n" +
            "<b>Both paths remain researchable.</b> This choice sets which emphasis receives full bonuses.\n\n" +
            $"<size=12><color=#AABBCC>After <b>{gerhardName}</b>, you may adopt the other emphasis " +
            $"for {ConfessionalUiVocabulary.FormatColloquySecondaryCost(SecondaryManuscriptCost)}</color></size>",
            choices);
    }

    void TryPresentSecondaryChoice()
    {
        if (primaryEmphasis == SynodicalEmphasisId.None ||
            secondaryEmphasis != SynodicalEmphasisId.None ||
            IsAwaitingPlayerChoice ||
            IsBeforeTurn(secondaryCooldownUntilTurn))
            return;

        var other = OtherEmphasis(primaryEmphasis);
        var choices = new List<CrisisCardChoice>
        {
            new(
                EmphasisLabel(other) + " (secondary)",
                FormatSecondaryDescription(other) + $"  —  {ConfessionalUiVocabulary.FormatColloquySecondaryCost(SecondaryManuscriptCost)}",
                () => ApplySecondary(other)),
            new(
                NotNowLabel,
                "Keep single emphasis",
                () => DeferColloquy(ref secondaryCooldownUntilTurn))
        };

        PresentCard(
            "<color=#88CCFF>Second Emphasis — Colloquy</color>",
            $"Having completed <b>{ConfessionTechDatabase.Get(SecondaryUnlockTech).Name}</b>, " +
            "the synod may integrate the other tradition at reduced strength.\n\n" +
            $"<size=12><color=#AABBCC><i>Dismissing hides this card for {EraBranchRules.ColloquyDeferTurns} turns.</i></color></size>",
            choices);
    }

    public void EnsureSecondaryChoiceVisible()
    {
        if (ConfessionResearchManager.Instance == null ||
            !ConfessionResearchManager.Instance.IsTechUnlocked(SecondaryUnlockTech))
            return;

        TryPresentSecondaryChoice();
        TryPresentIntegrationChoice();
    }

    void TryPresentIntegrationChoice()
    {
        if (primaryEmphasis == SynodicalEmphasisId.None ||
            secondaryEmphasis == SynodicalEmphasisId.None ||
            synodicalIntegrated ||
            IsAwaitingPlayerChoice ||
            IsBeforeTurn(integrationCooldownUntilTurn))
            return;

        if (ConfessionResearchManager.Instance == null ||
            !ConfessionResearchManager.Instance.IsTechUnlocked(IntegrationUnlockTech))
            return;

        int cost = EraBranchRules.ColloquyCostForTier(ConfessionTechDatabase.Get(IntegrationUnlockTech).Tier);
        var choices = new List<CrisisCardChoice>
        {
            new(
                "Integrate synodical voice",
                $"Deepen secondary reception to {FormatPotency(SecondaryPotency + IntegrationPotencyBoost)}  —  {cost} mss",
                ApplyIntegration),
            new(NotNowLabel, "Keep secondary reception unchanged", () => DeferColloquy(ref integrationCooldownUntilTurn))
        };

        PresentCard(
            "<color=#88CCFF>Synodical Integration</color>",
            $"Having completed <b>{ConfessionTechDatabase.Get(IntegrationUnlockTech).Name}</b>, " +
            "Walther and Pieper emphases may be preached as one synodical witness.\n\n" +
            $"<size=12><color=#AABBCC><i>Dismissing hides this card for {EraBranchRules.ColloquyDeferTurns} turns.</i></color></size>",
            choices);
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

    void ApplyPrimary(SynodicalEmphasisId emphasis, bool deferred = false)
    {
        primaryEmphasis = emphasis;
        Debug.Log($"Synodical emphasis (primary): {emphasis}");
        if (!deferred)
            TurnPhaseBanner.Instance?.Refresh($"Synod emphasizes {EmphasisLabel(emphasis)}.");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ApplySecondary(SynodicalEmphasisId emphasis)
    {
        var faction = FirstSteps.Instance;
        if (faction != null && faction.scriptureManuscripts < SecondaryManuscriptCost)
        {
            TurnPhaseBanner.Instance?.Refresh(
                $"Need {SecondaryManuscriptCost} manuscripts for the colloquy — choice postponed.");
            return;
        }

        if (faction != null)
            faction.scriptureManuscripts -= SecondaryManuscriptCost;

        secondaryEmphasis = emphasis;
        Debug.Log($"Synodical emphasis (secondary): {emphasis} at {SecondaryPotency:P0} potency");
        TurnPhaseBanner.Instance?.Refresh(
            $"Secondary emphasis: {EmphasisLabel(emphasis)} ({FormatPotency(SecondaryPotency)} bonuses).");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    void ApplyIntegration()
    {
        int cost = EraBranchRules.ColloquyCostForTier(ConfessionTechDatabase.Get(IntegrationUnlockTech).Tier);
        var faction = FirstSteps.Instance;
        if (faction == null || faction.scriptureManuscripts < cost)
        {
            TurnPhaseBanner.Instance?.Refresh($"Need {cost} manuscripts for synodical integration.");
            return;
        }

        faction.scriptureManuscripts -= cost;
        synodicalIntegrated = true;
        TurnPhaseBanner.Instance?.Refresh(
            $"Synodical emphases integrated — secondary now {FormatPotency(SecondaryPotencyEffective)}.");
        ConfessionResearchManager.Instance?.NotifyAdherenceChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    static SynodicalEmphasisId OtherEmphasis(SynodicalEmphasisId current) =>
        current == SynodicalEmphasisId.WaltherPastoral
            ? SynodicalEmphasisId.PieperDogmatic
            : SynodicalEmphasisId.WaltherPastoral;

    static string EmphasisLabel(SynodicalEmphasisId emphasis) =>
        emphasis == SynodicalEmphasisId.WaltherPastoral ? "Walther pastoral" : "Pieper dogmatic";

    static string FormatSecondaryDescription(SynodicalEmphasisId emphasis) =>
        emphasis == SynodicalEmphasisId.WaltherPastoral
            ? "Half Law/Gospel drift reduction"
            : "Half preach adherence + refund";

    static ConfessionModifiers ModifiersFor(SynodicalEmphasisId emphasis) =>
        emphasis == SynodicalEmphasisId.WaltherPastoral
            ? new ConfessionModifiers { LawGospelDriftMultiplier = 0.5f }
            : new ConfessionModifiers { PreachAdherenceBonus = 10f, PreachManuscriptRefundChance = 0.25f };

    static ConfessionModifiers ScaledEmphasis(
        ConfessionModifiers source,
        float emphasisPotency,
        float adherencePotency)
    {
        var scaled = ConfessionModifiers.Scaled(source, emphasisPotency);
        return ConfessionModifiers.Scaled(scaled, adherencePotency);
    }

    void PresentCard(string title, string body, List<CrisisCardChoice> choices)
    {
        if (TryShowImmediate(title, body, choices))
            return;

        if (deferredPresentRoutine != null)
            StopCoroutine(deferredPresentRoutine);
        deferredPresentRoutine = StartCoroutine(RetryPresentDeferred(title, body, choices));
    }

    bool TryShowImmediate(string title, string body, IReadOnlyList<CrisisCardChoice> choices)
    {
        if (CrisisCardPanel.Instance == null)
            return false;

        if (!CrisisCardPanel.Instance.Show(title, body, choices, this))
            return false;

        IsAwaitingPlayerChoice = true;
        FirstSteps.Instance?.RefreshDashboard();
        TurnPhaseBanner.Instance?.Refresh();
        return true;
    }

    IEnumerator RetryPresentDeferred(string title, string body, List<CrisisCardChoice> choices)
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
        Debug.LogWarning("Synodical emphasis card could not open after deferred retries.");
    }
}
