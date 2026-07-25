using System.Text;

public struct TileYield
{
    public int Food;
    public int Production;
    public int Manuscripts;

    public TileYield(int food, int production, int manuscripts)
    {
        Food = food;
        Production = production;
        Manuscripts = manuscripts;
    }

    public int WorkPriority => Food * 4 + Production * 2 + Manuscripts * 3;

    public static TileYield operator +(TileYield a, TileYield b) =>
        new(a.Food + b.Food, a.Production + b.Production, a.Manuscripts + b.Manuscripts);

    public string FormatCompact()
    {
        var parts = new StringBuilder();
        if (Food > 0) parts.Append(Food).Append(" food");
        if (Production > 0)
        {
            if (parts.Length > 0) parts.Append(", ");
            parts.Append(Production).Append(" prod");
        }
        if (Manuscripts > 0)
        {
            if (parts.Length > 0) parts.Append(", ");
            parts.Append(Manuscripts).Append(" mss");
        }
        return parts.Length > 0 ? parts.ToString() : "0";
    }

    public string FormatForHover(bool isWorked)
    {
        var parts = new StringBuilder();
        if (Food > 0) parts.Append(Food).Append(" food");
        if (Production > 0)
        {
            if (parts.Length > 0) parts.Append(", ");
            parts.Append(Production).Append(" prod");
        }
        if (Manuscripts > 0)
        {
            if (parts.Length > 0) parts.Append(", ");
            parts.Append("<color=#EECC66><b>").Append(Manuscripts).Append(" mss");
            parts.Append(isWorked ? "/turn</b></color>" : " if worked</b></color>");
        }
        return parts.Length > 0 ? parts.ToString() : "none";
    }
}

public static class TileYieldDatabase
{
    public static TileYield GetTerrainYield(TerrainType terrain) => terrain switch
    {
        TerrainType.Pasture => new TileYield(2, 0, 0),
        TerrainType.Forest => new TileYield(1, 1, 0),
        TerrainType.Hill => new TileYield(0, 2, 0),
        TerrainType.Wilderness => new TileYield(1, 0, 0),
        TerrainType.Shore => new TileYield(2, 1, 0),
        TerrainType.River => new TileYield(1, 1, 0),
        _ => default
    };

    public static TileYield GetTileYield(TerrainType terrain, MapResourceType resource)
    {
        var yield = GetTerrainYield(terrain);
        if (resource != MapResourceType.None)
            yield += MapResourceDatabase.BonusYield(resource);
        return yield;
    }

    public static TileYield GetTileYield(HexTile tile)
    {
        if (tile == null) return default;
        return GetTileYield(tile.Terrain, MapResourceDatabase.VisibleResource(tile.Resource));
    }

    public static TileYield GetVisibleTileYield(HexTile tile) => GetTileYield(tile);

    public static TileYield GetVisibleTileYield(TerrainType terrain, MapResourceType resource) =>
        GetTileYield(terrain, MapResourceDatabase.VisibleResource(resource));
}
