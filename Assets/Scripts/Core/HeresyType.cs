using System.Linq;

/// <summary>Historical heresy flavors for repeatable schisms  -  each tweaks one growth/confession axis.</summary>
public enum HeresyType
{
    None = 0,
    Legalism,
    Antinomian,
    DoctrinalDrift,
    Enthusiasm,
    Sacramentarian,
    Calvinist
}

public enum SchismaticBlocId
{
    None = 0,
    Bloc1 = 1,
    Bloc2 = 2,
    Bloc3 = 3
}

public enum HeresyAxisTweak
{
    Appeal,
    Restraint,
    Doctrine
}

public readonly struct HeresyProfile
{
    public readonly HeresyType Type;
    public readonly string DisplayName;
    public readonly string CapitalSuffix;
    public readonly HeresyAxisTweak AxisTweak;
    public readonly float Adherence;
    public readonly float SpiritualComfort;
    public readonly float CivicRestraint;
    public readonly float MigrationMultiplier;
    public readonly float SecularAppealBonus;
    public readonly float SpiritualAppealBonus;
    public readonly string TensionLabel;
    public readonly bool PreferMissionaries;
    public readonly bool PreferSoldiers;
    public readonly bool PreferRanged;

    public HeresyProfile(
        HeresyType type,
        string displayName,
        string capitalSuffix,
        HeresyAxisTweak axisTweak,
        float adherence,
        float spiritualComfort,
        float civicRestraint,
        float migrationMultiplier,
        float secularAppealBonus,
        float spiritualAppealBonus,
        string tensionLabel,
        bool preferMissionaries,
        bool preferSoldiers,
        bool preferRanged = false)
    {
        Type = type;
        DisplayName = displayName;
        CapitalSuffix = capitalSuffix;
        AxisTweak = axisTweak;
        Adherence = adherence;
        SpiritualComfort = spiritualComfort;
        CivicRestraint = civicRestraint;
        MigrationMultiplier = migrationMultiplier;
        SecularAppealBonus = secularAppealBonus;
        SpiritualAppealBonus = spiritualAppealBonus;
        TensionLabel = tensionLabel;
        PreferMissionaries = preferMissionaries;
        PreferSoldiers = preferSoldiers;
        PreferRanged = preferRanged;
    }
}

public static class HeresyDatabase
{
    public static HeresyProfile ProfileFor(HeresyType type) => type switch
    {
        HeresyType.Legalism => new HeresyProfile(
            HeresyType.Legalism, "Pharisaic Synod", "Pharisaic Synod",
            HeresyAxisTweak.Restraint, 52f, 28f, 82f, 0.85f, 4f, -6f,
            "Rigid legalism", preferMissionaries: false, preferSoldiers: true),
        HeresyType.Antinomian => new HeresyProfile(
            HeresyType.Antinomian, "Libertine Congregation", "Libertine Congregation",
            HeresyAxisTweak.Appeal, 34f, 78f, 22f, 1.15f, 6f, 8f,
            "Gospel without Law", preferMissionaries: true, preferSoldiers: false),
        HeresyType.DoctrinalDrift => new HeresyProfile(
            HeresyType.DoctrinalDrift, "Augsburg Dissent", "Augsburg Dissent",
            HeresyAxisTweak.Doctrine, 38f, 42f, 62f, 1f, 0f, 0f,
            "Rigid dissent", preferMissionaries: true, preferSoldiers: true, preferRanged: true),
        HeresyType.Enthusiasm => new HeresyProfile(
            HeresyType.Enthusiasm, "Schwaermer Circle", "Schwaermer Circle",
            HeresyAxisTweak.Appeal, 30f, 88f, 35f, 1.2f, -2f, 12f,
            "Enthusiast fervor", preferMissionaries: true, preferSoldiers: false),
        HeresyType.Sacramentarian => new HeresyProfile(
            HeresyType.Sacramentarian, "Zwingli Remnant", "Zwingli Remnant",
            HeresyAxisTweak.Doctrine, 44f, 50f, 55f, 0.95f, 3f, -4f,
            "Memorialist drift", preferMissionaries: false, preferSoldiers: true, preferRanged: true),
        HeresyType.Calvinist => new HeresyProfile(
            HeresyType.Calvinist, "Geneva Colloquy", "Geneva Colloquy",
            HeresyAxisTweak.Restraint, 48f, 38f, 74f, 0.9f, 5f, -3f,
            "Double predestination rigor", preferMissionaries: false, preferSoldiers: true, preferRanged: true),
        _ => ProfileFor(HeresyType.DoctrinalDrift)
    };

    public static HeresyType ForCrisis(CrisisType crisis) => crisis switch
    {
        CrisisType.Legalism => HeresyType.Legalism,
        CrisisType.Antinomian => HeresyType.Antinomian,
        CrisisType.DoctrinalDrift => HeresyType.DoctrinalDrift,
        _ => HeresyType.DoctrinalDrift
    };

    public static HeresyType[] GetHeresyPool(HeresyPackId pack) => pack switch
    {
        HeresyPackId.ReformationCore => new[]
        {
            HeresyType.Legalism,
            HeresyType.Antinomian,
            HeresyType.DoctrinalDrift
        },
        HeresyPackId.RadicalFringe => new[]
        {
            HeresyType.Enthusiasm,
            HeresyType.Sacramentarian,
            HeresyType.Calvinist
        },
        _ => new[]
        {
            HeresyType.Legalism,
            HeresyType.Antinomian,
            HeresyType.DoctrinalDrift,
            HeresyType.Enthusiasm,
            HeresyType.Sacramentarian,
            HeresyType.Calvinist
        }
    };

    public static HeresyType PickRepeatHeresy(
        System.Collections.Generic.IReadOnlyCollection<HeresyType> active,
        HeresyPackId pack = HeresyPackId.FullCanon)
    {
        foreach (var candidate in GetHeresyPool(pack))
        {
            if (!active.Contains(candidate))
                return candidate;
        }

        return GetHeresyPool(pack)[0];
    }

    public static HeresyType PickHeresyForCrisis(
        CrisisType crisis,
        bool isRepeat,
        System.Collections.Generic.IReadOnlyCollection<HeresyType> active,
        HeresyPackId pack)
    {
        if (!isRepeat)
        {
            var crisisHeresy = ForCrisis(crisis);
            if (System.Array.IndexOf(GetHeresyPool(pack), crisisHeresy) >= 0 &&
                !active.Contains(crisisHeresy))
                return crisisHeresy;
        }

        return PickRepeatHeresy(active, pack);
    }
}
