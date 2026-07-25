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
        SynodLegacyTraitId.GerhardLegacy => "Legalism was checked  -  Law and Gospel held together.",
        SynodLegacyTraitId.ConcordLegacy => "Antinomian drift was rebuked  -  confession held firm.",
        SynodLegacyTraitId.CrisisSurvivor => "The synod endured doctrinal tension without fracturing.",
        SynodLegacyTraitId.ConfessionalWitness => "Word and deed spread  -  fame across the land.",
        SynodLegacyTraitId.SynodRepute => "Renowned confessional witness  -  soft power of the synod.",
        _ => ""
    };

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
