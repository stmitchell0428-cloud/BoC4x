using System.Collections.Generic;

public enum HamletSpecialty
{
    None = 0,
    Seminary,
    Garrison,
    Market,
    Scholastic
}

public class HamletSpecialtyDefinition
{
    public HamletSpecialty Id;
    public string Name;
    public string Subtitle;
    public string Description;
    public readonly HashSet<CityBuildId> AllowedBuilds = new();
    public readonly HashSet<UnitUpgradeId> AllowedUpgrades = new();

    public HamletSpecialtyDefinition(
        HamletSpecialty id,
        string name,
        string subtitle,
        string description,
        IEnumerable<CityBuildId> builds,
        IEnumerable<UnitUpgradeId> upgrades = null)
    {
        Id = id;
        Name = name;
        Subtitle = subtitle;
        Description = description;
        foreach (var b in builds)
            AllowedBuilds.Add(b);
        if (upgrades != null)
        {
            foreach (var u in upgrades)
                AllowedUpgrades.Add(u);
        }
    }
}

public static class HamletSpecialtyDatabase
{
    static readonly Dictionary<HamletSpecialty, HamletSpecialtyDefinition> defs = Build();
    static readonly HashSet<CityBuildId> districtExclusive = BuildDistrictExclusive();

    public static HamletSpecialtyDefinition Get(HamletSpecialty id) =>
        defs.TryGetValue(id, out var def) ? def : null;

    public static IEnumerable<HamletSpecialtyDefinition> All => defs.Values;

    public static bool IsDistrictExclusive(CityBuildId id) => districtExclusive.Contains(id);

    public static bool IsBuildAllowed(City city, CityBuildId id)
    {
        if (city == null)
            return false;

        if (!city.IsHamlet)
            return !IsDistrictExclusive(id);

        if (city.Specialty == HamletSpecialty.None)
            return false;

        return Get(city.Specialty)?.AllowedBuilds.Contains(id) == true;
    }

    public static bool IsUpgradeAllowed(City city, UnitUpgradeId id)
    {
        if (city == null || !city.IsHamlet)
            return true;
        if (city.Specialty == HamletSpecialty.None)
            return false;
        return Get(city.Specialty)?.AllowedUpgrades.Contains(id) == true;
    }

    public static string DisplayName(HamletSpecialty id) =>
        Get(id)?.Name ?? "Unset";

    static HashSet<CityBuildId> BuildDistrictExclusive()
    {
        var set = new HashSet<CityBuildId>
        {
            CityBuildId.TrainPastor,
            CityBuildId.TrainDeaconess,
            CityBuildId.TrainCantor,
            CityBuildId.TrainArcher,
            CityBuildId.TrainHorseman,
            CityBuildId.BuildOrganLoft,
            CityBuildId.BuildParishChurch,
            CityBuildId.BuildBarracks,
            CityBuildId.BuildArcheryRange,
            CityBuildId.BuildStable,
            CityBuildId.BuildArmory,
            CityBuildId.BuildWatchtower,
            CityBuildId.BuildMarketHall,
            CityBuildId.BuildMill,
            CityBuildId.TrainCoastalPatrol,
            CityBuildId.BuildDock,
            CityBuildId.TrainCoastalGalley
        };
        return set;
    }

    static Dictionary<HamletSpecialty, HamletSpecialtyDefinition> Build() => new()
    {
        [HamletSpecialty.Seminary] = new HamletSpecialtyDefinition(
            HamletSpecialty.Seminary,
            "Seminary District",
            "Word, hymn, and pastoral office",
            "Trains missionaries, pastors, deaconesses; pastor->bishop/chaplain upgrades.",
            new[]
            {
                CityBuildId.TrainMissionary,
                CityBuildId.TrainPastor,
                CityBuildId.TrainDeaconess,
                CityBuildId.TrainCantor,
                CityBuildId.BuildChapel,
                CityBuildId.BuildParishSchool,
                CityBuildId.BuildScriptorium,
                CityBuildId.BuildParishChurch,
                CityBuildId.BuildOrganLoft,
                CityBuildId.BuildOrphanage,
                CityBuildId.BuildHospital,
                CityBuildId.BindCatechism
            },
            new[]
            {
                UnitUpgradeId.MissionaryToPastor,
                UnitUpgradeId.PastorToChaplain,
                UnitUpgradeId.PastorToBishop,
                UnitUpgradeId.BishopToArchbishop
            }),

        [HamletSpecialty.Garrison] = new HamletSpecialtyDefinition(
            HamletSpecialty.Garrison,
            "Garrison District",
            "Sword, bow, and horse  -  siege engines planned for Tier 5-6",
            "Trains soldiers, slingers, archers, siege engines; breaching at Maxwell tier.",
            new[]
            {
                CityBuildId.TrainSoldier,
                CityBuildId.TrainSlinger,
                CityBuildId.TrainArcher,
                CityBuildId.TrainHorseman,
                CityBuildId.TrainSiegeEngine,
                CityBuildId.BuildBarracks,
                CityBuildId.BuildArcheryRange,
                CityBuildId.BuildStable,
                CityBuildId.BuildArmory,
                CityBuildId.BuildFortification,
                CityBuildId.BuildWatchtower
            },
            new[] { UnitUpgradeId.SoldierToDefender }),

        [HamletSpecialty.Market] = new HamletSpecialtyDefinition(
            HamletSpecialty.Market,
            "Market District",
            "Trade, craft, and frontier expansion",
            "Trains colonists, scouts, coastal patrol, and galleys; builds dock and frontier craft.",
            new[]
            {
                CityBuildId.TrainColonist,
                CityBuildId.TrainScout,
                CityBuildId.TrainCoastalPatrol,
                CityBuildId.TrainCoastalGalley,
                CityBuildId.BuildGuildWorkshop,
                CityBuildId.BuildPotteryWorkshop,
                CityBuildId.BuildGranary,
                CityBuildId.BuildMarketHall,
                CityBuildId.BuildMill,
                CityBuildId.BuildPrintingPress,
                CityBuildId.BuildMissionHouse,
                CityBuildId.BuildDock
            }),

        [HamletSpecialty.Scholastic] = new HamletSpecialtyDefinition(
            HamletSpecialty.Scholastic,
            "Scholastic District",
            "Libraries, loci, and natural philosophy",
            "Research and manuscript production for the synod.",
            new[]
            {
                CityBuildId.TrainMissionary,
                CityBuildId.BuildScriptorium,
                CityBuildId.BuildLibrary,
                CityBuildId.BuildUniversity,
                CityBuildId.BuildObservatory,
                CityBuildId.BindCatechism,
                CityBuildId.BuildParishSchool
            },
            new[] { UnitUpgradeId.MissionaryToPastor })
    };
}
