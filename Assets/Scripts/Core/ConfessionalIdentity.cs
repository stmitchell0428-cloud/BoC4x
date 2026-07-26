/// <summary>National Spirit equivalent  -  chosen when Wittenberg is founded (Humankind/Millennia inspired).</summary>

public enum ConfessionalIdentityId

{

    None,

    MissionarySending,

    Magisterial,

    PastoralCare,

    ChemnitzConfessional

}



public static class ConfessionalIdentityDatabase

{

    public static string DisplayName(ConfessionalIdentityId id) => id switch

    {

        ConfessionalIdentityId.MissionarySending => "Missionary Sending",

        ConfessionalIdentityId.Magisterial => "Magisterial Confession",

        ConfessionalIdentityId.PastoralCare => "Pastoral Care",

        ConfessionalIdentityId.ChemnitzConfessional => "Chemnitz Confessional",

        _ => "Undeclared"

    };



    public static string Description(ConfessionalIdentityId id) => id switch

    {

        ConfessionalIdentityId.MissionarySending =>

            "The synod sends the Word abroad  -  stronger missionaries and preaching.",

        ConfessionalIdentityId.Magisterial =>

            "Doctrine and order guard the church  -  soldiers and adherence resilience.",

        ConfessionalIdentityId.PastoralCare =>

            "Comfort and catechesis sustain the flock  -  Gospel meter and chaplains.",

        ConfessionalIdentityId.ChemnitzConfessional =>

            "Chemnitz's method and Missouri's confession  -  balanced doctrine, sending, and care.",

        _ => ""

    };



    public static ConfessionModifiers ModifiersFor(ConfessionalIdentityId id) => id switch

    {

        ConfessionalIdentityId.MissionarySending => new ConfessionModifiers

        {

            MissionaryMovementBonus = 1,

            PreachAdherenceBonus = 3f,

            WildernessManuscriptBonus = 1

        },

        ConfessionalIdentityId.Magisterial => new ConfessionModifiers

        {

            SoldierDefenseBonus = 2,

            AdherenceDecayMultiplier = 0.9f

        },

        ConfessionalIdentityId.PastoralCare => new ConfessionModifiers

        {

            SpiritualComfortTurnBonus = 4f,

            PreachSpiritualComfortBonus = 8f,

            SettlementPopulationBonus = 1

        },

        ConfessionalIdentityId.ChemnitzConfessional => new ConfessionModifiers

        {

            PreachAdherenceBonus = 2f,

            AdherenceDecayMultiplier = 0.88f,

            SettlementManuscriptBonus = 1,

            MissionaryMovementBonus = 1

        },

        _ => new ConfessionModifiers()

    };

}

