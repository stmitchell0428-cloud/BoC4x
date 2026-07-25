using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct CityPlacementScore
{
    public bool IsValid;
    public string InvalidReason;
    public int Score;
    public TileYield CenterYield;
    public TileYield AdjacentTotal;
    public TileYield TerritoryTotal;
    public int AdjacentPassableCount;
    public int TerritoryPassableCount;
    public int ManuscriptTilesNearby;
    public string HighlightResource;

    public string Rating => !IsValid ? "Invalid" : Score switch
    {
        >= 100 => "Excellent",
        >= 72 => "Good",
        >= 48 => "Fair",
        _ => "Poor"
    };

    public string RatingColor => Rating switch
    {
        "Excellent" => "#88FFAA",
        "Good" => "#DDEE88",
        "Fair" => "#FFCC88",
        "Poor" => "#CCAA88",
        "Invalid" => "#FF8888",
        _ => "#CCCCCC"
    };

    public string FormatCompact()
    {
        if (!IsValid)
            return $"<color=#FF8888>{InvalidReason}</color>";

        var sb = new StringBuilder();
        sb.Append("<color=").Append(RatingColor).Append("><b>").Append(Rating).Append(" (").Append(Score).Append(")</b></color>");
        sb.Append("  -  center ").Append(CenterYield.FormatCompact());
        sb.Append(" | adjacent ").Append(AdjacentTotal.FormatCompact());
        if (TerritoryPassableCount > AdjacentPassableCount + 1)
            sb.Append(" | 4-hex lands ").Append(TerritoryTotal.FormatCompact());
        if (ManuscriptTilesNearby > 0)
            sb.Append(" | <color=#EECC66>").Append(ManuscriptTilesNearby).Append(" mss tile(s)</color>");
        return sb.ToString();
    }

    public string FormatSiteLabel(HexCoordinates hex)
    {
        string terrain = "land";
        if (HexGridMap.Instance != null && HexGridMap.Instance.TryGetTile(hex, out var tile))
        {
            terrain = HexGridMap.TerrainDisplayName(tile.Terrain);
            if (tile.Resource != MapResourceType.None && MapResourceDatabase.IsRevealedToPlayer(tile.Resource))
                terrain += "+" + MapResourceDatabase.DisplayName(tile.Resource);
        }
        return $"{terrain} <color={RatingColor}>{Rating} {Score}</color>";
    }
}

public static class CityPlacementAdvisor
{
    static List<(HexCoordinates hex, CityPlacementScore score)> cachedCapitalSites;
    static readonly Dictionary<City, List<(HexCoordinates hex, CityPlacementScore score)>> cachedDistrictSites = new();

    public static void InvalidateCache()
    {
        cachedCapitalSites = null;
        cachedDistrictSites.Clear();
    }

    public static CityPlacementScore EvaluateCapitalSite(HexCoordinates hex)
    {
        var result = new CityPlacementScore();
        if (HexGridMap.Instance == null || CityManager.Instance == null)
        {
            result.IsValid = false;
            result.InvalidReason = "Map not ready";
            return result;
        }

        hex = HexGridMap.Instance.Wrap(hex);
        if (!HexGridMap.Instance.TryGetTile(hex, out var centerTile))
        {
            result.IsValid = false;
            result.InvalidReason = "Off map";
            return result;
        }

        if (!TerrainRules.IsPassable(centerTile.Terrain))
        {
            result.IsValid = false;
            result.InvalidReason = "Water or impassable";
            return result;
        }

        if (centerTile.Settlement != null)
        {
            result.IsValid = false;
            result.InvalidReason = "City already here";
            return result;
        }

        if (CityManager.Instance.IsTooCloseToIndependentCity(hex))
        {
            result.IsValid = false;
            result.InvalidReason = $"Too close (need {CityManager.MinCitySeparation}+ hexes from cities)";
            return result;
        }

        result.IsValid = true;
        result.CenterYield = TileYieldDatabase.GetVisibleTileYield(centerTile);
        result.HighlightResource = centerTile.Resource != MapResourceType.None &&
                                   MapResourceDatabase.IsRevealedToPlayer(centerTile.Resource)
            ? MapResourceDatabase.DisplayName(centerTile.Resource)
            : null;

        var adjacent = new List<HexTile>();
        foreach (var n in HexGridMap.Instance.GetWrappedNeighbors(hex))
        {
            if (!HexGridMap.Instance.TryGetTile(n, out var nTile)) continue;
            if (!TerrainRules.IsPassable(nTile.Terrain)) continue;
            adjacent.Add(nTile);
            result.AdjacentTotal += TileYieldDatabase.GetVisibleTileYield(nTile);
        }
        result.AdjacentPassableCount = adjacent.Count;

        var territory = CollectTerritoryTiles(hex, CityManager.MaxTerritoryRadius);
        result.TerritoryPassableCount = territory.Count;
        foreach (var t in territory)
        {
            var y = TileYieldDatabase.GetVisibleTileYield(t);
            result.TerritoryTotal += y;
            if (y.Manuscripts > 0)
                result.ManuscriptTilesNearby++;
            if (result.HighlightResource == null &&
                t.Resource != MapResourceType.None &&
                MapResourceDatabase.IsRevealedToPlayer(t.Resource))
                result.HighlightResource = MapResourceDatabase.DisplayName(t.Resource);
        }

        int score = result.CenterYield.WorkPriority * 4;
        score += result.AdjacentTotal.WorkPriority * 2;
        score += result.TerritoryTotal.WorkPriority;
        score += result.ManuscriptTilesNearby * 10;
        score += result.AdjacentTotal.Food;
        score += result.CenterYield.Production * 2;
        result.Score = score;
        return result;
    }

    public static CityPlacementScore EvaluateDistrictSite(HexCoordinates hex, City parentCity)
    {
        var result = new CityPlacementScore();
        if (CityManager.Instance == null || parentCity == null)
        {
            result.IsValid = false;
            result.InvalidReason = "No parent city";
            return result;
        }

        if (!CityManager.Instance.IsValidHamletDistrictSite(hex, parentCity))
        {
            result.IsValid = false;
            result.InvalidReason = "Not a valid district site";
            return result;
        }

        result.IsValid = true;
        if (HexGridMap.Instance.TryGetTile(hex, out var centerTile))
        {
            result.CenterYield = TileYieldDatabase.GetVisibleTileYield(centerTile);
            if (centerTile.Resource != MapResourceType.None &&
                MapResourceDatabase.IsRevealedToPlayer(centerTile.Resource))
                result.HighlightResource = MapResourceDatabase.DisplayName(centerTile.Resource);
        }

        foreach (var n in HexGridMap.Instance.GetWrappedNeighbors(hex))
        {
            if (!HexGridMap.Instance.TryGetTile(n, out var nTile)) continue;
            if (!TerrainRules.IsPassable(nTile.Terrain)) continue;
            result.AdjacentTotal += TileYieldDatabase.GetVisibleTileYield(nTile);
            result.AdjacentPassableCount++;
            var y = TileYieldDatabase.GetVisibleTileYield(nTile);
            if (y.Manuscripts > 0) result.ManuscriptTilesNearby++;
        }

        result.Score = result.CenterYield.WorkPriority * 3 + result.AdjacentTotal.WorkPriority * 2 +
                       result.ManuscriptTilesNearby * 8;
        return result;
    }

    public static List<(HexCoordinates hex, CityPlacementScore score)> GetTopCapitalSites(int count = 3)
    {
        if (cachedCapitalSites != null)
            return cachedCapitalSites.Take(count).ToList();

        var ranked = RankCapitalSites();
        cachedCapitalSites = ranked;
        return ranked.Take(count).ToList();
    }

    public static List<(HexCoordinates hex, CityPlacementScore score)> GetTopDistrictSites(City parent, int count = 3)
    {
        if (parent != null && cachedDistrictSites.TryGetValue(parent, out var cached))
            return cached.Take(count).ToList();

        var ranked = RankDistrictSites(parent);
        if (parent != null)
            cachedDistrictSites[parent] = ranked;
        return ranked.Take(count).ToList();
    }

    static List<(HexCoordinates hex, CityPlacementScore score)> RankCapitalSites()
    {
        var ranked = new List<(HexCoordinates hex, CityPlacementScore score)>();
        if (HexGridMap.Instance == null) return ranked;

        foreach (var tile in HexGridMap.Instance.AllTiles)
        {
            if (FogOfWarManager.Instance != null &&
                FogOfWarManager.Instance.GetVisibility(tile.Coordinates) == FogVisibility.Unexplored)
                continue;

            var score = EvaluateCapitalSite(tile.Coordinates);
            if (!score.IsValid) continue;
            ranked.Add((tile.Coordinates, score));
        }

        return ranked.OrderByDescending(e => e.score.Score).ToList();
    }

    static List<(HexCoordinates hex, CityPlacementScore score)> RankDistrictSites(City parent)
    {
        var ranked = new List<(HexCoordinates hex, CityPlacementScore score)>();
        if (HexGridMap.Instance == null || parent == null) return ranked;

        foreach (var tile in HexGridMap.Instance.AllTiles)
        {
            if (FogOfWarManager.Instance != null &&
                FogOfWarManager.Instance.GetVisibility(tile.Coordinates) == FogVisibility.Unexplored)
                continue;

            var score = EvaluateDistrictSite(tile.Coordinates, parent);
            if (!score.IsValid) continue;
            ranked.Add((tile.Coordinates, score));
        }

        return ranked.OrderByDescending(e => e.score.Score).ToList();
    }

    public static int GetPlacementHighlightTier(HexCoordinates hex, IList<(HexCoordinates hex, CityPlacementScore score)> topSites)
    {
        if (topSites == null || topSites.Count == 0) return 0;
        hex = HexGridMap.Instance != null ? HexGridMap.Instance.Wrap(hex) : hex;
        for (int i = 0; i < topSites.Count; i++)
        {
            var siteHex = HexGridMap.Instance != null
                ? HexGridMap.Instance.Wrap(topSites[i].hex)
                : topSites[i].hex;
            if (siteHex != hex) continue;
            if (topSites[i].score.Score < 48) return 0;
            return i == 0 ? 2 : 1;
        }
        return 0;
    }

    static List<HexTile> CollectTerritoryTiles(HexCoordinates center, int maxRadius)
    {
        var tiles = new List<HexTile>();
        if (HexGridMap.Instance == null) return tiles;

        center = HexGridMap.Instance.Wrap(center);
        var visited = new HashSet<HexCoordinates>();
        var queue = new Queue<(HexCoordinates coords, int dist)>();
        queue.Enqueue((center, 0));

        while (queue.Count > 0)
        {
            var (coords, dist) = queue.Dequeue();
            if (!visited.Add(coords)) continue;
            if (dist > maxRadius) continue;
            if (!HexGridMap.Instance.TryGetTile(coords, out var tile)) continue;
            if (!TerrainRules.IsPassable(tile.Terrain)) continue;

            tiles.Add(tile);
            if (dist >= maxRadius) continue;
            foreach (var n in HexGridMap.Instance.GetWrappedNeighbors(coords))
                queue.Enqueue((n, dist + 1));
        }

        return tiles;
    }
}
