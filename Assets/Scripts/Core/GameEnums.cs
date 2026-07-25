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

public enum UnitType
{
    Settler,
    Scout,
    Missionary,
    Soldier,
    Chaplain,
    Cantor,
    Defender,
    Colonist,
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
