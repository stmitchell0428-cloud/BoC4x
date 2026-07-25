using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders toroidal/cylindrical wrap copies of edge tiles and units so opposite map edges
/// are visible without scrolling across the whole world.
/// </summary>
public class MapWrapVisuals : MonoBehaviour
{
    public static MapWrapVisuals Instance { get; private set; }

    const int MinEdgeMargin = 8;
    const int MaxEdgeMargin = 16;

    Transform wrapRoot;
    readonly Dictionary<HexTile, List<TileClone>> tileClones = new();
    readonly Dictionary<Transform, List<EntityClone>> entityClones = new();

    struct TileClone
    {
        public SpriteRenderer Terrain;
        public SpriteRenderer Resource;
        public SpriteRenderer Worked;
    }

    struct EntityClone
    {
        public Transform Root;
        public SpriteRenderer Sprite;
    }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LateUpdate() => SyncEntityWraps();

    public void Rebuild()
    {
        Clear();
        var map = HexGridMap.Instance;
        if (map == null || map.wrapStyle == MapWrapStyle.Bounded)
            return;

        EnsureWrapRoot(map.transform);
        int margin = ComputeEdgeMargin(map);
        Vector3 period = map.WrapPeriodLocal;

        foreach (var tile in map.AllTiles)
        {
            var offsets = CollectOffsets(tile.Coordinates, margin, period, map);
            if (offsets.Count == 0)
                continue;

            var clones = new List<TileClone>(offsets.Count);
            foreach (var offset in offsets)
                clones.Add(CreateTileClone(tile, offset));
            tileClones[tile] = clones;
            SyncTile(tile);
        }
    }

    public void SyncTile(HexTile tile)
    {
        if (tile == null || !tileClones.TryGetValue(tile, out var clones))
            return;

        var srcTerrain = tile.GetComponent<SpriteRenderer>();
        var srcResource = FindChildSprite(tile.transform, "ResourceMarker");
        var srcWorked = FindChildSprite(tile.transform, "WorkedRing");

        foreach (var clone in clones)
        {
            if (srcTerrain != null && clone.Terrain != null)
            {
                clone.Terrain.sprite = srcTerrain.sprite;
                clone.Terrain.color = srcTerrain.color;
                clone.Terrain.sortingOrder = srcTerrain.sortingOrder;
            }

            SyncOptionalSprite(clone.Resource, srcResource);
            SyncOptionalSprite(clone.Worked, srcWorked);
        }
    }

    void SyncEntityWraps()
    {
        var map = HexGridMap.Instance;
        if (map == null || map.wrapStyle == MapWrapStyle.Bounded)
            return;

        var live = new HashSet<Transform>();
        int margin = ComputeEdgeMargin(map);
        Vector3 period = map.WrapPeriodLocal;

        foreach (var unit in FindObjectsByType<Unit>())
        {
            if (unit == null || !unit.IsAlive)
                continue;
            SyncEntityWrap(unit.transform, unit.GetComponent<SpriteRenderer>(), unit.HexPosition, margin, period, map, live);
        }

        foreach (var city in FindObjectsByType<City>())
        {
            if (city == null)
                continue;
            SyncEntityWrap(city.transform, city.GetComponent<SpriteRenderer>(), city.HexPosition, margin, period, map, live);
        }

        RemoveStaleEntityClones(live);
    }

    void SyncEntityWrap(
        Transform source,
        SpriteRenderer srcSprite,
        HexCoordinates hex,
        int margin,
        Vector3 period,
        HexGridMap map,
        HashSet<Transform> live)
    {
        if (source == null || srcSprite == null)
            return;

        live.Add(source);
        var offsets = CollectOffsets(hex, margin, period, map);
        if (offsets.Count == 0)
        {
            RemoveEntityClones(source);
            return;
        }

        if (!entityClones.TryGetValue(source, out var clones))
        {
            clones = new List<EntityClone>(offsets.Count);
            entityClones[source] = clones;
        }

        while (clones.Count < offsets.Count)
        {
            var root = new GameObject($"WrapEntity_{source.name}_{clones.Count}");
            root.transform.SetParent(wrapRoot, false);
            var sr = root.AddComponent<SpriteRenderer>();
            clones.Add(new EntityClone { Root = root.transform, Sprite = sr });
        }

        for (int i = clones.Count - 1; i >= offsets.Count; i--)
        {
            if (clones[i].Root != null)
                Destroy(clones[i].Root.gameObject);
            clones.RemoveAt(i);
        }

        for (int i = 0; i < offsets.Count; i++)
        {
            var clone = clones[i];
            clone.Root.localPosition = map.transform.InverseTransformPoint(source.position) + offsets[i];
            clone.Root.localRotation = source.localRotation;
            clone.Root.localScale = source.lossyScale;
            clone.Sprite.sprite = srcSprite.sprite;
            clone.Sprite.color = srcSprite.color;
            clone.Sprite.sortingOrder = srcSprite.sortingOrder + 1;
            clone.Sprite.enabled = srcSprite.enabled;
        }
    }

    static void SyncOptionalSprite(SpriteRenderer clone, SpriteRenderer source)
    {
        if (clone == null)
            return;

        if (source == null || !source.enabled)
        {
            clone.enabled = false;
            return;
        }

        clone.enabled = true;
        clone.sprite = source.sprite;
        clone.color = source.color;
        clone.sortingOrder = source.sortingOrder;
    }

    static SpriteRenderer FindChildSprite(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        return child != null ? child.GetComponent<SpriteRenderer>() : null;
    }

    TileClone CreateTileClone(HexTile source, Vector3 localOffset)
    {
        var go = new GameObject($"Wrap_{source.Coordinates.Q}_{source.Coordinates.R}_{localOffset}");
        go.transform.SetParent(wrapRoot, false);
        go.transform.localPosition = source.transform.localPosition + localOffset;
        go.transform.localScale = source.transform.localScale;

        var srcTerrain = source.GetComponent<SpriteRenderer>();
        var terrain = go.AddComponent<SpriteRenderer>();
        terrain.sortingOrder = srcTerrain != null ? srcTerrain.sortingOrder : 0;

        SpriteRenderer resource = null;
        var srcResource = FindChildSprite(source.transform, "ResourceMarker");
        if (srcResource != null)
        {
            var resourceGo = new GameObject("ResourceMarker");
            resourceGo.transform.SetParent(go.transform, false);
            resourceGo.transform.localPosition = srcResource.transform.localPosition;
            resourceGo.transform.localScale = srcResource.transform.localScale;
            resource = resourceGo.AddComponent<SpriteRenderer>();
            resource.sortingOrder = srcResource.sortingOrder;
        }

        SpriteRenderer worked = null;
        var srcWorked = FindChildSprite(source.transform, "WorkedRing");
        if (srcWorked != null)
        {
            var workedGo = new GameObject("WorkedRing");
            workedGo.transform.SetParent(go.transform, false);
            workedGo.transform.localPosition = srcWorked.transform.localPosition;
            workedGo.transform.localScale = srcWorked.transform.localScale;
            worked = workedGo.AddComponent<SpriteRenderer>();
            worked.sortingOrder = srcWorked.sortingOrder;
        }

        return new TileClone { Terrain = terrain, Resource = resource, Worked = worked };
    }

    static List<Vector3> CollectOffsets(HexCoordinates coords, int margin, Vector3 period, HexGridMap map)
    {
        var offsets = new List<Vector3>(4);
        int qSide = 0;
        if (coords.Q < margin)
            qSide = 1;
        else if (coords.Q >= map.gridWidthCols - margin)
            qSide = -1;

        int rSide = 0;
        if (map.WrapsVertically)
        {
            if (coords.R < margin)
                rSide = 1;
            else if (coords.R >= map.gridHeightRows - margin)
                rSide = -1;
        }

        if (qSide == 0 && rSide == 0)
            return offsets;

        int[] qFactors = qSide != 0 ? new[] { 0, qSide } : new[] { 0 };
        int[] rFactors = rSide != 0 ? new[] { 0, rSide } : new[] { 0 };

        foreach (int qFactor in qFactors)
        {
            foreach (int rFactor in rFactors)
            {
                if (qFactor == 0 && rFactor == 0)
                    continue;
                if (qFactor != 0 && !map.WrapsHorizontally)
                    continue;
                if (rFactor != 0 && !map.WrapsVertically)
                    continue;

                offsets.Add(new Vector3(qFactor * period.x, rFactor * period.y, 0f));
            }
        }

        return offsets;
    }

    static int ComputeEdgeMargin(HexGridMap map)
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return 12;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        int marginQ = Mathf.CeilToInt(halfW / (map.HexSize * 1.5f)) + 3;
        int marginR = Mathf.CeilToInt(halfH / (map.HexSize * Mathf.Sqrt(3f))) + 3;
        return Mathf.Clamp(Mathf.Max(marginQ, marginR), MinEdgeMargin, MaxEdgeMargin);
    }

    void EnsureWrapRoot(Transform parent)
    {
        if (wrapRoot != null)
            return;

        var go = new GameObject("MapWrapVisuals");
        go.transform.SetParent(parent, false);
        wrapRoot = go.transform;
    }

    void RemoveEntityClones(Transform source)
    {
        if (!entityClones.TryGetValue(source, out var clones))
            return;

        foreach (var clone in clones)
        {
            if (clone.Root != null)
                Destroy(clone.Root.gameObject);
        }

        entityClones.Remove(source);
    }

    void RemoveStaleEntityClones(HashSet<Transform> live)
    {
        var stale = new List<Transform>();
        foreach (var pair in entityClones)
        {
            if (!live.Contains(pair.Key))
                stale.Add(pair.Key);
        }

        foreach (var source in stale)
            RemoveEntityClones(source);
    }

    void Clear()
    {
        tileClones.Clear();
        entityClones.Clear();

        if (wrapRoot != null)
        {
            Destroy(wrapRoot.gameObject);
            wrapRoot = null;
        }
    }
}
