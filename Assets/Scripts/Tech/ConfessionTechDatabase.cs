using System.Collections.Generic;

public class ConfessionTechNode
{
    public ConfessionTechId Id;
    public string Name;
    public string Description;
    public string EffectSummary;
    public string FigureName;
    public string Lifespan;
    public string Era;
    public TechTrack Track;
    public int Tier;
    public int ManuscriptCost;
    public int TurnsToComplete;
    public float MinAdherence;
    public ConfessionTechId[] Prerequisites;
    /// <summary>Mutually exclusive era branch  -  unlocking one node locks siblings in the same group.</summary>
    public string EraBranchGroup;

    public bool HasFigure => !string.IsNullOrEmpty(FigureName);

    public ConfessionTechNode(
        ConfessionTechId id,
        string name,
        string description,
        string effectSummary,
        string era,
        int tier,
        int manuscriptCost,
        int turnsToComplete,
        TechTrack track = TechTrack.Doctrine,
        float minAdherence = 0f,
        string figureName = null,
        string lifespan = null,
        string eraBranchGroup = null,
        params ConfessionTechId[] prerequisites)
    {
        Id = id;
        Name = name;
        Description = description;
        EffectSummary = effectSummary;
        Era = era;
        Track = track;
        Tier = tier;
        ManuscriptCost = manuscriptCost;
        TurnsToComplete = turnsToComplete;
        MinAdherence = minAdherence;
        FigureName = figureName;
        Lifespan = lifespan;
        EraBranchGroup = eraBranchGroup;
        Prerequisites = prerequisites ?? System.Array.Empty<ConfessionTechId>();
    }
}

public static class ConfessionTechDatabase
{
    static readonly Dictionary<ConfessionTechId, ConfessionTechNode> nodes = Build();

    public static IReadOnlyDictionary<ConfessionTechId, ConfessionTechNode> All => nodes;

    public static ConfessionTechNode Get(ConfessionTechId id) => nodes[id];

    public static IEnumerable<ConfessionTechNode> ByTier(int tier)
    {
        foreach (var node in nodes.Values)
            if (node.Tier == tier)
                yield return node;
    }

    public static IEnumerable<ConfessionTechNode> ByTier(int tier, TechTrack track)
    {
        foreach (var node in nodes.Values)
            if (node.Tier == tier && node.Track == track)
                yield return node;
    }

    public static IEnumerable<ConfessionTechNode> ByTier(int tier, TechTreeCategory tree)
    {
        foreach (var node in nodes.Values)
            if (node.Tier == tier && TechTreeRules.CategoryFor(node.Track) == tree)
                yield return node;
    }

    public static TechTreeCategory TreeCategory(ConfessionTechNode node) =>
        TechTreeRules.CategoryFor(node.Track);

    public static int TierCount => 6;

    static Dictionary<ConfessionTechId, ConfessionTechNode> Build()
    {
        const string reformation = "Reformation";
        const string confessions = "Confessions";
        const string orthodoxy = "Age of Orthodoxy";
        const string synodical = "Synodical Era";
        const string modern = "Modern Confession";

        var list = new[]
        {
            //  -  -  Tier 1: Reformation  -  - 
            new ConfessionTechNode(
                ConfessionTechId.LuthersCatechism,
                "Luther's Small Catechism",
                "Teach the faith plainly in six chief parts for pastors and fathers to use in the home.",
                "Preach restores +5 adherence",
                reformation, tier: 1, manuscriptCost: 1, turnsToComplete: 1,
                figureName: "Martin Luther", lifespan: "1483-1546"),

            new ConfessionTechNode(
                ConfessionTechId.VerbalInspiration,
                "Verbal Inspiration",
                "Affirm that Scripture is the inspired, inerrant Word of God.",
                "Adherence decay -25%",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2),

            new ConfessionTechNode(
                ConfessionTechId.LawAndGospel,
                "Law & Gospel Distinction",
                "Preach the Law to convict and the Gospel to comfort sinners.",
                "Preach restores +10 adherence",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2),

            new ConfessionTechNode(
                ConfessionTechId.SacramentalLife,
                "Sacramental Life",
                "Baptism and the Lord's Supper sustain the congregation.",
                "+1 population growth each turn; reveals Grapes",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2),

            new ConfessionTechNode(
                ConfessionTechId.ReformationHymnody,
                "Reformation Hymnody",
                "Sing the faith into the hearts of the people  -  the Word dwelling in melody.",
                "Preach +10 spiritual comfort",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2,
                track: TechTrack.Culture,
                figureName: "Martin Luther", lifespan: "1483-1546",
                prerequisites: new[] { ConfessionTechId.LuthersCatechism }),

            new ConfessionTechNode(
                ConfessionTechId.AlbrechtDurer,
                "Reformation Woodcuts",
                "Visual preaching through Apocalypse scenes, Passion cycles, and the Small Catechism.",
                "+1 population growth in settlements",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2,
                track: TechTrack.Culture,
                figureName: "Albrecht Duerer", lifespan: "1471-1528",
                prerequisites: new[] { ConfessionTechId.LawAndGospel }),

            new ConfessionTechNode(
                ConfessionTechId.LucasCranach,
                "Law & Gospel Panels",
                "Altarpieces and portraits that teach Christ for sinners  -  Luther's visual preacher at Wittenberg.",
                "Preach +5 adherence; legalism drift -15%; reveals Timber",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2,
                track: TechTrack.Culture,
                figureName: "Lucas Cranach the Elder", lifespan: "1472-1553",
                prerequisites: new[] { ConfessionTechId.LuthersCatechism }),

            new ConfessionTechNode(
                ConfessionTechId.OrderedCreation,
                "Ordered Creation",
                "The heavens declare God's glory  -  creation's fixed laws invite faithful inquiry.",
                "Forest/hill movement penalty -1",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2,
                track: TechTrack.Secular, minAdherence: 40f,
                figureName: "Genesis & natural law", lifespan: "Scripture",
                prerequisites: new[] { ConfessionTechId.VerbalInspiration }),

            //  -  -  Tier 2: Confessions  -  - 
            new ConfessionTechNode(
                ConfessionTechId.ConfessionalEmphasis,
                "Confessional Emphasis",
                "The synod chooses whether internal concord or external confession leads this era.",
                "Internal, Smalcald, or Augsburg emphasis (scout contact / schismatic combat)",
                confessions, tier: 2, manuscriptCost: 2, turnsToComplete: 2, minAdherence: 48f,
                figureName: "Book of Concord era", lifespan: "1580",
                prerequisites: new[] { ConfessionTechId.LawAndGospel, ConfessionTechId.SacramentalLife }),

            new ConfessionTechNode(
                ConfessionTechId.ConfessionsCultureEmphasis,
                "Confessions Culture Emphasis",
                "The synod chooses ordered chorale life or cross-and-comfort hymnody.",
                "Chorale or Gerhardt emphasis (Gerhardt if battle endured)",
                confessions, tier: 2, manuscriptCost: 2, turnsToComplete: 2, track: TechTrack.Culture,
                minAdherence: 45f,
                figureName: "Lutheran hymnody", lifespan: "16th-17th c.",
                prerequisites: new[] { ConfessionTechId.ReformationHymnody }),

            new ConfessionTechNode(
                ConfessionTechId.AugsburgConfession,
                "Augsburg Confession",
                "Present the evangelical faith before Emperor Charles V.",
                "Siege pressure +1; reveals Iron",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3, minAdherence: 50f,
                figureName: "Philip Melanchthon", lifespan: "1497-1560",
                prerequisites: new[] { ConfessionTechId.VerbalInspiration }),

            new ConfessionTechNode(
                ConfessionTechId.SmalcaldArticles,
                "Smalcald Articles",
                "Draw a bright line against compromise with Rome.",
                "Law/Gospel drift −15%; settlement +1 manuscript",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3, minAdherence: 45f,
                figureName: "Martin Luther", lifespan: "1483-1546",
                prerequisites: new[] { ConfessionTechId.LawAndGospel }),

            new ConfessionTechNode(
                ConfessionTechId.FormulaOfConcord,
                "Formula of Concord",
                "Settle internal disputes and bind the church to pure doctrine.",
                "Adherence decay −10%",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3, minAdherence: 55f,
                figureName: "Jakob Andreae", lifespan: "1528-1590",
                prerequisites: new[] { ConfessionTechId.SacramentalLife }),

            new ConfessionTechNode(
                ConfessionTechId.PaulGerhardt,
                "Sacred Hymnody",
                "Hymns of cross and comfort that carried congregations through war and plague.",
                "Adherence decay −10%",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3, track: TechTrack.Culture,
                minAdherence: 45f,
                figureName: "Paul Gerhardt", lifespan: "1607-1676",
                prerequisites: new[] { ConfessionTechId.ReformationHymnody, ConfessionTechId.SmalcaldArticles }),

            new ConfessionTechNode(
                ConfessionTechId.ChoraleTradition,
                "Chorale Tradition",
                "Congregational song woven through Sunday liturgy and domestic piety.",
                "Cantor hymns +6 comfort",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3, track: TechTrack.Culture,
                minAdherence: 48f,
                figureName: "Lutheran cantors", lifespan: "16th-18th c.",
                prerequisites: new[] { ConfessionTechId.ReformationHymnody, ConfessionTechId.SacramentalLife }),

            new ConfessionTechNode(
                ConfessionTechId.JohannesKepler,
                "Celestial Harmonies",
                "Track the planets as geometry written by the Creator  -  'thinking God's thoughts after Him.'",
                "All units +1 movement",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3,
                track: TechTrack.Secular, minAdherence: 50f,
                figureName: "Johannes Kepler", lifespan: "1571-1630",
                prerequisites: new[] { ConfessionTechId.OrderedCreation, ConfessionTechId.AugsburgConfession }),

            new ConfessionTechNode(
                ConfessionTechId.CarlLinnaeus,
                "Systema Naturae",
                "Classify living kinds  -  order in biology reflects the wisdom of the Creator.",
                "Wilderness +1 manuscript; reveals Fish",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3,
                track: TechTrack.Secular, minAdherence: 48f,
                figureName: "Carl Linnaeus", lifespan: "1707-1778",
                prerequisites: new[] { ConfessionTechId.OrderedCreation, ConfessionTechId.SacramentalLife }),

            new ConfessionTechNode(
                ConfessionTechId.CoastalWharves,
                "River Trade & Wharves",
                "Wharves and fishing along Pomeranian and Baltic shores — Bugenhagen's church order meeting coastal commerce.",
                "Wharf, fishing post, coastal patrol & explorer at coastal cities",
                confessions, tier: 2, manuscriptCost: 2, turnsToComplete: 2, minAdherence: 42f,
                track: TechTrack.Secular,
                figureName: "Johannes Bugenhagen", lifespan: "1485-1558",
                prerequisites: new[] { ConfessionTechId.OrderedCreation, ConfessionTechId.LawAndGospel }),

            //  -  -  Tier 3: Age of Orthodoxy  -  - 
            new ConfessionTechNode(
                ConfessionTechId.MartinChemnitz,
                "Examination of the Council of Trent",
                "Answer Rome's decrees with Scripture and the Lutheran confessions.",
                "Soldiers +2 defense",
                orthodoxy, tier: 3, manuscriptCost: 3, turnsToComplete: 3, minAdherence: 55f,
                figureName: "Martin Chemnitz", lifespan: "1524-1586",
                prerequisites: new[] { ConfessionTechId.AugsburgConfession, ConfessionTechId.FormulaOfConcord }),

            new ConfessionTechNode(
                ConfessionTechId.NavalWarfare,
                "Naval Warfare & War Docks",
                "After Chemnitz's patristic reception, fortify Baltic shores — war docks and galleys for confessional defense.",
                "War dock, galley; coastal patrol & galley +1 move (requires wharf + Chemnitz)",
                orthodoxy, tier: 3, manuscriptCost: 3, turnsToComplete: 3, minAdherence: 55f,
                track: TechTrack.Secular,
                figureName: "Martin Chemnitz", lifespan: "1524-1586",
                prerequisites: new[] { ConfessionTechId.CoastalWharves, ConfessionTechId.MartinChemnitz }),

            new ConfessionTechNode(
                ConfessionTechId.JohannGerhard,
                "Loci Theologici",
                "Systematic theology grounded in Scripture and the Book of Concord.",
                "Legalism crisis softened; decay -10%",
                orthodoxy, tier: 3, manuscriptCost: 4, turnsToComplete: 4, minAdherence: 60f,
                figureName: "Johann Gerhard", lifespan: "1582-1637",
                prerequisites: new[] { ConfessionTechId.MartinChemnitz, ConfessionTechId.SmalcaldArticles }),

            new ConfessionTechNode(
                ConfessionTechId.AbrahamCalov,
                "Systema Locorum Theologicorum",
                "Defend Lutheran orthodoxy with exhaustive biblical exposition.",
                "+1 manuscript in settlements each turn; reveals Gold",
                orthodoxy, tier: 3, manuscriptCost: 4, turnsToComplete: 3, minAdherence: 62f,
                figureName: "Abraham Calov", lifespan: "1612-1686",
                prerequisites: new[] { ConfessionTechId.JohannGerhard }),

            new ConfessionTechNode(
                ConfessionTechId.IsaacNewton,
                "Principia Mathematica",
                "Mechanics and gravitation  -  the universe governed by laws established by God.",
                "Soldiers +2 attack",
                orthodoxy, tier: 3, manuscriptCost: 4, turnsToComplete: 4,
                track: TechTrack.Secular, minAdherence: 58f,
                figureName: "Isaac Newton", lifespan: "1643-1727",
                prerequisites: new[] { ConfessionTechId.JohannesKepler, ConfessionTechId.MartinChemnitz }),

            new ConfessionTechNode(
                ConfessionTechId.GregorMendel,
                "Laws of Inheritance",
                "Peas and patience reveal order in living things  -  science serving created life.",
                "+1 population growth",
                orthodoxy, tier: 3, manuscriptCost: 4, turnsToComplete: 3,
                track: TechTrack.Secular, minAdherence: 55f,
                figureName: "Gregor Mendel", lifespan: "1822-1884",
                prerequisites: new[] { ConfessionTechId.CarlLinnaeus, ConfessionTechId.JohannGerhard }),

            //  -  -  Tier 4: Synodical era  -  - 
            new ConfessionTechNode(
                ConfessionTechId.SynodicalEmphasis,
                "Synodical Emphasis",
                "The immigrant synod chooses whether pastoral Law/Gospel or systematic dogmatics leads the church.",
                "Choose Walther or Pieper emphasis (full bonus; other path later at half)",
                synodical, tier: 4, manuscriptCost: 3, turnsToComplete: 2, minAdherence: 62f,
                figureName: "Synodical colloquy", lifespan: "1847+",
                prerequisites: new[] { ConfessionTechId.SmalcaldArticles, ConfessionTechId.FormulaOfConcord }),

            new ConfessionTechNode(
                ConfessionTechId.WaltherPastoralTheology,
                "Law & Gospel in Preaching",
                "Pastoral theology for immigrants building confessional congregations in America.",
                "Law/Gospel drift halved each turn",
                synodical, tier: 4, manuscriptCost: 4, turnsToComplete: 4, minAdherence: 65f,
                figureName: "C. F. W. Walther", lifespan: "1811-1887",
                prerequisites: new[] { ConfessionTechId.SynodicalEmphasis }),

            new ConfessionTechNode(
                ConfessionTechId.FrancisPieper,
                "Christian Dogmatics",
                "Three-volume dogmatics teaching Scripture as the sole source and norm of doctrine.",
                "Preach +10 adherence; 25% manuscript refund",
                synodical, tier: 4, manuscriptCost: 5, turnsToComplete: 4, minAdherence: 68f,
                figureName: "Francis Pieper", lifespan: "1852-1931",
                prerequisites: new[] { ConfessionTechId.WaltherPastoralTheology, ConfessionTechId.JohannGerhard }),

            new ConfessionTechNode(
                ConfessionTechId.MissionarySending,
                "Frontier Mission",
                "Send preachers to unchurched settlements and the wilderness.",
                "Missionaries +1 movement",
                synodical, tier: 4, manuscriptCost: 3, turnsToComplete: 2, minAdherence: 60f,
                figureName: "C. F. W. Walther", lifespan: "1811-1887",
                prerequisites: new[] { ConfessionTechId.AugsburgConfession, ConfessionTechId.FormulaOfConcord }),

            new ConfessionTechNode(
                ConfessionTechId.JohannSebastianBach,
                "Liturgical Cantatas",
                "Music in service of the Word  -  Soli Deo Gloria at Leipzig and beyond.",
                "+8 spiritual comfort/turn; preach +5 adherence",
                synodical, tier: 4, manuscriptCost: 5, turnsToComplete: 4, track: TechTrack.Culture,
                minAdherence: 65f,
                figureName: "J. S. Bach", lifespan: "1685-1750",
                prerequisites: new[] { ConfessionTechId.ChoraleTradition, ConfessionTechId.PaulGerhardt, ConfessionTechId.JohannGerhard }),

            new ConfessionTechNode(
                ConfessionTechId.OttoVonGuericke,
                "Magdeburg Hemispheres",
                "Demonstrate vacuum and air pressure  -  Lutheran Magdeburg serving mechanical inquiry.",
                "Soldiers +2 defense",
                synodical, tier: 4, manuscriptCost: 4, turnsToComplete: 3,
                track: TechTrack.Secular, minAdherence: 62f,
                figureName: "Otto von Guericke", lifespan: "1602-1686",
                prerequisites: new[] { ConfessionTechId.IsaacNewton }),

            new ConfessionTechNode(
                ConfessionTechId.OpenOceanNavigation,
                "Open-Ocean Navigation",
                "Guericke's Magdeburg inquiry meets North Sea charts — heavy hulls for crossings beyond coastal waters.",
                "Deep-sea ship; explorer +1 sight; galley & deep-sea +1 move",
                synodical, tier: 4, manuscriptCost: 4, turnsToComplete: 3, minAdherence: 58f,
                track: TechTrack.Secular,
                figureName: "Otto von Guericke", lifespan: "1602-1686",
                prerequisites: new[] { ConfessionTechId.NavalWarfare, ConfessionTechId.OttoVonGuericke }),

            new ConfessionTechNode(
                ConfessionTechId.MichaelFaraday,
                "Electromagnetic Order",
                "Fields and forces in creation  -  experimental science confessing lawful design.",
                "Adherence decay -10%; reveals Coal",
                synodical, tier: 4, manuscriptCost: 4, turnsToComplete: 4,
                track: TechTrack.Secular, minAdherence: 65f,
                figureName: "Michael Faraday", lifespan: "1791-1867",
                prerequisites: new[] { ConfessionTechId.GregorMendel, ConfessionTechId.WaltherPastoralTheology }),

            //  -  -  Tier 5: Modern confession  -  - 
            new ConfessionTechNode(
                ConfessionTechId.HermannSasse,
                "Here We Stand",
                "Confess the faith amid ecclesiastical turmoil and unionistic pressure.",
                "-25% damage from Schismatic units",
                modern, tier: 5, manuscriptCost: 4, turnsToComplete: 3, minAdherence: 70f,
                figureName: "Hermann Sasse", lifespan: "1895-1976",
                prerequisites: new[] { ConfessionTechId.FrancisPieper, ConfessionTechId.FormulaOfConcord }),

            new ConfessionTechNode(
                ConfessionTechId.BoGiertz,
                "The Hammer of God",
                "Pastoral realism: the Law crushes and the Gospel raises the sinner.",
                "Missionaries +2 attack",
                modern, tier: 5, manuscriptCost: 4, turnsToComplete: 3, minAdherence: 68f,
                figureName: "Bo Giertz", lifespan: "1905-1998",
                prerequisites: new[] { ConfessionTechId.WaltherPastoralTheology, ConfessionTechId.MissionarySending }),

            new ConfessionTechNode(
                ConfessionTechId.RobertPreus,
                "Theology of Post-Reformation Lutheranism",
                "Recover the riches of orthodox Lutheran dogmatics for the church today.",
                "+1 population growth; adherence floor 40%",
                modern, tier: 5, manuscriptCost: 5, turnsToComplete: 4, minAdherence: 72f,
                figureName: "Robert Preus", lifespan: "1928-1995",
                prerequisites: new[] { ConfessionTechId.FrancisPieper, ConfessionTechId.AbrahamCalov }),

            new ConfessionTechNode(
                ConfessionTechId.SynodicalGovernance,
                "Mutual Conference & Aid",
                "Congregations walk together in confession, discipline, and mission.",
                "+2 population growth each turn",
                modern, tier: 5, manuscriptCost: 5, turnsToComplete: 4, minAdherence: 70f,
                figureName: "Synodical tradition", lifespan: "1847-present",
                prerequisites: new[] { ConfessionTechId.AugsburgConfession, ConfessionTechId.FormulaOfConcord, ConfessionTechId.WaltherPastoralTheology }),

            new ConfessionTechNode(
                ConfessionTechId.JamesClerkMaxwell,
                "Electromagnetic Theory",
                "Unify light and magnetism  -  mathematics praising the coherence of creation.",
                "All units +1 movement; soldiers +1 attack",
                modern, tier: 5, manuscriptCost: 5, turnsToComplete: 4,
                track: TechTrack.Secular, minAdherence: 72f,
                figureName: "James Clerk Maxwell", lifespan: "1831-1879",
                prerequisites: new[] { ConfessionTechId.MichaelFaraday, ConfessionTechId.FrancisPieper }),

            new ConfessionTechNode(
                ConfessionTechId.LouisPasteur,
                "Germ Theory & Pasteurization",
                "Life does not arise from chaos  -  science confirming creation against spontaneous generation.",
                "+1 settlement pop; +1 population growth",
                modern, tier: 5, manuscriptCost: 4, turnsToComplete: 3,
                track: TechTrack.Secular, minAdherence: 70f,
                figureName: "Louis Pasteur", lifespan: "1822-1895",
                prerequisites: new[] { ConfessionTechId.GregorMendel, ConfessionTechId.HermannSasse }),

            new ConfessionTechNode(
                ConfessionTechId.EdRiojas,
                "Confessional Church Art",
                "Contemporary visual theology  -  altarpieces and narrative panels teaching Christ crucified for sinners.",
                "+1 population; adherence floor +5%; missions +1 comfort",
                modern, tier: 5, manuscriptCost: 4, turnsToComplete: 3, track: TechTrack.Culture,
                minAdherence: 68f,
                figureName: "Ed Riojas", lifespan: "b. 1958",
                prerequisites: new[] { ConfessionTechId.JohannSebastianBach, ConfessionTechId.AlbrechtDurer, ConfessionTechId.LucasCranach, ConfessionTechId.HermannSasse }),

            new ConfessionTechNode(
                ConfessionTechId.EarthenVessels,
                "Earthen Vessels (Pottery)",
                "Clay pots store grain, ink, and wine  -  the stuff of parish household and scriptorium.",
                "+1 population growth in settlements",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2,
                track: TechTrack.Secular, minAdherence: 35f,
                figureName: "Parish potters", lifespan: "Ancient-modern",
                prerequisites: new[] { ConfessionTechId.OrderedCreation }),

            new ConfessionTechNode(
                ConfessionTechId.ParishWalls,
                "Parish Walls",
                "Timber and stone around the parish  -  earthly authority guarding Word and sacrament.",
                "Unlocks Fortifications build; walled cities block hostile entry until loyalty falls",
                reformation, tier: 1, manuscriptCost: 3, turnsToComplete: 2,
                track: TechTrack.Secular, minAdherence: 38f,
                figureName: "Parish magistrates", lifespan: "Reformation",
                prerequisites: new[] { ConfessionTechId.EarthenVessels, ConfessionTechId.TwoKingdoms }),

            new ConfessionTechNode(
                ConfessionTechId.ParishGranary,
                "Parish Granary",
                "Store the harvest against famine  -  good stewardship of God's provision.",
                "+1 population growth; settlements +1 pop; reveals Wheat and Cattle",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 2,
                track: TechTrack.Secular, minAdherence: 45f,
                figureName: "Stewardship of grain", lifespan: "Biblical-modern",
                prerequisites: new[] { ConfessionTechId.EarthenVessels, ConfessionTechId.SacramentalLife }),

            new ConfessionTechNode(
                ConfessionTechId.ShepherdsSling,
                "Shepherd's Sling",
                "Like David before Goliath  -  humble sling-stones in defense of the flock.",
                "Unlock slingers; soldiers +1 attack",
                confessions, tier: 2, manuscriptCost: 2, turnsToComplete: 2,
                minAdherence: 48f,
                figureName: "1 Samuel 17", lifespan: "Scripture",
                prerequisites: new[] { ConfessionTechId.EarthenVessels, ConfessionTechId.AugsburgConfession }),

            new ConfessionTechNode(
                ConfessionTechId.BondageOfWill,
                "Bondage of the Will",
                "Confess that man's will is enslaved to sin apart from the Holy Spirit.",
                "Adherence decay -12%; preach +3 adherence",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2,
                figureName: "Martin Luther", lifespan: "1483-1546",
                prerequisites: new[] { ConfessionTechId.VerbalInspiration }),

            new ConfessionTechNode(
                ConfessionTechId.TwoKingdoms,
                "Two Kingdoms",
                "Distinguish God's spiritual reign from earthly authority  -  vocation in both.",
                "Soldiers +2 defense; legalism drift -12%; reveals Stone",
                reformation, tier: 1, manuscriptCost: 2, turnsToComplete: 2,
                figureName: "Martin Luther", lifespan: "1483-1546",
                prerequisites: new[] { ConfessionTechId.LawAndGospel }),

            new ConfessionTechNode(
                ConfessionTechId.LargeCatechism,
                "Large Catechism",
                "Pastoral exposition of the catechism for preachers and fathers in the home.",
                "Preach +5 adherence; 15% manuscript refund",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3, minAdherence: 48f,
                figureName: "Martin Luther", lifespan: "1483-1546",
                prerequisites: new[] { ConfessionTechId.LuthersCatechism, ConfessionTechId.AugsburgConfession }),

            new ConfessionTechNode(
                ConfessionTechId.GutenbergPress,
                "Gutenberg & the Printed Word",
                "Moveable type spreads Luther's writings and the vernacular Bible.",
                "+1 manuscript in settlements and wilderness",
                confessions, tier: 2, manuscriptCost: 3, turnsToComplete: 3,
                track: TechTrack.Secular, minAdherence: 46f,
                figureName: "Johannes Gutenberg", lifespan: "c. 1400-1468",
                prerequisites: new[] { ConfessionTechId.OrderedCreation, ConfessionTechId.LucasCranach }),

            new ConfessionTechNode(
                ConfessionTechId.DavidChytraeus,
                "Chytraeus on the Last Days",
                "Eschatology and confessional polemics in the age of Chemnitz.",
                "Soldiers +1 defense; adherence floor +12%",
                orthodoxy, tier: 3, manuscriptCost: 4, turnsToComplete: 3, minAdherence: 58f,
                figureName: "David Chytraeus", lifespan: "1530-1600",
                prerequisites: new[] { ConfessionTechId.MartinChemnitz }),

            new ConfessionTechNode(
                ConfessionTechId.NikolausSelnecker,
                "Selnecker & Liturgical Order",
                "Hymns, agenda, and ordered worship sustaining Lutheran parishes.",
                "+4 comfort/turn; cantor hymns +6 comfort",
                orthodoxy, tier: 3, manuscriptCost: 4, turnsToComplete: 3, track: TechTrack.Culture,
                minAdherence: 56f,
                figureName: "Nikolaus Selnecker", lifespan: "1530-1592",
                prerequisites: new[] { ConfessionTechId.ChoraleTradition, ConfessionTechId.FormulaOfConcord }),

            new ConfessionTechNode(
                ConfessionTechId.WilhelmLoehe,
                "Loehe's Mission Sending",
                "Send deacons and pastors to frontier settlements and abroad.",
                "Missionaries +1 move and +1 attack",
                synodical, tier: 4, manuscriptCost: 4, turnsToComplete: 3, minAdherence: 62f,
                figureName: "Wilhelm Loehe", lifespan: "1808-1872",
                prerequisites: new[] { ConfessionTechId.MissionarySending, ConfessionTechId.ChoraleTradition }),

            new ConfessionTechNode(
                ConfessionTechId.CTCRReports,
                "CTCR Reports",
                "Commission on Theology and Church Relations  -  contemporary confessional guidance.",
                "Decay -15%; adherence floor 50%",
                "Global Witness", tier: 6, manuscriptCost: 5, turnsToComplete: 4, minAdherence: 74f,
                figureName: "LCMS CTCR", lifespan: "1962-present",
                prerequisites: new[] { ConfessionTechId.SynodicalGovernance, ConfessionTechId.HermannSasse }),

            new ConfessionTechNode(
                ConfessionTechId.NormanNagel,
                "Nagel on Preaching",
                "Christ at the center  -  the sermon delivers the forgiveness won on the cross.",
                "Preach +8 adherence; Law/Gospel drift -30%",
                "Global Witness", tier: 6, manuscriptCost: 5, turnsToComplete: 4, minAdherence: 75f,
                figureName: "Norman Nagel", lifespan: "1931-2019",
                prerequisites: new[] { ConfessionTechId.RobertPreus, ConfessionTechId.BoGiertz }),

            new ConfessionTechNode(
                ConfessionTechId.ConcordiaPublishing,
                "Concordia Publishing House",
                "Confessional resources for congregations, schools, and missions worldwide.",
                "+1 settlement manuscript; +1 population growth",
                "Global Witness", tier: 6, manuscriptCost: 4, turnsToComplete: 3, track: TechTrack.Culture,
                minAdherence: 72f,
                figureName: "CPH", lifespan: "1869-present",
                prerequisites: new[] { ConfessionTechId.EdRiojas, ConfessionTechId.SynodicalGovernance }),

            new ConfessionTechNode(
                ConfessionTechId.WernerHeisenberg,
                "Quantum Uncertainty",
                "Physical limits of measurement  -  creation ordered yet not mechanically closed.",
                "Soldiers +1 attack; decay -8%",
                "Global Witness", tier: 6, manuscriptCost: 5, turnsToComplete: 4,
                track: TechTrack.Secular, minAdherence: 74f,
                figureName: "Werner Heisenberg", lifespan: "1901-1976",
                prerequisites: new[] { ConfessionTechId.JamesClerkMaxwell, ConfessionTechId.MichaelFaraday }),

            new ConfessionTechNode(
                ConfessionTechId.GlobalLutheranFellowship,
                "Global Lutheran Fellowship",
                "Confessional Lutherans confess together across nations and languages.",
                "Missionaries +1 move; +3 comfort each turn",
                "Global Witness", tier: 6, manuscriptCost: 4, turnsToComplete: 3, track: TechTrack.Culture,
                minAdherence: 73f,
                figureName: "ILC / confessional fellowship", lifespan: "20th-21st c.",
                prerequisites: new[] { ConfessionTechId.CTCRReports, ConfessionTechId.LouisPasteur }),

            new ConfessionTechNode(
                ConfessionTechId.KurtMarquart,
                "Marquart on Scripture",
                "Defend verbal inspiration and the inerrancy of Holy Scripture.",
                "Preach +5 adherence; -15% schismatic damage",
                "Global Witness", tier: 6, manuscriptCost: 5, turnsToComplete: 4, minAdherence: 76f,
                figureName: "Kurt Marquart", lifespan: "1932-2006",
                prerequisites: new[] { ConfessionTechId.NormanNagel, ConfessionTechId.FrancisPieper }),
        };

        var dict = new Dictionary<ConfessionTechId, ConfessionTechNode>();
        foreach (var node in list)
            dict[node.Id] = node;
        AssignEraBranches(dict);
        return dict;
    }

    static void AssignEraBranches(Dictionary<ConfessionTechId, ConfessionTechNode> dict)
    {
        // Confessions era — public confession vs print culture (emphasis handles Formula/Augsburg/Smalcald).
        SetBranch(dict, ConfessionTechId.AugsburgConfession, "confessional", "Era2-Confession");
        SetBranch(dict, ConfessionTechId.GutenbergPress, "confessional", "Era2-Confession");

        // Synodical era — mission sending is no longer forked; Bach stands alone (no era fork).
    }

    static void SetBranch(
        Dictionary<ConfessionTechId, ConfessionTechNode> dict,
        ConfessionTechId id,
        string track,
        string branchId)
    {
        if (!dict.TryGetValue(id, out var node))
            return;

        node.EraBranchGroup = $"{track}:{branchId}";
    }
}
