using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct FactionSpawnLayout
{
    public readonly HexCoordinates SynodSettler;
    public readonly HexCoordinates SynodScout;

    public FactionSpawnLayout(HexCoordinates synodSettler, HexCoordinates synodScout)
    {
        SynodSettler = synodSettler;
        SynodScout = synodScout;
    }
}

public class HexGridMap : MonoBehaviour
{
    [Header("Map Size")]
    [Tooltip("Sized for ~4 players x 4-6 cities with wilderness between clusters (toroidal wrap).")]
    public int gridWidthCols = 64;
    public int gridHeightRows = 42;

    [Header("Map Topology")]
    [Tooltip("Legacy bool  -  derived from wrapStyle when lobby applies settings.")]
    public bool wrapMap = true;
    public MapWrapStyle wrapStyle = MapWrapStyle.Toroidal;
    public CoastalDensity coastalDensity = CoastalDensity.Normal;

    [Header("Hex Metrics")]
    [Tooltip("Outer radius  -  auto-calibrated from prefab sprite if left at 0.")]
    public float hexRadiusSize;
    public float tileScale = 1.4f;

    [Header("Prefabs")]
    public GameObject hexPrefabBlueprint;

    [Header("Map Generation")]
    [Tooltip("0 = random layout each play. Non-zero values reproduce the same map.")]
    public int mapSeed;

    readonly Dictionary<HexCoordinates, HexTile> tiles = new();
    Vector3 mapOriginOffset;

    HexCoordinates movementCostOrigin;
    int movementCostRange;
    FactionId movementCostFaction;
    UnitType movementCostUnitType;
    Dictionary<HexCoordinates, int> movementCostCache;

    public static HexGridMap Instance { get; private set; }
    public float HexSize => hexRadiusSize;
    public FactionSpawnLayout SpawnLayout { get; private set; }
    public int NavalCoastTileCount { get; private set; }
    public int NavigableWaterTileCount { get; private set; }
    public int DeepWaterTileCount { get; private set; }

    void Awake()
    {
        Instance = this;
        CalibrateHexSizeFromPrefab();
        EnsureGameSystems();
    }

    void Start()
    {
        // Map generation waits for MatchLobbyController.BeginMatch().
    }

    public void ApplyMatchSettings(MatchSettings settings)
    {
        if (settings == null) return;

        gridWidthCols = Mathf.Max(16, settings.MapWidth);
        gridHeightRows = Mathf.Max(12, settings.MapHeight);
        mapSeed = settings.MapSeed;
        wrapStyle = settings.WrapStyle;
        coastalDensity = settings.CoastalDensity;
        wrapMap = wrapStyle != MapWrapStyle.Bounded;
    }

    void CalibrateHexSizeFromPrefab()
    {
        if (hexPrefabBlueprint == null || hexRadiusSize > 0f) return;

        var sr = hexPrefabBlueprint.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        // Flat-top hex sprite: width = 2 x outer radius
        hexRadiusSize = sr.sprite.bounds.size.x * 0.5f * tileScale;
    }

    void EnsureGameSystems()
    {
        if (FindAnyObjectByType<TurnManager>() == null)
            gameObject.AddComponent<TurnManager>();
        if (FindAnyObjectByType<HexSelectionController>() == null)
            gameObject.AddComponent<HexSelectionController>();
        if (FindAnyObjectByType<SimpleAI>() == null)
            gameObject.AddComponent<SimpleAI>();
        if (FindAnyObjectByType<GameBootstrap>() == null)
            gameObject.AddComponent<GameBootstrap>();
        if (FindAnyObjectByType<GameHUD>() == null)
            gameObject.AddComponent<GameHUD>();
        if (FindAnyObjectByType<ConfessionResearchManager>() == null)
            gameObject.AddComponent<ConfessionResearchManager>();
        if (FindAnyObjectByType<ConfessionTechPanel>() == null)
            gameObject.AddComponent<ConfessionTechPanel>();
        if (FindAnyObjectByType<TerrainInfoPanel>() == null)
            gameObject.AddComponent<TerrainInfoPanel>();
        if (FindAnyObjectByType<CityManager>() == null)
            gameObject.AddComponent<CityManager>();
        if (FindAnyObjectByType<CityScreenPanel>() == null)
            gameObject.AddComponent<CityScreenPanel>();
        if (FindAnyObjectByType<MatchController>() == null)
            gameObject.AddComponent<MatchController>();
        if (FindAnyObjectByType<MatchEndPanel>() == null)
            gameObject.AddComponent<MatchEndPanel>();
        if (FindAnyObjectByType<TurnPhaseBanner>() == null)
            gameObject.AddComponent<TurnPhaseBanner>();
        if (FindAnyObjectByType<FogOfWarManager>() == null)
            gameObject.AddComponent<FogOfWarManager>();
        if (FindAnyObjectByType<SchismManager>() == null)
            gameObject.AddComponent<SchismManager>();
        if (FindAnyObjectByType<SchismaticBlocRegistry>() == null)
            gameObject.AddComponent<SchismaticBlocRegistry>();
        if (FindAnyObjectByType<SchismEventPanel>() == null)
            gameObject.AddComponent<SchismEventPanel>();
        if (FindAnyObjectByType<CrisisCardPanel>() == null)
            gameObject.AddComponent<CrisisCardPanel>();
        if (FindAnyObjectByType<LegacySlotPickerPanel>() == null)
            gameObject.AddComponent<LegacySlotPickerPanel>();
        if (FindAnyObjectByType<EndTurnPhaseController>() == null)
            gameObject.AddComponent<EndTurnPhaseController>();
        if (FindAnyObjectByType<PlayerUnitCycle>() == null)
            gameObject.AddComponent<PlayerUnitCycle>();
        if (FindAnyObjectByType<CrisisManager>() == null)
            gameObject.AddComponent<CrisisManager>();
        if (FindAnyObjectByType<SynodLegacyManager>() == null)
            gameObject.AddComponent<SynodLegacyManager>();
        if (FindAnyObjectByType<IdentityPickerPanel>() == null)
            gameObject.AddComponent<IdentityPickerPanel>();
        if (FindAnyObjectByType<DistrictSpecialtyPickerPanel>() == null)
            gameObject.AddComponent<DistrictSpecialtyPickerPanel>();
        if (FindAnyObjectByType<CityGrowthManager>() == null)
            gameObject.AddComponent<CityGrowthManager>();
        if (FindAnyObjectByType<DistrictOfferPanel>() == null)
            gameObject.AddComponent<DistrictOfferPanel>();
        if (FindAnyObjectByType<AppealOverlayController>() == null)
            gameObject.AddComponent<AppealOverlayController>();
        if (FindAnyObjectByType<LoadingScreenPanel>() == null)
            gameObject.AddComponent<LoadingScreenPanel>();
        if (FindAnyObjectByType<MatchLobbyController>() == null)
            gameObject.AddComponent<MatchLobbyController>();
        if (FindAnyObjectByType<MatchLobbyPanel>() == null)
            gameObject.AddComponent<MatchLobbyPanel>();
        if (FindAnyObjectByType<ArtEraVisualController>() == null)
            gameObject.AddComponent<ArtEraVisualController>();
        if (GetComponent<MapWrapVisuals>() == null)
            gameObject.AddComponent<MapWrapVisuals>();
    }

    public void GenerateMap()
    {
        if (hexPrefabBlueprint == null) return;
        PrepareMapGeneration();

        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int r = 0; r < gridHeightRows; r++)
                CreateTile(q, r);
        }

        ApplyWaterFeatures();
        PlaceMapResources();
        PickFactionSpawnLocations();
        TagNavalCoastTiles();
        TagNavigableWaterTiles();
        FinishMapGeneration();
    }

    public IEnumerator GenerateMapAsync(System.Action<float> onProgress = null)
    {
        if (hexPrefabBlueprint == null)
            yield break;

        onProgress?.Invoke(0.02f);
        yield return null;
        PrepareMapGeneration();

        int total = gridWidthCols * gridHeightRows;
        int count = 0;
        const int batchSize = 80;
        const float tileProgressStart = 0.12f;
        const float tileProgressEnd = 0.64f;

        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int r = 0; r < gridHeightRows; r++)
            {
                CreateTile(q, r);
                count++;
                if (count % batchSize == 0 || count == total)
                {
                    float tileT = total > 0 ? (float)count / total : 1f;
                    float progress = Mathf.Lerp(tileProgressStart, tileProgressEnd, tileT);
                    onProgress?.Invoke(progress);
                    yield return null;
                }
            }
        }

        onProgress?.Invoke(0.72f);
        yield return null;
        ApplyWaterFeatures();

        onProgress?.Invoke(0.82f);
        yield return null;
        PlaceMapResources();

        onProgress?.Invoke(0.88f);
        yield return null;
        PickFactionSpawnLocations();
        TagNavalCoastTiles();
        TagNavigableWaterTiles();

        onProgress?.Invoke(0.94f);
        yield return null;
        FinishMapGeneration(onProgress);
    }

    void PrepareMapGeneration()
    {
        if (mapSeed != 0)
            Random.InitState(mapSeed);
        else
            Random.InitState(System.Environment.TickCount);

        if (hexRadiusSize <= 0f)
            CalibrateHexSizeFromPrefab();
        if (hexRadiusSize <= 0f)
            hexRadiusSize = 0.7f;

        foreach (Transform child in transform)
            Destroy(child.gameObject);
        tiles.Clear();
        InvalidateMovementCostCache();

        var centerHex = new HexCoordinates(gridWidthCols / 2, gridHeightRows / 2);
        mapOriginOffset = centerHex.ToWorldPosition(hexRadiusSize);
    }

    void CreateTile(int q, int r)
    {
        var coords = new HexCoordinates(q, r);
        var terrain = RollLandTerrain(q, r);
        var localPos = coords.ToWorldPosition(hexRadiusSize) - mapOriginOffset;
        var go = Instantiate(hexPrefabBlueprint, transform);
        go.transform.localPosition = localPos;
        go.name = $"Hex_{q}_{r}";
        go.transform.localScale = Vector3.one * tileScale;

        var tile = go.GetComponent<HexTile>();
        if (tile == null)
            tile = go.AddComponent<HexTile>();
        tile.Initialize(coords, terrain);

        EnsureCollider(go);
        tiles[coords] = tile;
    }

    void FinishMapGeneration(System.Action<float> onProgress = null)
    {
        int waterCount = CountTerrain(TerrainRules.IsWater);
        int shoreCount = CountTerrain(t => t == TerrainType.Shore);
        Debug.Log($"Book of Concord: generated {tiles.Count} hex tiles ({gridWidthCols}x{gridHeightRows}, wrap={wrapStyle}). Water={waterCount}, shore={shoreCount}, navalCoast={NavalCoastTileCount}, navigableWater={NavigableWaterTileCount}, deepWater={DeepWaterTileCount}.");
        GetComponent<MapWrapVisuals>()?.Rebuild();
        FogOfWarManager.Instance?.Refresh();
        onProgress?.Invoke(0.96f);
    }

    public bool IsInBounds(HexCoordinates coords) =>
        coords.Q >= 0 && coords.Q < gridWidthCols &&
        coords.R >= 0 && coords.R < gridHeightRows;

    public bool WrapsHorizontally => wrapStyle != MapWrapStyle.Bounded;
    public bool WrapsVertically => wrapStyle == MapWrapStyle.Toroidal;

    public Vector3 WrapPeriodLocal
    {
        get
        {
            var origin = new HexCoordinates(0, 0).ToWorldPosition(hexRadiusSize) - mapOriginOffset;
            var qEdge = new HexCoordinates(gridWidthCols, 0).ToWorldPosition(hexRadiusSize) - mapOriginOffset;
            var rEdge = new HexCoordinates(0, gridHeightRows).ToWorldPosition(hexRadiusSize) - mapOriginOffset;
            return new Vector3(qEdge.x - origin.x, rEdge.y - origin.y, 0f);
        }
    }

    public HexCoordinates Wrap(HexCoordinates coords)
    {
        switch (wrapStyle)
        {
            case MapWrapStyle.Toroidal:
            {
                int q = ((coords.Q % gridWidthCols) + gridWidthCols) % gridWidthCols;
                int r = ((coords.R % gridHeightRows) + gridHeightRows) % gridHeightRows;
                return new HexCoordinates(q, r);
            }
            case MapWrapStyle.Cylindrical:
            {
                int q = ((coords.Q % gridWidthCols) + gridWidthCols) % gridWidthCols;
                int r = Mathf.Clamp(coords.R, 0, gridHeightRows - 1);
                return new HexCoordinates(q, r);
            }
            default:
                return coords;
        }
    }

    bool IsInVerticalBounds(int r) => r >= 0 && r < gridHeightRows;

    public IEnumerable<HexCoordinates> GetWrappedNeighbors(HexCoordinates coords)
    {
        foreach (var neighbor in coords.GetNeighbors())
        {
            switch (wrapStyle)
            {
                case MapWrapStyle.Toroidal:
                    yield return Wrap(neighbor);
                    break;
                case MapWrapStyle.Cylindrical:
                    if (!IsInVerticalBounds(neighbor.R))
                        continue;
                    yield return Wrap(neighbor);
                    break;
                default:
                    if (IsInBounds(neighbor))
                        yield return neighbor;
                    break;
            }
        }
    }

    public bool AreWrappedAdjacent(HexCoordinates a, HexCoordinates b)
    {
        a = Wrap(a);
        b = Wrap(b);
        foreach (var neighbor in GetWrappedNeighbors(a))
        {
            if (neighbor == b)
                return true;
        }
        return false;
    }

    public int WrappedDistance(HexCoordinates from, HexCoordinates to)
    {
        from = Wrap(from);
        to = Wrap(to);
        if (from == to)
            return 0;

        var visited = new HashSet<HexCoordinates> { from };
        var queue = new Queue<(HexCoordinates coords, int distance)>();
        queue.Enqueue((from, 0));

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();
            foreach (var neighbor in GetWrappedNeighbors(current))
            {
                if (neighbor == to)
                    return distance + 1;

                if (visited.Add(neighbor))
                    queue.Enqueue((neighbor, distance + 1));
            }
        }

        return int.MaxValue;
    }

    static TerrainType RollLandTerrain(int q, int r)
    {
        int hash = (q * 73856093) ^ (r * 19349663);
        float n = (hash & 0xFFFF) / 65535f;
        if (n < 0.35f) return TerrainType.Pasture;
        if (n < 0.60f) return TerrainType.Wilderness;
        if (n < 0.85f) return TerrainType.Forest;
        return TerrainType.Hill;
    }

    static float MoistureNoise(int q, int r)
    {
        unchecked
        {
            int h1 = q * 73856093 ^ r * 19349663;
            int h2 = (q + 11) * 83492791 ^ (r + 23) * 132049789;
            float a = (h1 & 0xFFFF) / 65535f;
            float b = (h2 & 0xFFFF) / 65535f;
            return a * 0.65f + b * 0.35f;
        }
    }

    void ApplyWaterFeatures()
    {
        if (wrapStyle != MapWrapStyle.Toroidal)
            CarveMapEdgeSeas();

        var wet = new bool[gridWidthCols, gridHeightRows];
        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int r = 0; r < gridHeightRows; r++)
            {
                float moisture = MoistureNoise(q, r);
                float blob = MoistureNoise(q / 3 + 7, r / 3 + 13);
                float wetThreshold = coastalDensity == CoastalDensity.Archipelago ? 0.48f : 0.54f;
                float blobThreshold = coastalDensity == CoastalDensity.Archipelago ? 0.34f : 0.40f;
                wet[q, r] = moisture > wetThreshold && blob > blobThreshold;
            }
        }

        float minWater = coastalDensity == CoastalDensity.Archipelago ? 0.14f : 0.08f;
        EnsureMinimumWaterCoverage(wet, minWater);
        int riverCount = coastalDensity == CoastalDensity.Archipelago
            ? Mathf.Max(8, gridWidthCols / 3)
            : Mathf.Max(5, gridWidthCols / 4);
        CarveRivers(riverCount);
        ClassifyWaterBodies(wet);
        ApplyShoreTiles();
    }

    void CarveMapEdgeSeas()
    {
        int edgeDepth = coastalDensity == CoastalDensity.Archipelago ? 2 : 1;

        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int d = 0; d < edgeDepth; d++)
            {
                SetTerrainIfExists(new HexCoordinates(q, d), TerrainType.Ocean);
                SetTerrainIfExists(new HexCoordinates(q, gridHeightRows - 1 - d), TerrainType.Ocean);
            }
        }

        if (wrapStyle == MapWrapStyle.Bounded)
        {
            for (int r = 0; r < gridHeightRows; r++)
            {
                for (int d = 0; d < edgeDepth; d++)
                {
                    SetTerrainIfExists(new HexCoordinates(d, r), TerrainType.Ocean);
                    SetTerrainIfExists(new HexCoordinates(gridWidthCols - 1 - d, r), TerrainType.Ocean);
                }
            }
        }
    }

    void SetTerrainIfExists(HexCoordinates coords, TerrainType terrain)
    {
        if (TryGetTile(coords, out var tile))
            tile.SetTerrain(terrain);
    }

    void TagNavalCoastTiles()
    {
        NavalCoastTileCount = 0;

        foreach (var tile in tiles.Values)
            tile.SetNavalCoast(false);

        foreach (var tile in tiles.Values)
        {
            bool isCoast = tile.Terrain == TerrainType.Shore ||
                           IsAdjacentToNavigableWater(tile.Coordinates);
            if (!isCoast)
                continue;

            tile.SetNavalCoast(true);
            NavalCoastTileCount++;
        }
    }

    void TagNavigableWaterTiles()
    {
        NavigableWaterTileCount = 0;
        DeepWaterTileCount = 0;

        foreach (var tile in tiles.Values)
            tile.SetNavigableWater(false);

        TagRiverNavigableWater();
        TagCoastalOceanAndLakeWater(GetCoastalNavigableDepth());

        foreach (var tile in tiles.Values)
        {
            if (TerrainRules.IsWater(tile.Terrain) && !tile.IsNavigableWater)
                DeepWaterTileCount++;
        }
    }

    int GetCoastalNavigableDepth() =>
        coastalDensity == CoastalDensity.Archipelago ? 5 : 3;

    void TagRiverNavigableWater()
    {
        var queue = new Queue<HexCoordinates>();
        foreach (var tile in tiles.Values)
        {
            if (tile.Terrain != TerrainType.River)
                continue;
            if (!IsWaterAdjacentToLand(tile.Coordinates))
                continue;

            tile.SetNavigableWater(true);
            NavigableWaterTileCount++;
            queue.Enqueue(tile.Coordinates);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in GetWrappedNeighbors(current))
            {
                if (!TryGetTile(neighbor, out var nTile))
                    continue;
                if (nTile.Terrain != TerrainType.River || nTile.IsNavigableWater)
                    continue;

                nTile.SetNavigableWater(true);
                NavigableWaterTileCount++;
                queue.Enqueue(neighbor);
            }
        }
    }

    void TagCoastalOceanAndLakeWater(int maxDepth)
    {
        var bestDist = new Dictionary<HexCoordinates, int>();
        var queue = new Queue<HexCoordinates>();

        foreach (var tile in tiles.Values)
        {
            if (tile.Terrain != TerrainType.Ocean && tile.Terrain != TerrainType.Lake)
                continue;
            if (!IsWaterAdjacentToLand(tile.Coordinates))
                continue;

            bestDist[tile.Coordinates] = 1;
            if (!tile.IsNavigableWater)
            {
                tile.SetNavigableWater(true);
                NavigableWaterTileCount++;
            }
            queue.Enqueue(tile.Coordinates);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!bestDist.TryGetValue(current, out int dist) || dist >= maxDepth)
                continue;

            foreach (var neighbor in GetWrappedNeighbors(current))
            {
                if (!TryGetTile(neighbor, out var nTile))
                    continue;
                if (nTile.Terrain != TerrainType.Ocean && nTile.Terrain != TerrainType.Lake)
                    continue;

                int nextDist = dist + 1;
                if (nextDist > maxDepth)
                    continue;

                if (bestDist.TryGetValue(neighbor, out int known) && known <= nextDist)
                    continue;

                bestDist[neighbor] = nextDist;
                if (!nTile.IsNavigableWater)
                {
                    nTile.SetNavigableWater(true);
                    NavigableWaterTileCount++;
                }
                queue.Enqueue(neighbor);
            }
        }
    }

    bool IsWaterAdjacentToLand(HexCoordinates coords)
    {
        foreach (var neighbor in GetWrappedNeighbors(coords))
        {
            if (TryGetTile(neighbor, out var nTile) && TerrainRules.IsPassable(nTile.Terrain))
                return true;
        }

        return false;
    }

    bool IsAdjacentToNavigableWater(HexCoordinates coords)
    {
        foreach (var neighbor in GetWrappedNeighbors(coords))
        {
            if (!TryGetTile(neighbor, out var nTile))
                continue;
            if (nTile.Terrain == TerrainType.Ocean ||
                nTile.Terrain == TerrainType.River ||
                nTile.Terrain == TerrainType.Lake)
                return true;
        }

        return false;
    }

    void EnsureMinimumWaterCoverage(bool[,] wet, float minFraction)
    {
        int target = Mathf.Max(12, Mathf.RoundToInt(gridWidthCols * gridHeightRows * minFraction));
        int current = CountWetCells(wet);
        int attempts = 0;

        while (current < target && attempts < 300)
        {
            attempts++;
            int seedQ = Random.Range(1, gridWidthCols - 1);
            int seedR = Random.Range(1, gridHeightRows - 1);
            int radius = Random.Range(1, 4);

            for (int dq = -radius; dq <= radius; dq++)
            {
                for (int dr = -radius; dr <= radius; dr++)
                {
                    if (Mathf.Abs(dq) + Mathf.Abs(dr) > radius + 1)
                        continue;

                    int nq = seedQ + dq;
                    int nr = seedR + dr;
                    if (nq < 0 || nr < 0 || nq >= gridWidthCols || nr >= gridHeightRows)
                        continue;

                    if (wet[nq, nr])
                        continue;

                    wet[nq, nr] = true;
                    current++;
                    if (current >= target)
                        return;
                }
            }
        }
    }

    static int CountWetCells(bool[,] wet)
    {
        int count = 0;
        for (int q = 0; q < wet.GetLength(0); q++)
        for (int r = 0; r < wet.GetLength(1); r++)
            if (wet[q, r]) count++;
        return count;
    }

    int CountTerrain(System.Func<TerrainType, bool> predicate)
    {
        int count = 0;
        foreach (var tile in tiles.Values)
        {
            if (predicate(tile.Terrain))
                count++;
        }
        return count;
    }

    void CarveRivers(int riverCount = 5)
    {
        for (int i = 0; i < riverCount; i++)
        {
            int q = Random.Range(1, gridWidthCols - 1);
            int r = Random.Range(1, gridHeightRows - 1);
            var coords = Wrap(new HexCoordinates(q, r));
            if (!TryGetTile(coords, out var tile) || !TerrainRules.IsPassable(tile.Terrain))
                continue;

            int length = Random.Range(
                Mathf.Max(8, gridWidthCols / 6),
                Mathf.Max(18, gridWidthCols / 3));
            for (int step = 0; step < length; step++)
            {
                if (!TryGetTile(coords, out tile))
                    break;

                tile.SetTerrain(TerrainType.River);

                var nextChoices = new List<HexCoordinates>();
                foreach (var neighbor in GetWrappedNeighbors(coords))
                {
                    if (!TryGetTile(neighbor, out var nTile))
                        continue;
                    if (TerrainRules.IsPassable(nTile.Terrain) || nTile.Terrain == TerrainType.River)
                        nextChoices.Add(neighbor);
                }

                if (nextChoices.Count == 0)
                    break;

                coords = nextChoices[Random.Range(0, nextChoices.Count)];
            }
        }
    }

    void ClassifyWaterBodies(bool[,] wetSeed)
    {
        var visited = new bool[gridWidthCols, gridHeightRows];

        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int r = 0; r < gridHeightRows; r++)
            {
                if (visited[q, r]) continue;
                var start = new HexCoordinates(q, r);
                if (!TryGetTile(start, out var startTile)) continue;

                bool seedWet = wetSeed[q, r];
                bool alreadyRiver = startTile.Terrain == TerrainType.River;
                if (!seedWet && !alreadyRiver)
                {
                    visited[q, r] = true;
                    continue;
                }

                var component = new List<HexCoordinates>();
                var queue = new Queue<HexCoordinates>();
                queue.Enqueue(start);
                visited[q, r] = true;

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(current);

                    foreach (var neighbor in GetWrappedNeighbors(current))
                    {
                        if (!TryGetTile(neighbor, out var nTile)) continue;
                        if (visited[neighbor.Q, neighbor.R]) continue;

                        bool neighborWet = wetSeed[neighbor.Q, neighbor.R] ||
                                           nTile.Terrain == TerrainType.River;
                        if (!neighborWet) continue;

                        visited[neighbor.Q, neighbor.R] = true;
                        queue.Enqueue(neighbor);
                    }
                }

                TerrainType waterType = component.Count switch
                {
                    >= 12 => TerrainType.Ocean,
                    >= 4 => TerrainType.Lake,
                    _ => TerrainType.River
                };

                foreach (var coords in component)
                {
                    if (TryGetTile(coords, out var tile))
                        tile.SetTerrain(waterType);
                }
            }
        }
    }

    void ApplyShoreTiles()
    {
        var toShore = new List<HexCoordinates>();

        foreach (var tile in tiles.Values)
        {
            if (TerrainRules.IsWater(tile.Terrain)) continue;

            foreach (var neighbor in GetWrappedNeighbors(tile.Coordinates))
            {
                if (!TryGetTile(neighbor, out var nTile)) continue;
                if (!TerrainRules.IsWater(nTile.Terrain)) continue;

                toShore.Add(tile.Coordinates);
                break;
            }
        }

        foreach (var coords in toShore)
        {
            if (TryGetTile(coords, out var tile) && tile.Terrain != TerrainType.Pasture)
                tile.SetTerrain(TerrainType.Shore);
        }
    }

    void PlaceMapResources()
    {
        var resourceTypes = new[]
        {
            MapResourceType.Wheat,
            MapResourceType.Cattle,
            MapResourceType.Grapes,
            MapResourceType.Fish,
            MapResourceType.Timber,
            MapResourceType.Stone,
            MapResourceType.Iron,
            MapResourceType.Coal,
            MapResourceType.Gold
        };

        int placed = 0;
        foreach (var tile in tiles.Values)
        {
            if (!TerrainRules.IsPassable(tile.Terrain))
                continue;

            int hash = (tile.Coordinates.Q * 92837111) ^ (tile.Coordinates.R * 689287499);
            float roll = (hash & 0xFFFF) / 65535f;
            if (roll > 0.045f)
                continue;

            var valid = new List<MapResourceType>();
            foreach (var resource in resourceTypes)
            {
                if (MapResourceDatabase.IsValidOnTerrain(resource, tile.Terrain))
                    valid.Add(resource);
            }

            if (valid.Count == 0)
                continue;

            int pick = Mathf.Abs(hash / 65536) % valid.Count;
            tile.SetResource(valid[pick]);
            placed++;
        }

        Debug.Log($"Map resources placed: {placed}.");
    }

    void PickFactionSpawnLocations()
    {
        int margin = Mathf.Max(3, Mathf.Min(gridWidthCols, gridHeightRows) / 10);
        var candidates = new List<HexCoordinates>();

        for (int q = margin; q < gridWidthCols - margin; q++)
        {
            for (int r = margin; r < gridHeightRows - margin; r++)
            {
                var coords = new HexCoordinates(q, r);
                if (IsValidCapitalSite(coords))
                    candidates.Add(coords);
            }
        }

        Shuffle(candidates);

        HexCoordinates synodCapital = candidates.Count > 0
            ? candidates[0]
            : new HexCoordinates(gridWidthCols / 4, gridHeightRows / 2);

        if (candidates.Count == 0)
            Debug.LogWarning("No ideal spawn sites found; using emergency land pocket.");

        var synodScout = PickUnitHexes(synodCapital)[0];

        PrepareSpawnCluster(synodCapital, new[] { synodScout });

        SpawnLayout = new FactionSpawnLayout(synodCapital, synodScout);

        Debug.Log($"Spawn layout: nomadic synod {synodCapital}+{synodScout} (schismatics emerge later from dissent).");
    }

    public bool TryPickSchismSite(HexCoordinates near, out HexCoordinates capital, out HexCoordinates soldierHex, out HexCoordinates missionaryHex)
    {
        capital = default;
        soldierHex = default;
        missionaryHex = default;

        var candidates = new List<HexCoordinates>();
        const int minDistance = 6;
        int maxDistance = Mathf.Max(14, Mathf.Max(gridWidthCols, gridHeightRows) / 2);

        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int r = 0; r < gridHeightRows; r++)
            {
                var coords = new HexCoordinates(q, r);
                int dist = WrappedDistance(coords, near);
                if (dist < minDistance || dist > maxDistance)
                    continue;
                if (!IsValidCapitalSite(coords))
                    continue;
                candidates.Add(coords);
            }
        }

        Shuffle(candidates);

        foreach (var site in candidates)
        {
            if (TryAssignSchismSite(site, out capital, out soldierHex, out missionaryHex))
                return true;
        }

        var fallback = new List<HexCoordinates>();
        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int r = 0; r < gridHeightRows; r++)
            {
                var coords = new HexCoordinates(q, r);
                if (WrappedDistance(coords, near) < minDistance)
                    continue;
                if (!IsValidCapitalSite(coords))
                    continue;
                fallback.Add(coords);
            }
        }

        Shuffle(fallback);
        foreach (var site in fallback)
        {
            if (TryAssignSchismSite(site, out capital, out soldierHex, out missionaryHex))
            {
                Debug.LogWarning($"Schism site fallback used at {site} (no ideal pocket in {minDistance}-{maxDistance} hex range).");
                return true;
            }
        }

        return false;
    }

    /// <summary>Picks a rival capital away from the synod and other existing capitals.</summary>
    public bool TryPickRivalSpawnSite(
        HexCoordinates synodAnchor,
        IReadOnlyList<HexCoordinates> avoidCapitals,
        out HexCoordinates capital,
        out HexCoordinates soldierHex,
        out HexCoordinates missionaryHex)
    {
        capital = default;
        soldierHex = default;
        missionaryHex = default;

        const int minFromSynod = 8;
        const int minFromRival = 6;
        int maxDistance = Mathf.Max(16, Mathf.Max(gridWidthCols, gridHeightRows) / 2);

        var candidates = new List<HexCoordinates>();
        for (int q = 0; q < gridWidthCols; q++)
        {
            for (int r = 0; r < gridHeightRows; r++)
            {
                var coords = new HexCoordinates(q, r);
                int distFromSynod = WrappedDistance(coords, synodAnchor);
                if (distFromSynod < minFromSynod || distFromSynod > maxDistance)
                    continue;

                bool tooClose = false;
                if (avoidCapitals != null)
                {
                    foreach (var avoid in avoidCapitals)
                    {
                        if (WrappedDistance(coords, avoid) < minFromRival)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }

                if (tooClose || !IsValidCapitalSite(coords))
                    continue;

                candidates.Add(coords);
            }
        }

        Shuffle(candidates);
        foreach (var site in candidates)
        {
            if (TryAssignSchismSite(site, out capital, out soldierHex, out missionaryHex))
                return true;
        }

        return false;
    }

    bool TryAssignSchismSite(HexCoordinates site, out HexCoordinates capital, out HexCoordinates soldierHex, out HexCoordinates missionaryHex)
    {
        capital = site;
        var unitHexes = PickUnitHexes(site);
        PrepareSpawnCluster(site, unitHexes);
        soldierHex = unitHexes[0];
        missionaryHex = unitHexes[1];
        return true;
    }

    bool IsValidCapitalSite(HexCoordinates coords)
    {
        if (!TryGetTile(coords, out var tile))
            return false;
        if (!TerrainRules.IsPassable(tile.Terrain))
            return false;

        if (GetPassableNeighborHexes(coords).Count < 3)
            return false;

        if (CountAdjacentWater(coords) > 2)
            return false;

        if (CountPassableWithin(coords, 5) < Mathf.Max(14, gridWidthCols * gridHeightRows / 180))
            return false;

        return true;
    }

    int CountAdjacentWater(HexCoordinates coords)
    {
        int count = 0;
        foreach (var neighbor in GetWrappedNeighbors(coords))
        {
            if (TryGetTile(neighbor, out var tile) && TerrainRules.IsWater(tile.Terrain))
                count++;
        }
        return count;
    }

    int CountPassableWithin(HexCoordinates origin, int maxDistance)
    {
        origin = Wrap(origin);
        var visited = new HashSet<HexCoordinates> { origin };
        var queue = new Queue<(HexCoordinates coords, int distance)>();
        queue.Enqueue((origin, 0));
        int passable = 0;

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();
            if (!TryGetTile(current, out var tile))
                continue;
            if (TerrainRules.IsPassable(tile.Terrain))
                passable++;

            if (distance >= maxDistance)
                continue;

            foreach (var neighbor in GetWrappedNeighbors(current))
            {
                if (visited.Add(neighbor))
                    queue.Enqueue((neighbor, distance + 1));
            }
        }

        return passable;
    }

    void PrepareSpawnCluster(HexCoordinates capital, HexCoordinates[] unitHexes)
    {
        ClearSpawnHex(capital);
        foreach (var hex in unitHexes)
            ClearSpawnHex(hex);

        EnsureLandPocket(capital, pocketRadius: 3, minPassable: 16);

        foreach (var hex in unitHexes)
        {
            if (!TryGetTile(hex, out var tile) || !TerrainRules.IsPassable(tile.Terrain))
                ClearSpawnHex(hex);
        }
    }

    void EnsureLandPocket(HexCoordinates center, int pocketRadius, int minPassable)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (CountPassableWithin(center, pocketRadius + attempt) >= minPassable)
                return;

            foreach (var tile in AllTiles)
            {
                if (WrappedDistance(center, tile.Coordinates) > pocketRadius + attempt)
                    continue;
                if (TerrainRules.IsWater(tile.Terrain))
                    tile.SetTerrain(TerrainType.Pasture);
            }
        }
    }

    List<HexCoordinates> GetPassableNeighborHexes(HexCoordinates coords)
    {
        var neighbors = new List<HexCoordinates>();
        foreach (var neighbor in GetWrappedNeighbors(coords))
        {
            if (!TryGetTile(neighbor, out var tile))
                continue;
            if (TerrainRules.IsPassable(tile.Terrain))
                neighbors.Add(neighbor);
        }
        return neighbors;
    }

    HexCoordinates[] PickUnitHexes(HexCoordinates capital)
    {
        var neighbors = GetPassableNeighborHexes(capital);
        Shuffle(neighbors);

        if (neighbors.Count < 2)
            return new[] { capital, capital };

        return new[] { neighbors[0], neighbors[1] };
    }

    void ClearSpawnHex(HexCoordinates coords)
    {
        if (!TryGetTile(coords, out var tile))
            return;
        tile.SetTerrain(TerrainType.Pasture);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    static void EnsureCollider(GameObject go)
    {
        if (go.GetComponent<Collider2D>() != null) return;
        var col = go.AddComponent<PolygonCollider2D>();
        col.isTrigger = false;
    }

    public bool TryGetTile(HexCoordinates coords, out HexTile tile)
    {
        if (wrapStyle == MapWrapStyle.Bounded)
        {
            if (!IsInBounds(coords))
            {
                tile = null;
                return false;
            }
        }
        else
        {
            coords = Wrap(coords);
            if (wrapStyle == MapWrapStyle.Cylindrical && !IsInVerticalBounds(coords.R))
            {
                tile = null;
                return false;
            }
        }

        return tiles.TryGetValue(coords, out tile);
    }

    public HexCoordinates WorldToHex(Vector3 world)
    {
        var local = transform.InverseTransformPoint(world) + mapOriginOffset;
        return Wrap(HexCoordinates.FromWorldPosition(local, hexRadiusSize));
    }

    public Vector3 HexToWorld(HexCoordinates coords) =>
        transform.TransformPoint(coords.ToWorldPosition(hexRadiusSize) - mapOriginOffset);

    public IEnumerable<HexTile> AllTiles => tiles.Values;

    public void RefreshResourceVisibility()
    {
        foreach (var tile in AllTiles)
            tile.SetFogVisibility(tile.FogVisibility);
    }

    public List<HexCoordinates> GetReachableHexes(HexCoordinates origin, int range, FactionId faction) =>
        GetReachableHexes(origin, range, faction, UnitType.Soldier);

    public List<HexCoordinates> GetReachableHexes(HexCoordinates origin, int range, FactionId faction, UnitType unitType)
    {
        var costs = GetMovementCosts(origin, range, faction, unitType);
        var result = new List<HexCoordinates>();
        foreach (var pair in costs)
        {
            if (pair.Key != origin)
                result.Add(pair.Key);
        }
        return result;
    }

    public bool TryGetMovementCost(
        HexCoordinates origin,
        HexCoordinates target,
        int range,
        FactionId faction,
        out int cost) =>
        TryGetMovementCost(origin, target, range, faction, UnitType.Soldier, out cost);

    public bool TryGetMovementCost(
        HexCoordinates origin,
        HexCoordinates target,
        int range,
        FactionId faction,
        UnitType unitType,
        out int cost)
    {
        cost = 0;
        origin = Wrap(origin);
        target = Wrap(target);
        if (origin == target) return true;
        if (!TryGetTile(target, out var targetTile) || targetTile.Occupant != null)
            return false;
        if (!NavalMovementRules.CanEnterTile(unitType, targetTile))
            return false;
        return GetMovementCosts(origin, range, faction, unitType).TryGetValue(target, out cost);
    }

    public Dictionary<HexCoordinates, int> GetMovementCosts(HexCoordinates origin, int range, FactionId faction) =>
        GetMovementCosts(origin, range, faction, UnitType.Soldier);

    public Dictionary<HexCoordinates, int> GetMovementCosts(
        HexCoordinates origin,
        int range,
        FactionId faction,
        UnitType unitType)
    {
        origin = Wrap(origin);
        if (movementCostCache != null &&
            origin == movementCostOrigin &&
            range == movementCostRange &&
            faction == movementCostFaction &&
            unitType == movementCostUnitType)
        {
            return movementCostCache;
        }

        movementCostOrigin = origin;
        movementCostRange = range;
        movementCostFaction = faction;
        movementCostUnitType = unitType;
        movementCostCache = ComputeMovementCosts(origin, range, faction, unitType);
        return movementCostCache;
    }

    public void InvalidateMovementCostCache() => movementCostCache = null;

    /// <summary>Dijkstra movement costs; occupied hexes block movement.</summary>
    public Dictionary<HexCoordinates, int> ComputeMovementCosts(HexCoordinates origin, int range, FactionId faction) =>
        ComputeMovementCosts(origin, range, faction, UnitType.Soldier);

    public Dictionary<HexCoordinates, int> ComputeMovementCosts(
        HexCoordinates origin,
        int range,
        FactionId faction,
        UnitType unitType)
    {
        origin = Wrap(origin);
        var best = new Dictionary<HexCoordinates, int> { [origin] = 0 };
        var queue = new Queue<HexCoordinates>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentCost = best[current];

            foreach (var neighbor in GetWrappedNeighbors(current))
            {
                if (!TryGetTile(neighbor, out var tile)) continue;
                if (!NavalMovementRules.CanEnterTile(unitType, tile)) continue;
                if (tile.Occupant != null) continue;

                int nextCost = currentCost + NavalMovementRules.StepCost(unitType, tile);
                if (nextCost > range) continue;

                if (best.TryGetValue(neighbor, out int known) && known <= nextCost)
                    continue;

                best[neighbor] = nextCost;
                queue.Enqueue(neighbor);
            }
        }

        return best;
    }

    public bool TryFindMovementPath(
        HexCoordinates origin,
        HexCoordinates target,
        int range,
        FactionId faction,
        out List<HexCoordinates> path) =>
        TryFindMovementPath(origin, target, range, faction, UnitType.Soldier, out path);

    public bool TryFindMovementPath(
        HexCoordinates origin,
        HexCoordinates target,
        int range,
        FactionId faction,
        UnitType unitType,
        out List<HexCoordinates> path)
    {
        path = new List<HexCoordinates>();
        origin = Wrap(origin);
        target = Wrap(target);

        if (origin == target)
        {
            path.Add(origin);
            return true;
        }

        if (!TryGetTile(target, out var targetTile) || targetTile.Occupant != null)
            return false;
        if (!NavalMovementRules.CanEnterTile(unitType, targetTile))
            return false;

        var best = new Dictionary<HexCoordinates, int> { [origin] = 0 };
        var parent = new Dictionary<HexCoordinates, HexCoordinates>();
        var queue = new Queue<HexCoordinates>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentCost = best[current];

            foreach (var neighbor in GetWrappedNeighbors(current))
            {
                if (!TryGetTile(neighbor, out var tile))
                    continue;
                if (!NavalMovementRules.CanEnterTile(unitType, tile))
                    continue;
                if (tile.Occupant != null)
                    continue;

                int nextCost = currentCost + NavalMovementRules.StepCost(unitType, tile);
                if (nextCost > range)
                    continue;

                if (best.TryGetValue(neighbor, out int known) && known <= nextCost)
                    continue;

                best[neighbor] = nextCost;
                parent[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        if (!best.ContainsKey(target))
            return false;

        var step = target;
        while (true)
        {
            path.Add(step);
            if (step == origin)
                break;
            step = parent[step];
        }

        path.Reverse();
        return true;
    }

    public bool TryFindMovementPath(
        HexCoordinates origin,
        HexCoordinates target,
        FactionId faction,
        out List<HexCoordinates> path) =>
        TryFindMovementPath(origin, target, int.MaxValue / 4, faction, out path);

    public bool TryFindMovementPath(
        HexCoordinates origin,
        HexCoordinates target,
        FactionId faction,
        UnitType unitType,
        out List<HexCoordinates> path) =>
        TryFindMovementPath(origin, target, int.MaxValue / 4, faction, unitType, out path);

    public bool TryTruncatePathToMovementBudget(
        IReadOnlyList<HexCoordinates> path,
        int maxCost,
        out List<HexCoordinates> segment,
        out int totalCost) =>
        TryTruncatePathToMovementBudget(path, maxCost, UnitType.Soldier, out segment, out totalCost);

    public bool TryTruncatePathToMovementBudget(
        IReadOnlyList<HexCoordinates> path,
        int maxCost,
        UnitType unitType,
        out List<HexCoordinates> segment,
        out int totalCost)
    {
        segment = new List<HexCoordinates>();
        totalCost = 0;
        if (path == null || path.Count == 0)
            return false;

        segment.Add(path[0]);
        for (int i = 1; i < path.Count; i++)
        {
            if (!TryGetTile(path[i], out var tile))
                return false;

            int stepCost = NavalMovementRules.StepCost(unitType, tile);
            if (totalCost + stepCost > maxCost)
                break;

            totalCost += stepCost;
            segment.Add(path[i]);
        }

        return segment.Count > 1;
    }

    public static int TerrainMovePenalty(TerrainType terrain)
    {
        if (!TerrainRules.IsPassable(terrain))
            return int.MaxValue / 4;

        int penalty = terrain switch
        {
            TerrainType.Forest => 1,
            TerrainType.Hill => 1,
            TerrainType.Shore => 0,
            _ => 0
        };

        int reduction = 0;
        if (ConfessionResearchManager.Instance != null)
            reduction = ConfessionResearchManager.Instance.GetEffectiveModifiers().TerrainMovePenaltyReduction;

        return Mathf.Max(0, penalty - reduction);
    }

    public string TerrainLabelAt(HexCoordinates coords)
    {
        if (!TryGetTile(coords, out var tile)) return "OUT_OF_BOUNDS";
        return GameplayTerrainCategory(tile.Terrain);
    }

    public static string TerrainDisplayName(TerrainType terrain) => terrain switch
    {
        TerrainType.Pasture => "Pasture",
        TerrainType.Forest => "Forest",
        TerrainType.Hill => "Hill",
        TerrainType.Ocean => "Ocean",
        TerrainType.Shore => "Shore",
        TerrainType.Lake => "Lake",
        TerrainType.River => "River",
        _ => "Wilderness"
    };

    public static string GameplayTerrainCategory(TerrainType terrain) => terrain switch
    {
        TerrainType.Pasture => "Settlement",
        TerrainType.Shore => "Coast",
        TerrainType.Ocean or TerrainType.Lake or TerrainType.River => "Water",
        _ => "Wilderness"
    };

    public static string TerrainEndTurnSummary(TerrainType terrain) => terrain switch
    {
        TerrainType.Pasture => "Stable adherence (no base manuscripts)",
        TerrainType.Shore => "Coastal trade: stable adherence, +1 manuscript",
        TerrainType.Forest => "+1 manuscript, extra adherence drift",
        TerrainType.Hill => "+1 manuscript, extra adherence drift",
        TerrainType.Ocean => "Impassable open sea",
        TerrainType.Lake => "Impassable inland water",
        TerrainType.River => "Impassable  -  no ford",
        _ => "+1 manuscript, extra adherence drift"
    };

    public static string TerrainMoveSummary(TerrainType terrain, bool isNavigableWater = false)
    {
        if (isNavigableWater)
            return "Navigable (naval units)";

        if (TerrainRules.IsWater(terrain))
            return "Deep water (impassable)";

        if (!TerrainRules.IsPassable(terrain))
            return "Impassable";

        int penalty = terrain switch
        {
            TerrainType.Forest => 1,
            TerrainType.Hill => 1,
            _ => 0
        };
        return penalty > 0 ? $"+{penalty} move cost" : "Normal movement";
    }

    public bool TryGetTerrainInfo(HexCoordinates coords, out TerrainTileInfo info)
    {
        if (!TryGetTile(coords, out var tile))
        {
            info = default;
            return false;
        }

        info = new TerrainTileInfo(
            tile.Terrain,
            tile.Resource,
            tile.Settlement != null ? tile.Settlement.CityName : null,
            tile.Settlement != null ? tile.Settlement.Faction : FactionId.None,
            TerritoryManager.Instance?.GetOwner(coords),
            TerritoryManager.Instance != null &&
            TerritoryManager.Instance.GetOwner(coords) != null &&
            tile.IsWorked,
            tile.IsNavalCoast,
            tile.IsNavigableWater);
        return true;
    }

    public string GetTerrainTypeAtPosition(Vector3 playerPos) =>
        TerrainLabelAt(WorldToHex(playerPos));
}

public readonly struct TerrainTileInfo
{
    public readonly TerrainType Type;
    public readonly MapResourceType Resource;
    public readonly string CityName;
    public readonly FactionId CityFaction;
    public readonly City TerritoryOwner;
    public readonly bool IsWorked;
    public readonly bool IsNavalCoast;
    public readonly bool IsNavigableWater;

    public TerrainTileInfo(
        TerrainType type,
        MapResourceType resource,
        string cityName,
        FactionId cityFaction,
        City territoryOwner,
        bool isWorked,
        bool isNavalCoast,
        bool isNavigableWater)
    {
        Type = type;
        Resource = resource;
        CityName = cityName;
        CityFaction = cityFaction;
        TerritoryOwner = territoryOwner;
        IsWorked = isWorked;
        IsNavalCoast = isNavalCoast;
        IsNavigableWater = isNavigableWater;
    }

    public string DisplayName => HexGridMap.TerrainDisplayName(Type);
    public string Category => HexGridMap.GameplayTerrainCategory(Type);
    public string EndTurnSummary => HexGridMap.TerrainEndTurnSummary(Type);
    public string MoveSummary => HexGridMap.TerrainMoveSummary(Type, IsNavigableWater);
    public bool HasCity => !string.IsNullOrEmpty(CityName);
    public TileYield Yield => TileYieldDatabase.GetVisibleTileYield(Type, Resource);
    public TileYield VisibleYield => Yield;

    public string FormatHoverLine()
    {
        var line = $"<b>{DisplayName}</b>  ({Category})  |  Yields: {Yield.FormatForHover(IsWorked)}  |  {MoveSummary}";

        int missionaryMss = MapResourceDatabase.MissionaryManuscriptBonus(Type);
        if (missionaryMss > 0)
            line += $"  |  Missionary: <color=#EECC66>+{missionaryMss} mss/turn</color>";

        if (Resource != MapResourceType.None)
        {
            if (MapResourceDatabase.IsRevealedToPlayer(Resource))
            {
                line += $"  |  <color=#EECC66>{MapResourceDatabase.DisplayName(Resource)}</color>";
                var mssNote = MapResourceDatabase.ManuscriptNote(Resource);
                if (!string.IsNullOrEmpty(mssNote))
                    line += $" (<color=#EECC66>{mssNote}</color>)";
            }
            else
            {
                line += "  |  <color=#AA9988>Unknown resource</color>";
                var hint = MapResourceDatabase.RevealHint(Resource);
                if (!string.IsNullOrEmpty(hint))
                    line += $"  (<size=11>{hint}</size>)";
            }
        }
        else if (Yield.Manuscripts > 0 && !IsWorked)
        {
            line += $"  |  <color=#EECC66>+{Yield.Manuscripts} mss/turn when worked</color>";
        }

        if (TerritoryOwner != null)
            line += $"  |  {TerritoryOwner.CityName} lands";
        if (IsWorked && Yield.Manuscripts > 0)
            line += "  |  <color=#EECC66>manuscript tile</color>";
        else if (IsWorked)
            line += "  |  <color=#DDDD88>worked</color>";
        if (HasCity)
            line += $"  |  City: {CityName}";
        if (IsNavigableWater)
            line += "  |  <color=#88CCFF>Navigable water</color>";
        else if (TerrainRules.IsWater(Type))
            line += "  |  <color=#6688AA>Deep water (impassable)</color>";
        else if (IsNavalCoast)
            line += "  |  <color=#88CCFF>Naval coast</color>";
        return line;
    }

    public string FormatMissionaryLine()
    {
        var line = $"<b>Missionary tile:</b> {DisplayName} ({Category})  -  {Yield.FormatCompact()}";
        int missionaryMss = MapResourceDatabase.MissionaryManuscriptBonus(Type);
        if (missionaryMss > 0)
            line += $"  |  <color=#EECC66>+{missionaryMss} mss end-turn</color>";
        line += $"  |  {EndTurnSummary}";
        if (Resource != MapResourceType.None)
        {
            if (MapResourceDatabase.IsRevealedToPlayer(Resource))
            {
                line += $"  [{MapResourceDatabase.DisplayName(Resource)}]";
                var mssNote = MapResourceDatabase.ManuscriptNote(Resource);
                if (!string.IsNullOrEmpty(mssNote))
                    line += $" (<color=#EECC66>{mssNote}</color>)";
            }
            else
                line += "  [Unknown resource]";
        }
        if (HasCity)
            line += $"  [{CityName}]";
        return line;
    }
}
