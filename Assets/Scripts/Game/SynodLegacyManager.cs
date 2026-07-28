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
        string name = SynodLegacyTraitDatabase.DisplayName(id);
        string effects = SynodLegacyTraitDatabase.FormatGameplayEffects(id);
        Debug.Log($"Legacy trait active: {name}  -  {effects}");
        TurnPhaseBanner.Instance?.Refresh(
            $"<color=#DDCC88><b>Legacy trait:</b></color> {name}\n" +
            $"<size=13><color=#CCEEBB>{effects}</color></size>");
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
            return "";

        var lines = new List<string>
        {
            $"<color=#DDCC88><b>Legacy traits ({activeSlots.Count}/{MaxActiveSlots})</b></color>"
        };
        foreach (var id in activeSlots)
        {
            lines.Add(
                $"<size=12>• <b>{SynodLegacyTraitDatabase.DisplayName(id)}</b>  -  " +
                $"<color=#BBDDAA>{SynodLegacyTraitDatabase.FormatGameplayEffects(id)}</color></size>");
        }

        return string.Join("\n", lines);
    }
}
