/// <summary>Permanent stacked bonuses from surviving crises (Humankind legacy traits).</summary>
public enum SynodLegacyTraitId
{
    GerhardLegacy,
    ConcordLegacy,
    CrisisSurvivor,
    ConfessionalWitness,
    SynodRepute
}

public static class SynodLegacyTraitDatabase
{
    public static string DisplayName(SynodLegacyTraitId id) => id switch
    {
        SynodLegacyTraitId.GerhardLegacy => "Gerhard's Loci Guard",
        SynodLegacyTraitId.ConcordLegacy => "Formula of Concord Guard",
        SynodLegacyTraitId.CrisisSurvivor => "Crisis Survivor",
        SynodLegacyTraitId.ConfessionalWitness => "Confessional Witness",
        SynodLegacyTraitId.SynodRepute => "Synod Repute",
        _ => id.ToString()
    };

    public static string Description(SynodLegacyTraitId id) => id switch
    {
        SynodLegacyTraitId.GerhardLegacy =>
            "Legalism Guard — when Civic Restraint (Law) runs high and Spiritual Comfort (Gospel) is low, the synod recenters instead of fracturing.",
        SynodLegacyTraitId.ConcordLegacy => "Antinomian drift was rebuked  -  confession held firm.",
        SynodLegacyTraitId.CrisisSurvivor => "The synod endured doctrinal tension without fracturing.",
        SynodLegacyTraitId.ConfessionalWitness => "Word and deed spread  -  fame across the land.",
        SynodLegacyTraitId.SynodRepute => "Renowned confessional witness  -  soft power of the synod.",
        _ => ""
    };

    /// <summary>Player-facing summary of mechanical bonuses (shown in HUD and legacy picker).</summary>
    public static string FormatGameplayEffects(SynodLegacyTraitId id) => id switch
    {
        SynodLegacyTraitId.GerhardLegacy =>
            "Softens legalism crises (recenters Law/Gospel); −15% Law/Gospel drift/turn; hides legalism warning",
        SynodLegacyTraitId.ConcordLegacy =>
            "Softens antinomian crises; +2 adherence when preaching",
        SynodLegacyTraitId.CrisisSurvivor =>
            "8% slower adherence decay; 10% less damage from schismatic units",
        SynodLegacyTraitId.ConfessionalWitness =>
            "+2 adherence when preaching; +1 manuscript on settlement/shore tiles each turn",
        SynodLegacyTraitId.SynodRepute =>
            "+5 Gospel comfort when preaching; +1 population growth each turn",
        _ => ""
    };

    public static string FormatDetailBlock(SynodLegacyTraitId id) =>
        $"<b>{DisplayName(id)}</b>\n" +
        $"<size=12><color=#AABBCC><i>{Description(id)}</i></color></size>\n" +
        $"<size=13><color=#DDEEAA>{FormatGameplayEffects(id)}</color></size>";

    public static string FormatCompactLabel(SynodLegacyTraitId id) =>
        $"{DisplayName(id)}  -  {FormatGameplayEffects(id)}";

    public static ConfessionModifiers ModifiersFor(SynodLegacyTraitId id) => id switch
    {
        SynodLegacyTraitId.GerhardLegacy => new ConfessionModifiers
        {
            LegalismGuard = true,
            LawGospelDriftMultiplier = 0.85f
        },
        SynodLegacyTraitId.ConcordLegacy => new ConfessionModifiers
        {
            AntinomianGuard = true,
            PreachAdherenceBonus = 2f
        },
        SynodLegacyTraitId.CrisisSurvivor => new ConfessionModifiers
        {
            AdherenceDecayMultiplier = 0.92f,
            SchismaticDamageTakenMultiplier = 0.9f
        },
        SynodLegacyTraitId.ConfessionalWitness => new ConfessionModifiers
        {
            PreachAdherenceBonus = 2f,
            SettlementManuscriptBonus = 1
        },
        SynodLegacyTraitId.SynodRepute => new ConfessionModifiers
        {
            PreachSpiritualComfortBonus = 5f,
            PopulationGrowthBonus = 1
        },
        _ => new ConfessionModifiers()
    };
}
