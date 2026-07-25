using System.Collections.Generic;
using UnityEngine;

public class ConfessionResearchManager : MonoBehaviour
{
    public static ConfessionResearchManager Instance { get; private set; }

    readonly HashSet<ConfessionTechId> unlocked = new();
    readonly HashSet<string> chosenEraBranches = new();

    ConfessionTechId? activeSpiritualResearch;
    int spiritualTurnsRemaining;
    int spiritualResearchStartedOnTurn = -1;

    ConfessionTechId? activeSecularResearch;
    int secularTurnsRemaining;
    int secularResearchStartedOnTurn = -1;

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

    static bool IsSpiritualTech(ConfessionTechId id) =>
        TechTreeRules.CategoryFor(id) == TechTreeCategory.Spiritual;

    static TechTreeCategory TreeFor(ConfessionTechId id) =>
        TechTreeRules.CategoryFor(id);

    ConfessionTechId? ActiveForTree(TechTreeCategory tree) =>
        tree == TechTreeCategory.Spiritual ? activeSpiritualResearch : activeSecularResearch;

    int TurnsRemainingForTree(TechTreeCategory tree) =>
        tree == TechTreeCategory.Spiritual ? spiritualTurnsRemaining : secularTurnsRemaining;

    int StartedOnTurnForTree(TechTreeCategory tree) =>
        tree == TechTreeCategory.Spiritual ? spiritualResearchStartedOnTurn : secularResearchStartedOnTurn;

    /// <summary>0 below 40% adherence, 1 at 100%. Scales all track effects.</summary>
    public float AdherencePotency
    {
        get
        {
            var faction = Faction;
            if (faction == null) return 0f;
            float a = faction.ConfessionalAdherence;
            if (a < 40f) return 0f;
            return (a - 40f) / 60f;
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

        return combined;
    }

    public ConfessionTechStatus GetStatus(ConfessionTechId id)
    {
        if (unlocked.Contains(id)) return ConfessionTechStatus.Unlocked;
        if (activeSpiritualResearch == id || activeSecularResearch == id)
            return ConfessionTechStatus.Researching;

        var node = ConfessionTechDatabase.Get(id);
        var faction = Faction;

        if (faction != null)
        {
            if (faction.ConfessionalAdherence < node.MinAdherence)
                return ConfessionTechStatus.AdherenceLocked;
            if (faction.ConfessionalAdherence < 40f)
                return ConfessionTechStatus.AdherenceLocked;
        }

        foreach (var prereq in node.Prerequisites)
            if (!unlocked.Contains(prereq))
                return ConfessionTechStatus.Locked;

        if (IsEraBranchBlocked(node))
            return ConfessionTechStatus.Locked;

        return ConfessionTechStatus.Available;
    }

    bool IsEraBranchBlocked(ConfessionTechNode node)
    {
        if (string.IsNullOrEmpty(node.EraBranchGroup))
            return false;
        return chosenEraBranches.Contains(node.EraBranchGroup);
    }

    void RegisterEraBranchChoice(ConfessionTechNode node)
    {
        if (!string.IsNullOrEmpty(node.EraBranchGroup))
            chosenEraBranches.Add(node.EraBranchGroup);
    }

    public bool HasActiveResearch =>
        activeSpiritualResearch.HasValue || activeSecularResearch.HasValue;

    public bool HasActiveResearchInTree(TechTreeCategory tree) =>
        ActiveForTree(tree).HasValue;

    public ConfessionTechId? ActiveResearchId =>
        activeSpiritualResearch ?? activeSecularResearch;

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
        if (tree == TechTreeCategory.Spiritual)
        {
            activeSpiritualResearch = null;
            spiritualTurnsRemaining = 0;
            spiritualResearchStartedOnTurn = -1;
        }
        else
        {
            activeSecularResearch = null;
            secularTurnsRemaining = 0;
            secularResearchStartedOnTurn = -1;
        }
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
        if (faction.ScriptureManuscripts < node.ManuscriptCost)
            return false;

        faction.ScriptureManuscripts -= node.ManuscriptCost;
        int startedTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;

        if (tree == TechTreeCategory.Spiritual)
        {
            activeSpiritualResearch = id;
            spiritualTurnsRemaining = node.TurnsToComplete;
            spiritualResearchStartedOnTurn = startedTurn;
        }
        else
        {
            activeSecularResearch = id;
            secularTurnsRemaining = node.TurnsToComplete;
            secularResearchStartedOnTurn = startedTurn;
        }

        Debug.Log($"Research started ({tree}): {node.Name} ({node.TurnsToComplete} turns).");
        ResearchChanged?.Invoke();
        faction.RefreshDashboard();
        return true;
    }

    public void AdvanceTurn()
    {
        AdvanceQueue(TechTreeCategory.Spiritual);
        AdvanceQueue(TechTreeCategory.Secular);
    }

    void AdvanceQueue(TechTreeCategory tree)
    {
        var active = ActiveForTree(tree);
        if (!active.HasValue) return;

        int turnsRemaining = TurnsRemainingForTree(tree);
        turnsRemaining--;
        int accel = CityManager.Instance?.GetResearchAcceleration() ?? 0;
        if (accel > 0)
            turnsRemaining = Mathf.Max(0, turnsRemaining - accel);

        if (tree == TechTreeCategory.Spiritual)
            spiritualTurnsRemaining = turnsRemaining;
        else
            secularTurnsRemaining = turnsRemaining;

        if (turnsRemaining > 0)
        {
            Debug.Log($"Research in progress ({tree}): {ConfessionTechDatabase.Get(active.Value).Name} ({turnsRemaining} turns left).");
            ResearchChanged?.Invoke();
            Faction?.RefreshDashboard();
            return;
        }

        var id = active.Value;
        ClearQueue(tree);
        unlocked.Add(id);
        RegisterEraBranchChoice(ConfessionTechDatabase.Get(id));

        var node = ConfessionTechDatabase.Get(id);
        RecomputeModifiers();
        ApplyUnitBonuses();

        string figure = node.HasFigure ? $" ({node.FigureName}, {node.Lifespan})" : "";
        Debug.Log($"Unlocked ({tree}): {node.Name}{figure}  -  {node.EffectSummary}");
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
        ApplyUnitBonuses();
        ResearchChanged?.Invoke();
    }

    public string ActiveResearchLabel()
    {
        string spiritual = FormatQueueLabel(TechTreeCategory.Spiritual, activeSpiritualResearch, spiritualTurnsRemaining);
        string secular = FormatQueueLabel(TechTreeCategory.Secular, activeSecularResearch, secularTurnsRemaining);
        return $"Spiritual: {spiritual}  |  Secular: {secular}";
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
        if (p <= 0f) return "Tech potency: dormant (<40% adherence)";
        return $"Tech potency: {p * 100f:F0}% (doctrine, culture, secular)";
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
        foreach (var unit in TurnManager.Instance.GetUnits(FactionId.LutheranSynod))
            unit.ApplyConfessionBonuses(effective);
    }

    public void ApplyBonusesToAllPlayerUnits() => ApplyUnitBonuses();
}
