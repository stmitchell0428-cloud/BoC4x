using System.Collections.Generic;
using UnityEngine;

public class ConfessionResearchManager : MonoBehaviour
{
    public static ConfessionResearchManager Instance { get; private set; }

    struct ResearchQueue
    {
        public ConfessionTechId? Active;
        public int TurnsRemaining;
        public int StartedOnTurn;
    }

    readonly HashSet<ConfessionTechId> unlocked = new();
    readonly HashSet<ConfessionTechId> integratedForkSiblings = new();
    readonly HashSet<ConfessionTechId> studiedForkSiblings = new();

    ResearchQueue doctrineQueue;
    ResearchQueue cultureQueue;
    ResearchQueue secularQueue;

    public ConfessionModifiers DoctrineModifiers { get; private set; } = new();
    public ConfessionModifiers CultureModifiers { get; private set; } = new();
    public ConfessionModifiers SecularModifiers { get; private set; } = new();

    public event System.Action ResearchChanged;

    void Awake()
    {
        Instance = this;
        RecomputeModifiers();
    }

    FirstSteps Faction => FirstSteps.Instance;

    static TechTreeCategory TreeFor(ConfessionTechId id) =>
        TechTreeRules.CategoryFor(id);

    static bool IsAdherenceGatedTech(ConfessionTechId id) =>
        TechTreeRules.RequiresAdherence(TreeFor(id));

    ref ResearchQueue QueueRef(TechTreeCategory tree)
    {
        switch (tree)
        {
            case TechTreeCategory.Doctrine: return ref doctrineQueue;
            case TechTreeCategory.Culture: return ref cultureQueue;
            default: return ref secularQueue;
        }
    }

    /// <summary>Tech bonuses scale above this adherence; at or below, unlocked effects are dormant.</summary>
    public const float BonusPotencyThreshold = 40f;

    ConfessionTechId? ActiveForTree(TechTreeCategory tree) => QueueRef(tree).Active;

    int TurnsRemainingForTree(TechTreeCategory tree) => QueueRef(tree).TurnsRemaining;

    int StartedOnTurnForTree(TechTreeCategory tree) => QueueRef(tree).StartedOnTurn;

    /// <summary>0 at or below BonusPotencyThreshold, 1 at 100%. Scales all track effects.</summary>
    public float AdherencePotency
    {
        get
        {
            var faction = Faction;
            if (faction == null) return 0f;
            float a = faction.ConfessionalAdherence;
            if (a <= BonusPotencyThreshold) return 0f;
            return (a - BonusPotencyThreshold) / (100f - BonusPotencyThreshold);
        }
    }

    public ConfessionModifiers GetEffectiveModifiers()
    {
        float potency = AdherencePotency;
        var combined = new ConfessionModifiers();
        combined.Merge(ConfessionModifiers.Scaled(DoctrineModifiers, potency));
        combined.Merge(ConfessionModifiers.Scaled(CultureModifiers, potency));
        combined.Merge(ConfessionModifiers.Scaled(SecularModifiers, potency));

        if (FirstSteps.Instance != null &&
            FirstSteps.Instance.confessionalIdentity != ConfessionalIdentityId.None)
        {
            combined.Merge(ConfessionModifiers.Scaled(
                ConfessionalIdentityDatabase.ModifiersFor(FirstSteps.Instance.confessionalIdentity),
                potency));
        }

        if (SynodLegacyManager.Instance != null)
            combined.Merge(SynodLegacyManager.Instance.GetModifiers());

        if (SynodicalEmphasisManager.Instance != null)
            combined.Merge(SynodicalEmphasisManager.Instance.GetEmphasisModifiers(potency));

        if (Tier2EmphasisManager.Instance != null)
            combined.Merge(Tier2EmphasisManager.Instance.GetEmphasisModifiers(potency));

        EmphasisDocumentRules.CapWildernessManuscriptBonus(combined);
        return combined;
    }

    public ConfessionTechStatus GetStatus(ConfessionTechId id)
    {
        if (unlocked.Contains(id)) return ConfessionTechStatus.Unlocked;
        if (IsResearching(id))
            return ConfessionTechStatus.Researching;

        var node = ConfessionTechDatabase.Get(id);
        var faction = Faction;

        if (faction != null && IsAdherenceGatedTech(id))
        {
            float required = RequiredAdherenceForSpiritual(node);
            if (faction.ConfessionalAdherence < required)
                return ConfessionTechStatus.AdherenceLocked;
        }

        foreach (var prereq in node.Prerequisites)
            if (!unlocked.Contains(prereq))
                return ConfessionTechStatus.Locked;

        if (IsEraForkLocked(id))
            return ConfessionTechStatus.EraForkLocked;

        return ConfessionTechStatus.Available;
    }

    bool IsResearching(ConfessionTechId id) =>
        doctrineQueue.Active == id || cultureQueue.Active == id || secularQueue.Active == id;

    public bool IsIntegratedForkSibling(ConfessionTechId id) => integratedForkSiblings.Contains(id);

    public ConfessionTechId? GetEraForkChoiceFor(ConfessionTechId id) =>
        EraBranchRules.ChosenSiblingInBranch(unlocked, id);

    public float ForkPotencyFor(ConfessionTechId id) =>
        EraBranchRules.ResolveForkPotency(id, unlocked, integratedForkSiblings, studiedForkSiblings);

    public int GetStudyColloquyCostIfNeeded(ConfessionTechId id)
    {
        if (unlocked.Contains(id) || !integratedForkSiblings.Contains(id) || studiedForkSiblings.Contains(id))
            return 0;

        return EraBranchRules.StudyColloquyCostForTier(ConfessionTechDatabase.Get(id).Tier);
    }

    public bool RequiresStudyColloquy(ConfessionTechId id) => GetStudyColloquyCostIfNeeded(id) > 0;

    public void UnlockIntegratedForkSiblings(EraForkIntegrationTrack track)
    {
        int added = 0;
        foreach (var node in ConfessionTechDatabase.All.Values)
        {
            if (EraBranchRules.TrackFor(node) != track)
                continue;
            if (unlocked.Contains(node.Id))
                continue;
            if (!IsEraForkLocked(node.Id))
                continue;

            integratedForkSiblings.Add(node.Id);
            added++;
        }

        if (added > 0)
        {
            Debug.Log($"Integration reopened {added} era-fork sibling(s) for {ConfessionalUiVocabulary.PartialReception} ({track}).");
            ResearchChanged?.Invoke();
            Faction?.RefreshDashboard();
        }
    }

    bool IsEraForkLocked(ConfessionTechId id)
    {
        if (unlocked.Contains(id))
            return false;
        if (integratedForkSiblings.Contains(id))
            return false;

        return EraBranchRules.ChosenSiblingInBranch(unlocked, id).HasValue;
    }

    public string FormatEraForkStatusLine()
    {
        var parts = new List<string>();
        foreach (var node in ConfessionTechDatabase.All.Values)
        {
            if (string.IsNullOrEmpty(node.EraBranchGroup))
                continue;

            if (unlocked.Contains(node.Id))
            {
                float pot = ForkPotencyFor(node.Id);
                parts.Add($"{node.Name} ({ConfessionalUiVocabulary.FormatEraForkPotencyLabel(pot)})");
            }
            else if (integratedForkSiblings.Contains(node.Id))
            {
                parts.Add($"{node.Name} ({ConfessionalUiVocabulary.FormatIntegratedSiblingReady()})");
            }
        }

        if (parts.Count == 0)
            return "";

        return $"<color=#AABBCC>Era paths  -  {string.Join("  |  ", parts)}</color>";
    }

    public static float RequiredAdherenceForSpiritual(ConfessionTechNode node)
    {
        float floor = BonusPotencyThreshold + 0.01f;
        return node.MinAdherence > 0f ? Mathf.Max(node.MinAdherence, floor) : floor;
    }

    public bool HasActiveResearch =>
        doctrineQueue.Active.HasValue || cultureQueue.Active.HasValue || secularQueue.Active.HasValue;

    public bool HasActiveResearchInTree(TechTreeCategory tree) =>
        ActiveForTree(tree).HasValue;

    public ConfessionTechId? ActiveResearchId =>
        doctrineQueue.Active ?? cultureQueue.Active ?? secularQueue.Active;

    public ConfessionTechId? ActiveResearchIdForTree(TechTreeCategory tree) =>
        ActiveForTree(tree);

    public bool WouldCancelRefundFull(TechTreeCategory tree) =>
        ActiveForTree(tree).HasValue &&
        TurnManager.Instance != null &&
        StartedOnTurnForTree(tree) == TurnManager.Instance.TurnNumber;

    public bool WouldCancelRefundFull(ConfessionTechId id) =>
        WouldCancelRefundFull(TreeFor(id));

    public bool CancelResearch(TechTreeCategory tree)
    {
        var active = ActiveForTree(tree);
        if (!active.HasValue)
            return false;

        var node = ConfessionTechDatabase.Get(active.Value);
        var faction = Faction;
        if (faction != null && node.ManuscriptCost > 0)
        {
            bool sameTurn = TurnManager.Instance != null &&
                            StartedOnTurnForTree(tree) == TurnManager.Instance.TurnNumber;
            int refund = sameTurn
                ? node.ManuscriptCost
                : Mathf.Max(1, node.ManuscriptCost / 2);
            faction.ScriptureManuscripts += refund;
            Debug.Log(sameTurn
                ? $"Research cancelled ({tree}): {node.Name}, full refund ({refund} manuscripts)."
                : $"Research cancelled ({tree}): {node.Name}, refunded {refund} manuscripts.");
        }
        else
        {
            Debug.Log($"Research cancelled ({tree}): {node.Name}.");
        }

        ClearQueue(tree);
        ResearchChanged?.Invoke();
        faction?.RefreshDashboard();
        return true;
    }

    public bool CancelResearch(ConfessionTechId id) =>
        CancelResearch(TreeFor(id));

    void ClearQueue(TechTreeCategory tree)
    {
        ref var queue = ref QueueRef(tree);
        queue.Active = null;
        queue.TurnsRemaining = 0;
        queue.StartedOnTurn = -1;
    }

    public bool TryStartResearch(ConfessionTechId id)
    {
        var tree = TreeFor(id);
        var active = ActiveForTree(tree);

        if (unlocked.Contains(id) || active == id)
            return false;

        if (active.HasValue)
        {
            if (GetStatus(id) != ConfessionTechStatus.Available)
                return false;
            CancelResearch(tree);
        }
        else if (GetStatus(id) != ConfessionTechStatus.Available)
        {
            return false;
        }

        var faction = Faction;
        if (faction == null)
        {
            Debug.LogWarning("Cannot start research: faction state (FirstSteps) not found.");
            return false;
        }

        var node = ConfessionTechDatabase.Get(id);
        int studyCost = GetStudyColloquyCostIfNeeded(id);
        int totalCost = node.ManuscriptCost + studyCost;
        if (faction.ScriptureManuscripts < totalCost)
            return false;

        if (studyCost > 0)
        {
            studiedForkSiblings.Add(id);
            Debug.Log(
                $"Study colloquy for {node.Name} ({studyCost} mss) — {ConfessionalUiVocabulary.DeepenedReception}.");
        }

        faction.ScriptureManuscripts -= totalCost;
        int startedTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;

        ref var queue = ref QueueRef(tree);
        queue.Active = id;
        queue.TurnsRemaining = node.TurnsToComplete;
        queue.StartedOnTurn = startedTurn;

        Debug.Log($"Research started ({tree}): {node.Name} ({node.TurnsToComplete} turns).");
        ResearchChanged?.Invoke();
        faction.RefreshDashboard();
        return true;
    }

    public void AdvanceTurn()
    {
        AdvanceQueue(TechTreeCategory.Doctrine);
        AdvanceQueue(TechTreeCategory.Culture);
        AdvanceQueue(TechTreeCategory.Secular);
    }

    void AdvanceQueue(TechTreeCategory tree)
    {
        ref var queue = ref QueueRef(tree);
        if (!queue.Active.HasValue) return;

        queue.TurnsRemaining--;
        int accel = CityManager.Instance?.GetResearchAcceleration() ?? 0;
        if (accel > 0)
            queue.TurnsRemaining = Mathf.Max(0, queue.TurnsRemaining - accel);

        if (queue.TurnsRemaining > 0)
        {
            Debug.Log($"Research in progress ({tree}): {ConfessionTechDatabase.Get(queue.Active.Value).Name} ({queue.TurnsRemaining} turns left).");
            ResearchChanged?.Invoke();
            Faction?.RefreshDashboard();
            return;
        }

        var id = queue.Active.Value;
        ClearQueue(tree);
        unlocked.Add(id);

        var node = ConfessionTechDatabase.Get(id);
        RecomputeModifiers();
        ApplyUnitBonuses();

        string figure = node.HasFigure ? $" ({node.FigureName}, {node.Lifespan})" : "";
        string receptionNote = "";
        if (integratedForkSiblings.Contains(id))
        {
            float forkPot = ForkPotencyFor(id);
            receptionNote = forkPot >= EraBranchRules.FullDualPathPotency - 0.01f
                ? $"  —  {ConfessionalUiVocabulary.FormatEraForkPotencyLabel(forkPot)} (both era paths complete)"
                : $"  —  {ConfessionalUiVocabulary.FormatEraForkPotencyLabel(forkPot)}";
        }

        Debug.Log($"Unlocked ({tree}): {node.Name}{figure}  -  {node.EffectSummary}{receptionNote}");
        SynodicalEmphasisManager.Instance?.OnTechUnlocked(id);
        Tier2EmphasisManager.Instance?.OnTechUnlocked(id);
        ResearchChanged?.Invoke();
        Faction?.RefreshDashboard();
        MatchController.Instance?.EvaluateConditions();
        TurnPhaseBanner.Instance?.Refresh();
        HexGridMap.Instance?.RefreshResourceVisibility();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        CityPlacementAdvisor.InvalidateCache();
    }

    public void NotifyAdherenceChanged()
    {
        RecomputeModifiers();
        ApplyUnitBonuses();
        ResearchChanged?.Invoke();
    }

    public string ActiveResearchLabel()
    {
        string doctrine = FormatQueueLabel(TechTreeCategory.Doctrine, doctrineQueue.Active, doctrineQueue.TurnsRemaining);
        string culture = FormatQueueLabel(TechTreeCategory.Culture, cultureQueue.Active, cultureQueue.TurnsRemaining);
        string secular = FormatQueueLabel(TechTreeCategory.Secular, secularQueue.Active, secularQueue.TurnsRemaining);
        return $"{TechTreeRules.DisplayName(TechTreeCategory.Doctrine)}: {doctrine}  |  " +
               $"{TechTreeRules.DisplayName(TechTreeCategory.Culture)}: {culture}  |  " +
               $"{TechTreeRules.DisplayName(TechTreeCategory.Secular)}: {secular}";
    }

    public string FormatProminentResearchBlock()
    {
        return "<size=21><color=#77CCFF><b>RESEARCH</b></color></size>\n" +
               FormatProminentResearchLine(TechTreeCategory.Doctrine, doctrineQueue.Active, doctrineQueue.TurnsRemaining) + "\n" +
               FormatProminentResearchLine(TechTreeCategory.Culture, cultureQueue.Active, cultureQueue.TurnsRemaining) + "\n" +
               FormatProminentResearchLine(TechTreeCategory.Secular, secularQueue.Active, secularQueue.TurnsRemaining);
    }

    static string FormatProminentResearchLine(TechTreeCategory tree, ConfessionTechId? active, int turnsRemaining)
    {
        string trackLabel = TechTreeRules.DisplayName(tree);
        if (!active.HasValue)
        {
            return $"  <color=#8899AA>{trackLabel}</color>  " +
                   "<color=#FFAA88>idle</color>  <size=15><color=#99AABB>(T — tech panel)</color></size>";
        }

        var node = ConfessionTechDatabase.Get(active.Value);
        return $"  <color=#AAEEFF>{trackLabel}</color>  " +
               $"<color=#FFFFFF>{node.Name}</color>  <color=#DDEEFF>({turnsRemaining}t)</color>";
    }

    public string FormatCompactResearchSummary()
    {
        string doctrine = FormatCompactTrackLabel(doctrineQueue.Active, doctrineQueue.TurnsRemaining);
        string culture = FormatCompactTrackLabel(cultureQueue.Active, cultureQueue.TurnsRemaining);
        string secular = FormatCompactTrackLabel(secularQueue.Active, secularQueue.TurnsRemaining);
        return $"{TechTreeRules.DisplayName(TechTreeCategory.Doctrine)} {doctrine}  ·  " +
               $"{TechTreeRules.DisplayName(TechTreeCategory.Culture)} {culture}  ·  " +
               $"{TechTreeRules.DisplayName(TechTreeCategory.Secular)} {secular}";
    }

    static string FormatCompactTrackLabel(ConfessionTechId? active, int turnsRemaining)
    {
        if (!active.HasValue)
            return "<color=#FFAA88>idle</color>";

        var node = ConfessionTechDatabase.Get(active.Value);
        return $"{node.Name} ({turnsRemaining}t)";
    }

    static string FormatQueueLabel(TechTreeCategory tree, ConfessionTechId? active, int turnsRemaining)
    {
        if (!active.HasValue) return "idle";
        var node = ConfessionTechDatabase.Get(active.Value);
        return $"{node.Name} ({turnsRemaining}t)";
    }

    public string AdherencePotencyLabel()
    {
        float p = AdherencePotency;
        if (p <= 0f)
            return $"Tech potency: dormant (≤{BonusPotencyThreshold:F0}% adherence) — civic research still allowed";
        return $"Tech potency: {p * 100f:F0}% (" +
               $"{TechTreeRules.DisplayName(TechTreeCategory.Doctrine)}, " +
               $"{TechTreeRules.DisplayName(TechTreeCategory.Culture)}, " +
               $"{TechTreeRules.DisplayName(TechTreeCategory.Secular)})";
    }

    public int UnlockedCount => unlocked.Count;

    public int GetHighestUnlockedTier()
    {
        int max = 1;
        foreach (var id in unlocked)
        {
            int tier = ConfessionTechDatabase.Get(id).Tier;
            if (tier > max)
                max = tier;
        }

        return max;
    }

    public VisualArtEra GetVisualArtEra() =>
        VisualArtEraResolver.FromTier(GetHighestUnlockedTier());

    public bool IsTechUnlocked(ConfessionTechId id) => unlocked.Contains(id);

    public bool HasDoctrineTrioVictory =>
        IsTechUnlocked(ConfessionTechId.CTCRReports) &&
        IsTechUnlocked(ConfessionTechId.NormanNagel) &&
        IsTechUnlocked(ConfessionTechId.GlobalLutheranFellowship);

    void RecomputeModifiers()
    {
        DoctrineModifiers = new ConfessionModifiers();
        CultureModifiers = new ConfessionModifiers();
        SecularModifiers = new ConfessionModifiers();

        foreach (var id in unlocked)
        {
            var node = ConfessionTechDatabase.Get(id);
            var mod = ConfessionModifiers.ForTech(id);
            mod = EmphasisDocumentRules.ApplyDocumentPotency(
                mod,
                EmphasisDocumentRules.CombinedDocumentPotency(id));
            switch (node.Track)
            {
                case TechTrack.Culture:
                    CultureModifiers.Merge(mod);
                    break;
                case TechTrack.Secular:
                    SecularModifiers.Merge(mod);
                    break;
                default:
                    DoctrineModifiers.Merge(mod);
                    break;
            }
        }
    }

    void ApplyUnitBonuses()
    {
        if (TurnManager.Instance == null) return;
        var effective = GetEffectiveModifiers();
        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
            unit.ApplyConfessionBonuses(effective);
    }

    public void ApplyBonusesToAllPlayerUnits() => ApplyUnitBonuses();
}
