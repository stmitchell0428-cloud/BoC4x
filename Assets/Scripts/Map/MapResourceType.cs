public enum MapResourceType
{
    None = 0,
    Wheat,
    Cattle,
    Grapes,
    Fish,
    Timber,
    Stone,
    Iron,
    Coal,
    Gold
}

public static class MapResourceDatabase
{
    public static string DisplayName(MapResourceType type) => type switch
    {
        MapResourceType.Wheat => "Wheat",
        MapResourceType.Cattle => "Cattle",
        MapResourceType.Grapes => "Grapes",
        MapResourceType.Fish => "Fish",
        MapResourceType.Timber => "Timber",
        MapResourceType.Stone => "Stone",
        MapResourceType.Iron => "Iron",
        MapResourceType.Coal => "Coal",
        MapResourceType.Gold => "Gold",
        _ => ""
    };

    /// <summary>Tech that identifies this resource on the map once unlocked.</summary>
    public static ConfessionTechId RevealingTech(MapResourceType type) => type switch
    {
        MapResourceType.Wheat => ConfessionTechId.ParishGranary,
        MapResourceType.Cattle => ConfessionTechId.ParishGranary,
        MapResourceType.Grapes => ConfessionTechId.SacramentalLife,
        MapResourceType.Fish => ConfessionTechId.CarlLinnaeus,
        MapResourceType.Timber => ConfessionTechId.LucasCranach,
        MapResourceType.Stone => ConfessionTechId.TwoKingdoms,
        MapResourceType.Iron => ConfessionTechId.AugsburgConfession,
        MapResourceType.Coal => ConfessionTechId.MichaelFaraday,
        MapResourceType.Gold => ConfessionTechId.AbrahamCalov,
        _ => ConfessionTechId.LuthersCatechism
    };

    public static bool IsRevealedToPlayer(MapResourceType type)
    {
        if (type == MapResourceType.None)
            return false;

        return ConfessionResearchManager.Instance != null &&
               ConfessionResearchManager.Instance.IsTechUnlocked(RevealingTech(type));
    }

    public static MapResourceType VisibleResource(MapResourceType type) =>
        IsRevealedToPlayer(type) ? type : MapResourceType.None;

    public static string DisplayNameForPlayer(MapResourceType type)
    {
        if (type == MapResourceType.None)
            return "";
        return IsRevealedToPlayer(type) ? DisplayName(type) : "Unknown resource";
    }

    public static string RevealHint(MapResourceType type)
    {
        if (type == MapResourceType.None || IsRevealedToPlayer(type))
            return null;

        var tech = ConfessionTechDatabase.Get(RevealingTech(type));
        return $"Research <i>{tech.Name}</i> to identify";
    }

    public static string ResourcesRevealedLabel(ConfessionTechId tech)
    {
        var names = new System.Collections.Generic.List<string>();
        foreach (MapResourceType resource in System.Enum.GetValues(typeof(MapResourceType)))
        {
            if (resource == MapResourceType.None)
                continue;
            if (RevealingTech(resource) == tech)
                names.Add(DisplayName(resource));
        }

        if (names.Count == 0)
            return null;

        return "Reveals " + string.Join(" and ", names) + " on the map";
    }

    public static string ShortLabel(MapResourceType type) => type switch
    {
        MapResourceType.Wheat => "W",
        MapResourceType.Cattle => "A",
        MapResourceType.Grapes => "G",
        MapResourceType.Fish => "F",
        MapResourceType.Timber => "T",
        MapResourceType.Stone => "S",
        MapResourceType.Iron => "I",
        MapResourceType.Coal => "C",
        MapResourceType.Gold => "$",
        _ => ""
    };

    public static bool IsValidOnTerrain(MapResourceType resource, TerrainType terrain)
    {
        if (resource == MapResourceType.None) return false;

        return resource switch
        {
            MapResourceType.Wheat => terrain is TerrainType.Pasture or TerrainType.Shore,
            MapResourceType.Cattle => terrain == TerrainType.Pasture,
            MapResourceType.Grapes => terrain is TerrainType.Pasture or TerrainType.Hill,
            MapResourceType.Fish => terrain == TerrainType.Shore,
            MapResourceType.Timber => terrain == TerrainType.Forest,
            MapResourceType.Stone => terrain is TerrainType.Hill or TerrainType.Wilderness,
            MapResourceType.Iron => terrain is TerrainType.Hill or TerrainType.Forest,
            MapResourceType.Coal => terrain is TerrainType.Hill or TerrainType.Forest,
            MapResourceType.Gold => terrain is TerrainType.Hill or TerrainType.River,
            _ => false
        };
    }

    public static TileYield BonusYield(MapResourceType resource) => resource switch
    {
        MapResourceType.Wheat => new TileYield(2, 0, 0),
        MapResourceType.Cattle => new TileYield(2, 0, 0),
        MapResourceType.Grapes => new TileYield(1, 1, 0),
        MapResourceType.Fish => new TileYield(2, 0, 0),
        MapResourceType.Timber => new TileYield(0, 2, 0),
        MapResourceType.Stone => new TileYield(0, 2, 0),
        MapResourceType.Iron => new TileYield(0, 3, 0),
        MapResourceType.Coal => new TileYield(0, 2, 0),
        MapResourceType.Gold => new TileYield(0, 0, 2),
        _ => default
    };

    public static string ManuscriptNote(MapResourceType resource) => resource switch
    {
        MapResourceType.Gold => "+2 manuscripts/turn when worked",
        _ => null
    };

    public static int MissionaryManuscriptBonus(TerrainType terrain) => terrain switch
    {
        TerrainType.Shore => 1,
        TerrainType.Forest => 1,
        TerrainType.Hill => 1,
        TerrainType.Wilderness => 1,
        _ => 0
    };
}
