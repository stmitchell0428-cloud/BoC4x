public enum TechTrack
{
    Doctrine,
    Culture,
    Secular
}

public enum TechTreeCategory
{
    Spiritual,
    Secular
}

public static class TechTreeRules
{
    public static TechTreeCategory CategoryFor(TechTrack track) =>
        track == TechTrack.Secular ? TechTreeCategory.Secular : TechTreeCategory.Spiritual;

    public static TechTreeCategory CategoryFor(ConfessionTechId id) =>
        CategoryFor(ConfessionTechDatabase.Get(id).Track);
}

public enum ConfessionTechId
{
    // Tier 1  -  Reformation foundation
    LuthersCatechism,
    VerbalInspiration,
    LawAndGospel,
    SacramentalLife,
    ReformationHymnody,
    AlbrechtDurer,
    LucasCranach,
    OrderedCreation,

    // Tier 2  -  Lutheran confessions & early arts / science
    AugsburgConfession,
    SmalcaldArticles,
    FormulaOfConcord,
    PaulGerhardt,
    ChoraleTradition,
    JohannesKepler,
    CarlLinnaeus,

    // Tier 3  -  Age of Orthodoxy
    MartinChemnitz,
    JohannGerhard,
    AbrahamCalov,
    IsaacNewton,
    GregorMendel,

    // Tier 4  -  Synodical era
    WaltherPastoralTheology,
    FrancisPieper,
    MissionarySending,
    JohannSebastianBach,
    OttoVonGuericke,
    MichaelFaraday,

    // Tier 5  -  Modern confession
    HermannSasse,
    BoGiertz,
    RobertPreus,
    SynodicalGovernance,
    EdRiojas,
    JamesClerkMaxwell,
    LouisPasteur,

    // Tier 1  -  Civic crafts (traditional 4X)
    EarthenVessels,
    // Tier 2  -  Civic economy & early war
    ParishGranary,
    ShepherdsSling,

    // Tier 1  -  Reformation branches
    BondageOfWill,
    TwoKingdoms,

    // Tier 2  -  Confessional expansion
    LargeCatechism,
    GutenbergPress,

    // Tier 3  -  Orthodoxy & liturgy
    DavidChytraeus,
    NikolausSelnecker,

    // Tier 4  -  Mission & sending
    WilhelmLoehe,

    // Tier 6  -  Global Witness
    CTCRReports,
    NormanNagel,
    ConcordiaPublishing,
    WernerHeisenberg,
    GlobalLutheranFellowship,
    KurtMarquart
}

public enum ConfessionTechStatus
{
    Locked,
    Available,
    Researching,
    Unlocked,
    AdherenceLocked
}
