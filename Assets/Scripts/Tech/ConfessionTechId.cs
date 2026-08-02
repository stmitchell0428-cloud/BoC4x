public enum TechTrack
{
    Doctrine,
    Culture,
    Secular
}

public enum TechTreeCategory
{
    Doctrine,
    Culture,
    Secular
}

public static class TechTreeRules
{
    public static TechTreeCategory CategoryFor(TechTrack track) => track switch
    {
        TechTrack.Doctrine => TechTreeCategory.Doctrine,
        TechTrack.Culture => TechTreeCategory.Culture,
        TechTrack.Secular => TechTreeCategory.Secular,
        _ => TechTreeCategory.Doctrine
    };

    public static TechTreeCategory CategoryFor(ConfessionTechId id) =>
        CategoryFor(ConfessionTechDatabase.Get(id).Track);

    public static TechTrack TrackForCategory(TechTreeCategory category) => category switch
    {
        TechTreeCategory.Doctrine => TechTrack.Doctrine,
        TechTreeCategory.Culture => TechTrack.Culture,
        _ => TechTrack.Secular
    };

    public static bool RequiresAdherence(TechTreeCategory category) =>
        category != TechTreeCategory.Secular;

    public static string DisplayName(TechTreeCategory category) => category switch
    {
        TechTreeCategory.Doctrine => "Doctrine",
        TechTreeCategory.Culture => "Hymnody",
        _ => "Civic"
    };

    /// <summary>Longer flavor for tooltips / detail text; HUD and tabs use <see cref="DisplayName"/>.</summary>
    public static string FlavorSubtitle(TechTreeCategory category) => category switch
    {
        TechTreeCategory.Doctrine => "Confessions & doctrine",
        TechTreeCategory.Culture => "Hymnody & life",
        _ => "Science & civic vocation"
    };
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
    ConfessionalEmphasis,
    ConfessionsCultureEmphasis,
    AugsburgConfession,
    SmalcaldArticles,
    FormulaOfConcord,
    PaulGerhardt,
    ChoraleTradition,
    JohannesKepler,
    CarlLinnaeus,
    CoastalWharves,
    NavalWarfare,
    OpenOceanNavigation,

    // Tier 3  -  Age of Orthodoxy
    MartinChemnitz,
    JohannGerhard,
    AbrahamCalov,
    IsaacNewton,
    GregorMendel,

    // Tier 4  -  Synodical era
    SynodicalEmphasis,
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
    ParishWalls,
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
    AdherenceLocked,
    EraForkLocked
}
