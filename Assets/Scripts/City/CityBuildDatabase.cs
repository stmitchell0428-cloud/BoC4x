using System.Collections.Generic;
using System.Linq;

public class CityBuildDefinition
{
    public CityBuildId Id;
    public string Name;
    public string Description;
    public string EffectSummary;
    public CityBuildCategory Category;
    public CityBuildTrack Track;
    public int ManuscriptCost;
    public int TurnsToComplete;
    public int ProductionCost;
    public bool UniquePerCity;
    public UnitType? SpawnsUnit;
    public ConfessionTechId? RequiredTech;
    public ConfessionTechId? RequiredTechSecondary;

    public bool UsesProduction => Track == CityBuildTrack.Secular && ProductionCost > 0;

    public CityBuildDefinition(
        CityBuildId id,
        string name,
        string description,
        string effectSummary,
        CityBuildCategory category,
        CityBuildTrack track,
        int manuscriptCost = 0,
        int turnsToComplete = 0,
        int productionCost = 0,
        bool uniquePerCity = false,
        UnitType? spawnsUnit = null,
        ConfessionTechId? requiredTech = null,
        ConfessionTechId? requiredTechSecondary = null)
    {
        Id = id;
        Name = name;
        Description = description;
        EffectSummary = effectSummary;
        Category = category;
        Track = track;
        ManuscriptCost = manuscriptCost;
        TurnsToComplete = turnsToComplete;
        ProductionCost = productionCost;
        UniquePerCity = uniquePerCity;
        SpawnsUnit = spawnsUnit;
        RequiredTech = requiredTech;
        RequiredTechSecondary = requiredTechSecondary;
    }
}

public static class CityBuildDatabase
{
    static readonly Dictionary<CityBuildId, CityBuildDefinition> defs = Build();

    public static CityBuildDefinition Get(CityBuildId id) => defs[id];

    public static IEnumerable<CityBuildDefinition> All => defs.Values;

    public static IEnumerable<ConfessionTechId> RequiredTechs(CityBuildDefinition def)
    {
        if (def.RequiredTech.HasValue)
            yield return def.RequiredTech.Value;
        if (def.RequiredTechSecondary.HasValue)
            yield return def.RequiredTechSecondary.Value;
    }

    public static bool MeetsTechRequirements(CityBuildDefinition def)
    {
        var rm = ConfessionResearchManager.Instance;
        if (rm == null)
            return false;

        foreach (var tech in RequiredTechs(def))
        {
            if (!rm.IsTechUnlocked(tech))
                return false;
        }

        return true;
    }

    public static string FormatMissingTechRequirement(CityBuildDefinition def)
    {
        var rm = ConfessionResearchManager.Instance;
        if (rm == null)
            return "Research required";

        var missing = RequiredTechs(def)
            .Where(tech => !rm.IsTechUnlocked(tech))
            .Select(tech => ConfessionTechDatabase.Get(tech).Name)
            .ToList();

        if (missing.Count == 0)
            return null;

        return missing.Count == 1
            ? $"Needs {missing[0]}"
            : $"Needs {string.Join(" + ", missing)}";
    }

    public static IEnumerable<CityBuildDefinition> ByCategory(CityBuildCategory category)
    {
        foreach (var def in defs.Values)
            if (def.Category == category)
                yield return def;
    }

    static Dictionary<CityBuildId, CityBuildDefinition> Build() => new()
    {
        [CityBuildId.TrainMissionary] = new CityBuildDefinition(
            CityBuildId.TrainMissionary,
            "Send Missionary",
            "Ordain and send a missionary to preach and survey the land.",
            "Spawns a missionary  -  upgrade to pastor on city hex (Large Catechism)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 1,
            spawnsUnit: UnitType.Missionary),

        [CityBuildId.TrainPastor] = new CityBuildDefinition(
            CityBuildId.TrainPastor,
            "Ordain Pastor",
            "Install a preaching pastor for parish ministry and visitation.",
            "Spawns a pastor (free preach +4 adherence)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            spawnsUnit: UnitType.Pastor,
            requiredTech: ConfessionTechId.LargeCatechism),

        [CityBuildId.TrainDeaconess] = new CityBuildDefinition(
            CityBuildId.TrainDeaconess,
            "Commission Deaconess",
            "Set apart deaconesses for mercy, catechesis, and parish nursing.",
            "Spawns a deaconess (free preach +comfort, +2 adherence)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 2,
            spawnsUnit: UnitType.Deaconess,
            requiredTech: ConfessionTechId.LargeCatechism),

        [CityBuildId.TrainCantor] = new CityBuildDefinition(
            CityBuildId.TrainCantor,
            "Train Cantor",
            "Raise a cantor to lead chorales and liturgical hymnody.",
            "Spawns a cantor (free hymn once/turn)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            spawnsUnit: UnitType.Cantor,
            requiredTech: ConfessionTechId.ChoraleTradition),

        [CityBuildId.TrainArcher] = new CityBuildDefinition(
            CityBuildId.TrainArcher,
            "Train Archer",
            "Levy bowmen from the district  -  disciplined ranged fire.",
            "Spawns an archer (2-hex ranged attack)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 1,
            spawnsUnit: UnitType.Archer,
            requiredTech: ConfessionTechId.ShepherdsSling),

        [CityBuildId.TrainHorseman] = new CityBuildDefinition(
            CityBuildId.TrainHorseman,
            "Train Horseman",
            "Mounted lay brothers for rapid response along the frontier.",
            "Spawns a horseman (3 move, strong attack)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            spawnsUnit: UnitType.Horseman,
            requiredTech: ConfessionTechId.MartinChemnitz),

        [CityBuildId.TrainSiegeEngine] = new CityBuildDefinition(
            CityBuildId.TrainSiegeEngine,
            "Build Siege Engine",
            "Artillery science — breaching engines from ordered mechanics and confessional polemic.",
            "Spawns a siege engine (slow, high loyalty pressure vs walls; local armory + Chytraeus + Maxwell)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            spawnsUnit: UnitType.SiegeEngine,
            requiredTech: ConfessionTechId.JamesClerkMaxwell,
            requiredTechSecondary: ConfessionTechId.DavidChytraeus),

        [CityBuildId.BuildWharf] = new CityBuildDefinition(
            CityBuildId.BuildWharf,
            "Build Wharf",
            "Timber slips and landings for fishing boats and river trade.",
            "Enables coastal explorer; +1 food at coastal cities",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 12,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.CoastalWharves),

        [CityBuildId.BuildFishingPost] = new CityBuildDefinition(
            CityBuildId.BuildFishingPost,
            "Build Fishing Post",
            "Smokehouses and nets along the shore to feed the synod.",
            "+2 food at coastal cities; requires wharf",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 8,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.CoastalWharves),

        [CityBuildId.TrainCoastalExplorer] = new CityBuildDefinition(
            CityBuildId.TrainCoastalExplorer,
            "Build Coastal Explorer",
            "Light fishing boat for rivers, lakes, and near-shore mapping.",
            "Spawns a coastal explorer (shore + navigable water; wide sight)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 1,
            spawnsUnit: UnitType.CoastalExplorer,
            requiredTech: ConfessionTechId.CoastalWharves),

        [CityBuildId.BuildDock] = new CityBuildDefinition(
            CityBuildId.BuildDock,
            "Build War Dock",
            "Heavy wharves, arsenals, and slips for galleys and deep-sea craft.",
            "Unlocks galleys & deep-sea ships; trade hub; requires wharf",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 20,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.NavalWarfare),

        [CityBuildId.TrainCoastalGalley] = new CityBuildDefinition(
            CityBuildId.TrainCoastalGalley,
            "Build Coastal Galley",
            "Light warship for rivers, lakes, and coastal seas.",
            "Spawns a galley (shore + navigable water only; strong attack)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            spawnsUnit: UnitType.CoastalGalley,
            requiredTech: ConfessionTechId.NavalWarfare),

        [CityBuildId.TrainDeepSeaShip] = new CityBuildDefinition(
            CityBuildId.TrainDeepSeaShip,
            "Build Deep-Sea Ship",
            "Ocean-going vessel for archipelago crossings and distant witness.",
            "Spawns a deep-sea ship (all ocean tiles; amphibious transport)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            spawnsUnit: UnitType.DeepSeaShip,
            requiredTech: ConfessionTechId.OpenOceanNavigation),

        [CityBuildId.TrainScout] = new CityBuildDefinition(
            CityBuildId.TrainScout,
            "Train Scout",
            "Send out riders to map land and report schismatic movement.",
            "Spawns a scout (4 hex sight, fast move)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 1,
            spawnsUnit: UnitType.Scout),

        [CityBuildId.TrainSoldier] = new CityBuildDefinition(
            CityBuildId.TrainSoldier,
            "Train Soldier",
            "Equip lay brothers with sword and shield to guard the synod.",
            "Spawns a soldier (melee defender)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 1,
            spawnsUnit: UnitType.Soldier),

        [CityBuildId.TrainSlinger] = new CityBuildDefinition(
            CityBuildId.TrainSlinger,
            "Train Slinger",
            "Levy shepherds with sling  -  ranged skirmishers before the sword.",
            "Spawns a slinger (2-hex ranged attack)",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 1,
            spawnsUnit: UnitType.Slinger,
            requiredTech: ConfessionTechId.ShepherdsSling),

        [CityBuildId.BuildScriptorium] = new CityBuildDefinition(
            CityBuildId.BuildScriptorium,
            "Scriptorium",
            "Copy Scripture and confessional writings for the synod.",
            "+1 manuscript each turn from this city",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            uniquePerCity: true),

        [CityBuildId.BuildParishSchool] = new CityBuildDefinition(
            CityBuildId.BuildParishSchool,
            "Parish School",
            "Teach the catechism to the next generation in the parish.",
            "+5 synod population when complete",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            uniquePerCity: true),

        [CityBuildId.BuildChapel] = new CityBuildDefinition(
            CityBuildId.BuildChapel,
            "Chapel",
            "A place for daily prayer and the Word in the community.",
            "+5 confessional adherence when complete",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 2,
            uniquePerCity: true),

        [CityBuildId.BuildGuildWorkshop] = new CityBuildDefinition(
            CityBuildId.BuildGuildWorkshop,
            "Guild Workshop",
            "Organize craftsmen and laborers for civic works in the town.",
            "+2 city production per turn",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 20,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.OrderedCreation),

        [CityBuildId.BuildPrintingPress] = new CityBuildDefinition(
            CityBuildId.BuildPrintingPress,
            "Printing Press",
            "Mechanize the reproduction of texts and civic broadsheets.",
            "+1 production and +1 manuscript per turn",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 28,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.GutenbergPress),

        [CityBuildId.BuildObservatory] = new CityBuildDefinition(
            CityBuildId.BuildObservatory,
            "Observatory",
            "Study the heavens with ordered reason alongside confession.",
            "+3 city production per turn",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 35,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.IsaacNewton),

        [CityBuildId.BindCatechism] = new CityBuildDefinition(
            CityBuildId.BindCatechism,
            "Bind Catechism",
            "Scriptorium work: bind manuscripts into portable catechisms for preaching.",
            "Craft 1 bound catechism (2 mss in, 1 turn)  -  preach with +4 adherence",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 2,
            turnsToComplete: 1),

        [CityBuildId.BuildPotteryWorkshop] = new CityBuildDefinition(
            CityBuildId.BuildPotteryWorkshop,
            "Pottery Workshop",
            "Kilns and wheel  -  vessels for grain, ink, and domestic life.",
            "+1 production per turn",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 18,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.EarthenVessels),

        [CityBuildId.BuildGranary] = new CityBuildDefinition(
            CityBuildId.BuildGranary,
            "Parish Granary",
            "Store grain against famine; tithe and distribute to the needy.",
            "+5 food each turn when complete",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 3,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.ParishGranary),

        [CityBuildId.TrainFrontierSettler] = new CityBuildDefinition(
            CityBuildId.TrainFrontierSettler,
            "Train Frontier Settler",
            "Send a settler to found a second independent synod city on valid frontier land.",
            "Requires Mission House in cluster; one settler at a time while only one city exists",
            CityBuildCategory.Unit,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 4,
            spawnsUnit: UnitType.Settler,
            requiredTech: ConfessionTechId.MissionarySending),

        [CityBuildId.BuildSeminary] = new CityBuildDefinition(
            CityBuildId.BuildSeminary,
            "Seminary",
            "Train pastors in exegetical theology and confessional dogmatics.",
            "+1 research progress each turn (confession tech)",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 5,
            turnsToComplete: 4,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.JohannGerhard),

        [CityBuildId.BuildCathedral] = new CityBuildDefinition(
            CityBuildId.BuildCathedral,
            "Cathedral",
            "A mother church for the diocese  -  Word, sacrament, and confession.",
            "+10 adherence and +10 fame (capital only)",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 6,
            turnsToComplete: 5,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.JohannSebastianBach),

        [CityBuildId.BuildHospital] = new CityBuildDefinition(
            CityBuildId.BuildHospital,
            "Parish Hospital",
            "Mercy corps serving body and soul  -  pastoral care in sickness.",
            "+3 city population when complete; +1 pop growth/turn",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.LouisPasteur),

        [CityBuildId.BuildMissionHouse] = new CityBuildDefinition(
            CityBuildId.BuildMissionHouse,
            "Mission House",
            "Base for frontier preachers and colonists sent to unchurched land.",
            "Unlocks colonists cluster-wide; -1 missionary/colonist cost here; +1 fame/turn",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.WilhelmLoehe),

        [CityBuildId.BuildFortification] = new CityBuildDefinition(
            CityBuildId.BuildFortification,
            "Fortifications",
            "Walls and watchmen when schismatic dissent threatens the city.",
            "+5 adherence; defenders +2 defense on this city's tile; blocks hostile entry until loyalty falls",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.ParishWalls),

        [CityBuildId.BuildLibrary] = new CityBuildDefinition(
            CityBuildId.BuildLibrary,
            "Confessional Library",
            "Archive the loci, confessions, and patristic sources for study.",
            "+2 production and +1 manuscript per turn",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Secular,
            productionCost: 24,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.AbrahamCalov),

        [CityBuildId.BuildUniversity] = new CityBuildDefinition(
            CityBuildId.BuildUniversity,
            "University",
            "Higher learning in theology, languages, and natural philosophy.",
            "+3 production; +1 research progress each turn",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 42,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.IsaacNewton),

        [CityBuildId.BuildOrphanage] = new CityBuildDefinition(
            CityBuildId.BuildOrphanage,
            "Orphanage",
            "Care for the fatherless  -  the church as mother in time of plague and war.",
            "+3 synod population; +2 spiritual comfort each turn",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.PaulGerhardt),

        [CityBuildId.BuildOrganLoft] = new CityBuildDefinition(
            CityBuildId.BuildOrganLoft,
            "Organ Loft",
            "Pipe organ and loft for chorale and liturgy  -  Bach would approve.",
            "+3 culture to parent city; +2 spiritual comfort",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.ChoraleTradition),

        [CityBuildId.BuildParishChurch] = new CityBuildDefinition(
            CityBuildId.BuildParishChurch,
            "Parish Church",
            "Stone church with pulpit and altar for Word and sacrament.",
            "+8 confessional adherence when complete",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            uniquePerCity: true),

        [CityBuildId.BuildBarracks] = new CityBuildDefinition(
            CityBuildId.BuildBarracks,
            "Barracks",
            "Quarter soldiers and drill the lay militia of the district.",
            "Train Soldier -1 turn; +1 district production",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            uniquePerCity: true),

        [CityBuildId.BuildArcheryRange] = new CityBuildDefinition(
            CityBuildId.BuildArcheryRange,
            "Archery Range",
            "Butts and bowyers  -  teach the congregation to shoot straight.",
            "Train Archer -1 turn",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.ShepherdsSling),

        [CityBuildId.BuildStable] = new CityBuildDefinition(
            CityBuildId.BuildStable,
            "Stable",
            "Horses, tack, and fodder for mounted patrols.",
            "Train Horseman -1 turn",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.MartinChemnitz),

        [CityBuildId.BuildArmory] = new CityBuildDefinition(
            CityBuildId.BuildArmory,
            "Armory",
            "Store arms and armor for defenders of the confession.",
            "Promote Defender -1 manuscript when this city has an armory; +1 district production",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 4,
            turnsToComplete: 3,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.MartinChemnitz),

        [CityBuildId.BuildWatchtower] = new CityBuildDefinition(
            CityBuildId.BuildWatchtower,
            "Watchtower",
            "Stone tower and bell to warn of schismatic raiders.",
            "+5 adherence; +1 city sight; martial garrison +1 atk/+2 def on city hex",
            CityBuildCategory.ConfessionalBuilding,
            CityBuildTrack.Confessional,
            manuscriptCost: 3,
            turnsToComplete: 2,
            uniquePerCity: true),

        [CityBuildId.BuildMarketHall] = new CityBuildDefinition(
            CityBuildId.BuildMarketHall,
            "Market Hall",
            "Weigh-house and stalls for grain, cloth, and civic trade.",
            "+2 district production per turn",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 22,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.OrderedCreation),

        [CityBuildId.BuildMill] = new CityBuildDefinition(
            CityBuildId.BuildMill,
            "Water Mill",
            "Grind grain and power simple machines for the parish economy.",
            "+1 district production; +1 food from worked tiles",
            CityBuildCategory.SecularBuilding,
            CityBuildTrack.Secular,
            productionCost: 18,
            uniquePerCity: true,
            requiredTech: ConfessionTechId.ParishGranary)
    };
}
