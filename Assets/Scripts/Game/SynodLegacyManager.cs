using System.Collections.Generic;
using UnityEngine;

public class SynodLegacyManager : MonoBehaviour
{
    public const int MaxActiveSlots = 3;

    public static SynodLegacyManager Instance { get; private set; }

    readonly HashSet<SynodLegacyTraitId> earned = new();
    readonly List<SynodLegacyTraitId> activeSlots = new();

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HasTrait(SynodLegacyTraitId id) => earned.Contains(id);
    public IReadOnlyCollection<SynodLegacyTraitId> EarnedTraits => earned;
    public IReadOnlyList<SynodLegacyTraitId> ActiveSlots => activeSlots;

    public bool TryAward(SynodLegacyTraitId id)
    {
        if (earned.Contains(id))
            return false;

        earned.Add(id);

        if (activeSlots.Count < MaxActiveSlots)
        {
            activeSlots.Add(id);
            ApplyAwardEffects(id);
            return true;
        }

        LegacySlotPickerPanel.Instance?.Show(id);
        return true;
    }

    public void ReplaceActiveSlot(SynodLegacyTraitId replaceId, SynodLegacyTraitId newTrait)
    {
        int index = activeSlots.IndexOf(replaceId);
        if (index < 0)
            return;

        activeSlots[index] = newTrait;
        ApplyAwardEffects(newTrait);
    }

    void ApplyAwardEffects(SynodLegacyTraitId id)
    {
        Debug.Log($"Legacy trait active: {SynodLegacyTraitDatabase.DisplayName(id)}");
        ConfessionResearchManager.Instance?.ApplyBonusesToAllPlayerUnits();
        FirstSteps.Instance?.RefreshDashboard();
    }

    public ConfessionModifiers GetModifiers()
    {
        var mods = new ConfessionModifiers();
        foreach (var id in activeSlots)
            mods.Merge(SynodLegacyTraitDatabase.ModifiersFor(id));
        return mods;
    }

    public void CheckFameMilestones()
    {
        var faction = FirstSteps.Instance;
        if (faction == null) return;

        if (faction.ConfessionalFame >= 25)
            TryAward(SynodLegacyTraitId.ConfessionalWitness);
        if (faction.ConfessionalFame >= 55)
            TryAward(SynodLegacyTraitId.SynodRepute);
    }

    public string FormatLegacyLine()
    {
        if (activeSlots.Count == 0)
            return earned.Count > 0 ? "Legacy traits: earned (choose slots)" : "Legacy traits: none yet";

        var names = new List<string>();
        foreach (var id in activeSlots)
            names.Add(SynodLegacyTraitDatabase.DisplayName(id));
        return $"Legacy ({activeSlots.Count}/{MaxActiveSlots}): " + string.Join(", ", names);
    }
}
