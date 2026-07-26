public enum TerrainType
{
    Wilderness,
    Pasture,
    Forest,
    Hill,
    Ocean,
    Shore,
    Lake,
    River
}

public static class TerrainRules
{
    public static bool IsWater(TerrainType terrain) => terrain switch
    {
        TerrainType.Ocean or TerrainType.Lake or TerrainType.River => true,
        _ => false
    };

    public static bool IsPassable(TerrainType terrain) => !IsWater(terrain);
}

public enum FactionId
{
    None = 0,
    LutheranSynod = 1,
    Schismatic = 2
}

/// <summary>Human is Player1; lobby slots 2–4 are AI rival synods (same faction, separate turns).</summary>
public enum SynodPlayerId
{
    None = 0,
    Player1 = 1,
    Player2 = 2,
    Player3 = 3,
    Player4 = 4
}

public enum UnitType
{
    Settler,
    Scout,
    Missionary,
    Soldier,
    Chaplain,
    Cantor,
    Defender,
    Slinger,
    Archer,
    Horseman,
    Pastor,
    Bishop,
    Archbishop,
    Deaconess,
    SiegeEngine,
    CoastalPatrol,
    CoastalGalley
}
