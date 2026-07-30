/// <summary>Cumulative bonuses from unlocked confession techs.</summary>
public class ConfessionModifiers
{
    public float AdherenceDecayMultiplier = 1f;
    public float SettlementAdherenceDecayMultiplier = 1f;
    public float PreachAdherenceBonus = 6f;
    public float PreachSpiritualComfortBonus = 16f;
    public float CantorComfortBonus = 0f;
    public float SpiritualComfortTurnBonus = 0f;
    public int PopulationGrowthBonus = 0;
    public int SettlementPopulationBonus = 0;
    public int WildernessManuscriptBonus = 0;
    public int SettlementManuscriptBonus = 0;
    public int SoldierAttackBonus = 0;
    public int SoldierDefenseBonus = 0;
    public int SiegePressureBonus = 0;
    public int MissionaryMovementBonus = 0;
    public int MissionaryAttackBonus = 0;
    public int AllUnitsMovementBonus = 0;
    public int TerrainMovePenaltyReduction = 0;
    public float LawGospelDriftMultiplier = 1f;
    public float LegalismDriftMultiplier = 1f;
    public float CivicRestraintGrowthMultiplier = 1f;
    public float SchismaticDamageTakenMultiplier = 1f;
    public float PreachManuscriptRefundChance = 0f;
    public float MinAdherenceFloor = 0f;
    public bool AntinomianGuard = false;
    public bool LegalismGuard = false;

    public void Merge(ConfessionModifiers other)
    {
        AdherenceDecayMultiplier *= other.AdherenceDecayMultiplier;
        SettlementAdherenceDecayMultiplier *= other.SettlementAdherenceDecayMultiplier;
        PreachAdherenceBonus += other.PreachAdherenceBonus;
        PreachSpiritualComfortBonus += other.PreachSpiritualComfortBonus;
        CantorComfortBonus += other.CantorComfortBonus;
        SpiritualComfortTurnBonus += other.SpiritualComfortTurnBonus;
        PopulationGrowthBonus += other.PopulationGrowthBonus;
        SettlementPopulationBonus += other.SettlementPopulationBonus;
        WildernessManuscriptBonus += other.WildernessManuscriptBonus;
        SettlementManuscriptBonus += other.SettlementManuscriptBonus;
        SoldierAttackBonus += other.SoldierAttackBonus;
        SoldierDefenseBonus += other.SoldierDefenseBonus;
        SiegePressureBonus += other.SiegePressureBonus;
        MissionaryMovementBonus += other.MissionaryMovementBonus;
        MissionaryAttackBonus += other.MissionaryAttackBonus;
        AllUnitsMovementBonus += other.AllUnitsMovementBonus;
        TerrainMovePenaltyReduction += other.TerrainMovePenaltyReduction;
        LawGospelDriftMultiplier *= other.LawGospelDriftMultiplier;
        LegalismDriftMultiplier *= other.LegalismDriftMultiplier;
        CivicRestraintGrowthMultiplier *= other.CivicRestraintGrowthMultiplier;
        SchismaticDamageTakenMultiplier *= other.SchismaticDamageTakenMultiplier;
        PreachManuscriptRefundChance += other.PreachManuscriptRefundChance;
        MinAdherenceFloor = System.Math.Max(MinAdherenceFloor, other.MinAdherenceFloor);
        AntinomianGuard |= other.AntinomianGuard;
        LegalismGuard |= other.LegalismGuard;
    }

    /// <summary>Scale bonuses by confessional adherence potency (0-1).</summary>
    public static ConfessionModifiers Scaled(ConfessionModifiers source, float potency)
    {
        if (potency <= 0f) return new ConfessionModifiers();
        if (potency >= 1f) return source;

        return new ConfessionModifiers
        {
            AdherenceDecayMultiplier = LerpMultiplier(1f, source.AdherenceDecayMultiplier, potency),
            SettlementAdherenceDecayMultiplier = LerpMultiplier(1f, source.SettlementAdherenceDecayMultiplier, potency),
            PreachAdherenceBonus = source.PreachAdherenceBonus * potency,
            PreachSpiritualComfortBonus = source.PreachSpiritualComfortBonus * potency,
            CantorComfortBonus = source.CantorComfortBonus * potency,
            SpiritualComfortTurnBonus = source.SpiritualComfortTurnBonus * potency,
            PopulationGrowthBonus = ScaleInt(source.PopulationGrowthBonus, potency),
            SettlementPopulationBonus = ScaleInt(source.SettlementPopulationBonus, potency),
            WildernessManuscriptBonus = ScaleInt(source.WildernessManuscriptBonus, potency),
            SettlementManuscriptBonus = ScaleInt(source.SettlementManuscriptBonus, potency),
            SoldierAttackBonus = ScaleInt(source.SoldierAttackBonus, potency),
            SoldierDefenseBonus = ScaleInt(source.SoldierDefenseBonus, potency),
            SiegePressureBonus = ScaleInt(source.SiegePressureBonus, potency),
            MissionaryMovementBonus = ScaleInt(source.MissionaryMovementBonus, potency),
            MissionaryAttackBonus = ScaleInt(source.MissionaryAttackBonus, potency),
            AllUnitsMovementBonus = ScaleInt(source.AllUnitsMovementBonus, potency),
            TerrainMovePenaltyReduction = ScaleInt(source.TerrainMovePenaltyReduction, potency),
            LawGospelDriftMultiplier = LerpMultiplier(1f, source.LawGospelDriftMultiplier, potency),
            LegalismDriftMultiplier = LerpMultiplier(1f, source.LegalismDriftMultiplier, potency),
            CivicRestraintGrowthMultiplier = LerpMultiplier(1f, source.CivicRestraintGrowthMultiplier, potency),
            SchismaticDamageTakenMultiplier = LerpMultiplier(1f, source.SchismaticDamageTakenMultiplier, potency),
            PreachManuscriptRefundChance = source.PreachManuscriptRefundChance * potency,
            MinAdherenceFloor = source.MinAdherenceFloor * potency,
            AntinomianGuard = source.AntinomianGuard && potency >= 0.6f,
            LegalismGuard = source.LegalismGuard && potency >= 0.6f
        };
    }

    static float LerpMultiplier(float neutral, float target, float t) => neutral + (target - neutral) * t;

    static int ScaleInt(int value, float t) => UnityEngine.Mathf.RoundToInt(value * t);

    public static ConfessionModifiers ForTech(ConfessionTechId id) => id switch
    {
        ConfessionTechId.LuthersCatechism => new ConfessionModifiers { PreachAdherenceBonus = 5f },
        ConfessionTechId.VerbalInspiration => new ConfessionModifiers { AdherenceDecayMultiplier = 0.75f },
        ConfessionTechId.LawAndGospel => new ConfessionModifiers { PreachAdherenceBonus = 10f },
        ConfessionTechId.SacramentalLife => new ConfessionModifiers { PopulationGrowthBonus = 1 },
        ConfessionTechId.ReformationHymnody => new ConfessionModifiers { PreachSpiritualComfortBonus = 10f },
        ConfessionTechId.AlbrechtDurer => new ConfessionModifiers { SettlementPopulationBonus = 1 },
        ConfessionTechId.LucasCranach => new ConfessionModifiers { PreachAdherenceBonus = 5f, LegalismDriftMultiplier = 0.85f },
        ConfessionTechId.OrderedCreation => new ConfessionModifiers { TerrainMovePenaltyReduction = 1 },
        ConfessionTechId.ConfessionalEmphasis => new ConfessionModifiers(),
        ConfessionTechId.ConfessionsCultureEmphasis => new ConfessionModifiers(),
        ConfessionTechId.AugsburgConfession => new ConfessionModifiers { SiegePressureBonus = 1 },
        ConfessionTechId.SmalcaldArticles => new ConfessionModifiers
        {
            LawGospelDriftMultiplier = 0.85f,
            SettlementManuscriptBonus = 1
        },
        ConfessionTechId.FormulaOfConcord => new ConfessionModifiers { AdherenceDecayMultiplier = 0.9f },
        ConfessionTechId.PaulGerhardt => new ConfessionModifiers { AdherenceDecayMultiplier = 0.9f },
        ConfessionTechId.ChoraleTradition => new ConfessionModifiers { CantorComfortBonus = 6f },
        ConfessionTechId.JohannesKepler => new ConfessionModifiers { AllUnitsMovementBonus = 1 },
        ConfessionTechId.CarlLinnaeus => new ConfessionModifiers { WildernessManuscriptBonus = 1 },
        ConfessionTechId.MartinChemnitz => new ConfessionModifiers { SoldierDefenseBonus = 2 },
        ConfessionTechId.JohannGerhard => new ConfessionModifiers { LegalismGuard = true, AdherenceDecayMultiplier = 0.9f },
        ConfessionTechId.AbrahamCalov => new ConfessionModifiers { SettlementManuscriptBonus = 1 },
        ConfessionTechId.IsaacNewton => new ConfessionModifiers { SoldierAttackBonus = 2 },
        ConfessionTechId.GregorMendel => new ConfessionModifiers { PopulationGrowthBonus = 1 },
        ConfessionTechId.SynodicalEmphasis => new ConfessionModifiers(),
        ConfessionTechId.WaltherPastoralTheology => new ConfessionModifiers { PreachSpiritualComfortBonus = 8f },
        ConfessionTechId.FrancisPieper => new ConfessionModifiers { AdherenceDecayMultiplier = 0.9f },
        ConfessionTechId.MissionarySending => new ConfessionModifiers { MissionaryMovementBonus = 1 },
        ConfessionTechId.JohannSebastianBach => new ConfessionModifiers { SpiritualComfortTurnBonus = 8f, PreachAdherenceBonus = 5f },
        ConfessionTechId.OttoVonGuericke => new ConfessionModifiers { SoldierDefenseBonus = 2, SiegePressureBonus = 2 },
        ConfessionTechId.MichaelFaraday => new ConfessionModifiers { AdherenceDecayMultiplier = 0.9f },
        ConfessionTechId.HermannSasse => new ConfessionModifiers { SchismaticDamageTakenMultiplier = 0.75f },
        ConfessionTechId.BoGiertz => new ConfessionModifiers { MissionaryAttackBonus = 2 },
        ConfessionTechId.RobertPreus => new ConfessionModifiers { PopulationGrowthBonus = 1 },
        ConfessionTechId.SynodicalGovernance => new ConfessionModifiers { PopulationGrowthBonus = 2 },
        ConfessionTechId.EdRiojas => new ConfessionModifiers
        {
            PopulationGrowthBonus = 1,
            SpiritualComfortTurnBonus = 1f
        },
        ConfessionTechId.JamesClerkMaxwell => new ConfessionModifiers { AllUnitsMovementBonus = 1, SoldierAttackBonus = 1, SiegePressureBonus = 1 },
        ConfessionTechId.LouisPasteur => new ConfessionModifiers { SettlementPopulationBonus = 1, PopulationGrowthBonus = 1 },

        ConfessionTechId.BondageOfWill => new ConfessionModifiers { AdherenceDecayMultiplier = 0.88f, PreachAdherenceBonus = 3f },
        ConfessionTechId.TwoKingdoms => new ConfessionModifiers { SoldierDefenseBonus = 2, LegalismDriftMultiplier = 0.88f },
        ConfessionTechId.LargeCatechism => new ConfessionModifiers { PreachAdherenceBonus = 5f, PreachManuscriptRefundChance = 0.15f },
        ConfessionTechId.GutenbergPress => new ConfessionModifiers { SettlementManuscriptBonus = 1, WildernessManuscriptBonus = 1 },
        ConfessionTechId.DavidChytraeus => new ConfessionModifiers { SoldierDefenseBonus = 1 },
        ConfessionTechId.NikolausSelnecker => new ConfessionModifiers { SpiritualComfortTurnBonus = 4f, CantorComfortBonus = 6f },
        ConfessionTechId.WilhelmLoehe => new ConfessionModifiers { MissionaryMovementBonus = 1, MissionaryAttackBonus = 1 },
        ConfessionTechId.CTCRReports => new ConfessionModifiers { AdherenceDecayMultiplier = 0.85f, MinAdherenceFloor = 50f },
        ConfessionTechId.NormanNagel => new ConfessionModifiers { PreachAdherenceBonus = 8f, LawGospelDriftMultiplier = 0.7f },
        ConfessionTechId.ConcordiaPublishing => new ConfessionModifiers { SettlementManuscriptBonus = 1, PopulationGrowthBonus = 1 },
        ConfessionTechId.WernerHeisenberg => new ConfessionModifiers { SoldierAttackBonus = 1, AdherenceDecayMultiplier = 0.92f },
        ConfessionTechId.GlobalLutheranFellowship => new ConfessionModifiers { MissionaryMovementBonus = 1, SpiritualComfortTurnBonus = 3f },
        ConfessionTechId.KurtMarquart => new ConfessionModifiers { PreachAdherenceBonus = 5f, SchismaticDamageTakenMultiplier = 0.85f },

        ConfessionTechId.EarthenVessels => new ConfessionModifiers { SettlementPopulationBonus = 1 },
        ConfessionTechId.ParishWalls => new ConfessionModifiers { SoldierDefenseBonus = 1 },
        ConfessionTechId.ParishGranary => new ConfessionModifiers { PopulationGrowthBonus = 1, SettlementPopulationBonus = 1 },
        ConfessionTechId.ShepherdsSling => new ConfessionModifiers { SoldierAttackBonus = 1 },
        _ => new ConfessionModifiers()
    };
}
