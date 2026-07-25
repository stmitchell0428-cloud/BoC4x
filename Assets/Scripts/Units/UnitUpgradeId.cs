public enum UnitUpgradeId

{

    MissionaryToPastor,

    PastorToChaplain,

    PastorToBishop,

    BishopToArchbishop,

    SoldierToDefender

}



public enum UnitUpgradeStatus

{

    Available,

    Locked,

    WrongUnit,

    NotOnCity,

    InsufficientManuscripts,

    ClergySlotsFull

}



public class UnitUpgradeDefinition

{

    public UnitUpgradeId Id;

    public string Name;

    public string Description;

    public string EffectSummary;

    public UnitType FromType;

    public UnitType ToType;

    public ConfessionTechId RequiredTech;

    public int ManuscriptCost;



    public UnitUpgradeDefinition(

        UnitUpgradeId id,

        string name,

        string description,

        string effectSummary,

        UnitType fromType,

        UnitType toType,

        ConfessionTechId requiredTech,

        int manuscriptCost)

    {

        Id = id;

        Name = name;

        Description = description;

        EffectSummary = effectSummary;

        FromType = fromType;

        ToType = toType;

        RequiredTech = requiredTech;

        ManuscriptCost = manuscriptCost;

    }

}



public static class UnitUpgradeDatabase

{

    static readonly System.Collections.Generic.Dictionary<UnitUpgradeId, UnitUpgradeDefinition> defs = Build();



    public static UnitUpgradeDefinition Get(UnitUpgradeId id) => defs[id];



    public static System.Collections.Generic.IEnumerable<UnitUpgradeDefinition> All => defs.Values;



    static System.Collections.Generic.Dictionary<UnitUpgradeId, UnitUpgradeDefinition> Build() => new()

    {

        [UnitUpgradeId.MissionaryToPastor] = new UnitUpgradeDefinition(

            UnitUpgradeId.MissionaryToPastor,

            "Ordain Pastor",

            "Install a missionary to the holy ministry  -  Word and Sacrament for a congregation.",

            "Missionary becomes a pastor (free preach +4 adherence, roster slot)",

            UnitType.Missionary,

            UnitType.Pastor,

            ConfessionTechId.LargeCatechism,

            manuscriptCost: 2),



        [UnitUpgradeId.PastorToChaplain] = new UnitUpgradeDefinition(

            UnitUpgradeId.PastorToChaplain,

            "Specialize as Chaplain",

            "Set apart a pastor for institutional ministry  -  military escort, hospital, or focused parish care.",

            "Pastor becomes chaplain (assign escort / hospital / parish via R roster panel)",

            UnitType.Pastor,

            UnitType.Chaplain,

            ConfessionTechId.WaltherPastoralTheology,

            manuscriptCost: 2),



        [UnitUpgradeId.PastorToBishop] = new UnitUpgradeDefinition(

            UnitUpgradeId.PastorToBishop,

            "Consecrate Bishop",

            "Set apart a pastor to the episcopal office  -  one bishop supervises each city.",

            "Pastor becomes bishop (1/city; stronger preach and parish oversight)",

            UnitType.Pastor,

            UnitType.Bishop,

            ConfessionTechId.FormulaOfConcord,

            manuscriptCost: 3),



        [UnitUpgradeId.BishopToArchbishop] = new UnitUpgradeDefinition(

            UnitUpgradeId.BishopToArchbishop,

            "Elevate to Archbishop",

            "When the synod spans multiple cities, one archbishop shepherds the whole communion.",

            "Bishop becomes archbishop (1/synod when 2+ cities; assign via R)",

            UnitType.Bishop,

            UnitType.Archbishop,

            ConfessionTechId.AugsburgConfession,

            manuscriptCost: 4),



        [UnitUpgradeId.SoldierToDefender] = new UnitUpgradeDefinition(

            UnitUpgradeId.SoldierToDefender,

            "Promote to Defender",

            "Equip a soldier for defensive warfare guarding churches and hamlets.",

            "Soldier becomes a defender (high defense, garrison bonus)",

            UnitType.Soldier,

            UnitType.Defender,

            ConfessionTechId.MartinChemnitz,

            manuscriptCost: 2)

    };

}


