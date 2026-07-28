using UnityEngine;

public class HexTile : MonoBehaviour
{
    public HexCoordinates Coordinates { get; private set; }
    public TerrainType Terrain { get; private set; }
    public MapResourceType Resource { get; private set; } = MapResourceType.None;
    public Unit Occupant { get; private set; }
    public City Settlement { get; private set; }
    public City TerritoryOwner { get; private set; }
    public bool IsWorked { get; private set; }
    /// <summary>Coast or river bank flagged for naval units.</summary>
    public bool IsNavalCoast { get; private set; }
    /// <summary>Ocean, lake, or river connected to land — passable for naval units.</summary>
    public bool IsNavigableWater { get; private set; }
    public FogVisibility FogVisibility { get; private set; } = FogVisibility.Unexplored;

    SpriteRenderer spriteRenderer;
    SpriteRenderer resourceMarker;
    SpriteRenderer workedRing;
    Color baseColor;
    HighlightKind currentHighlight = HighlightKind.None;

    static readonly Color HighlightMove = new(0.45f, 0.75f, 1f, 1f);
    static readonly Color HighlightMovePath = new(0.35f, 0.55f, 0.82f, 0.75f);
    static readonly Color HighlightAttack = new(1f, 0.45f, 0.45f, 1f);
    static readonly Color HighlightSelected = new(0.95f, 0.9f, 0.35f, 1f);
    static readonly Color HighlightPlacementExcellent = new(0.45f, 0.95f, 0.55f, 1f);
    static readonly Color HighlightPlacementGood = new(0.55f, 0.85f, 0.65f, 0.85f);
    static readonly Color HighlightAppealExcellent = new(0.92f, 0.82f, 0.35f, 1f);
    static readonly Color HighlightAppealGood = new(0.62f, 0.52f, 0.88f, 0.85f);
    static readonly Color UnexploredFog = new(0.06f, 0.06f, 0.09f, 1f);

    public void Initialize(HexCoordinates coords, TerrainType terrain)
    {
        Coordinates = coords;
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetTerrain(terrain);
    }

    public void SetTerrain(TerrainType terrain)
    {
        Terrain = terrain;
        baseColor = ArtEraPalette.TerrainColor(terrain, ArtEraVisualController.CurrentEra);
        ApplyColor();
    }

    public void RefreshArtEraVisuals()
    {
        baseColor = ArtEraPalette.TerrainColor(Terrain, ArtEraVisualController.CurrentEra);
        ApplyColor();
    }

    public void SetNavalCoast(bool isNavalCoast) => IsNavalCoast = isNavalCoast;

    public void SetNavigableWater(bool isNavigableWater) => IsNavigableWater = isNavigableWater;

    public void SetResource(MapResourceType resource)
    {
        Resource = resource;
        EnsureResourceMarker();
        UpdateResourceMarkerVisibility();
        ApplyColor();
    }

    public void SetOccupant(Unit unit) => Occupant = unit;

    public void SetSettlement(City city) => Settlement = city;

    public void SetTerritoryVisual(City owner, bool worked)
    {
        TerritoryOwner = owner;
        IsWorked = worked;
        EnsureWorkedRing();
        if (workedRing != null)
            workedRing.enabled = worked && FogVisibility == FogVisibility.Visible;
        ApplyColor();
    }

    public void SetFogVisibility(FogVisibility visibility)
    {
        if (FogVisibility == visibility)
        {
            UpdateResourceMarkerVisibility();
            return;
        }

        FogVisibility = visibility;
        UpdateResourceMarkerVisibility();
        if (workedRing != null)
            workedRing.enabled = IsWorked && visibility == FogVisibility.Visible;
        ApplyColor();
    }

    public void ClearHighlight()
    {
        currentHighlight = HighlightKind.None;
        ApplyColor();
    }

    public void SetHighlight(HighlightKind kind)
    {
        if (FogVisibility == FogVisibility.Unexplored)
            return;

        currentHighlight = kind;
        ApplyColor();
    }

    public TileYield GetYield() =>
        TileYieldDatabase.GetTileYield(Terrain, MapResourceDatabase.VisibleResource(Resource));

    void ApplyColor()
    {
        if (spriteRenderer == null) return;

        Color display = baseColor;

        if (TerritoryOwner != null && FogVisibility != FogVisibility.Unexplored)
        {
            var factionColor = Unit.FactionColor(TerritoryOwner.Faction);
            display = display * 0.82f + factionColor * 0.18f;
        }

        if (IsWorked && FogVisibility == FogVisibility.Visible)
            display = display * 0.88f + Color.white * 0.12f;

        if (ShouldDrawHighlight(FogVisibility, currentHighlight))
        {
            Color highlight = currentHighlight switch
            {
                HighlightKind.Move => HighlightMove,
                HighlightKind.MovePath => HighlightMovePath,
                HighlightKind.Attack => HighlightAttack,
                HighlightKind.Selected => HighlightSelected,
                HighlightKind.PlacementExcellent => HighlightPlacementExcellent,
                HighlightKind.PlacementGood => HighlightPlacementGood,
                HighlightKind.AppealExcellent => HighlightAppealExcellent,
                HighlightKind.AppealGood => HighlightAppealGood,
                _ => display
            };

            if (FogVisibility == FogVisibility.Explored)
            {
                Color explored = TerrainRules.IsWater(Terrain)
                    ? display * 0.82f + Color.black * 0.18f
                    : display * 0.62f + Color.black * 0.38f;
                spriteRenderer.color = Color.Lerp(explored, highlight, 0.82f);
            }
            else
                spriteRenderer.color = highlight;
        }
        else
        {
            spriteRenderer.color = FogVisibility switch
            {
                FogVisibility.Unexplored => UnexploredFog,
                FogVisibility.Explored => TerrainRules.IsWater(Terrain)
                    ? display * 0.82f + Color.black * 0.18f
                    : display * 0.62f + Color.black * 0.38f,
                _ => display
            };
        }

        MapWrapVisuals.Instance?.SyncTile(this);
    }

    void UpdateResourceMarkerVisibility()
    {
        if (resourceMarker == null)
            return;

        resourceMarker.enabled = Resource != MapResourceType.None &&
                                 FogVisibility != FogVisibility.Unexplored &&
                                 MapResourceDatabase.IsRevealedToPlayer(Resource);
    }

    void EnsureResourceMarker()
    {
        if (Resource == MapResourceType.None)
            return;

        if (resourceMarker == null)
        {
            var go = new GameObject("ResourceMarker");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.22f, 0.22f, -0.1f);
            go.transform.localScale = Vector3.one * 0.28f;
            resourceMarker = go.AddComponent<SpriteRenderer>();
            resourceMarker.sortingOrder = 3;
        }

        resourceMarker.sprite = CreateResourceSprite();
        resourceMarker.color = ResourceMarkerColor(Resource);
    }

    void EnsureWorkedRing()
    {
        if (workedRing != null)
            return;

        var go = new GameObject("WorkedRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * 1.08f;
        workedRing = go.AddComponent<SpriteRenderer>();
        workedRing.sprite = CreateRingSprite();
        workedRing.color = new Color(0.95f, 0.92f, 0.55f, 0.55f);
        workedRing.sortingOrder = 1;
    }

    static Color ResourceMarkerColor(MapResourceType resource) => resource switch
    {
        MapResourceType.Wheat => new Color(0.92f, 0.82f, 0.35f),
        MapResourceType.Cattle => new Color(0.78f, 0.58f, 0.38f),
        MapResourceType.Grapes => new Color(0.62f, 0.28f, 0.58f),
        MapResourceType.Fish => new Color(0.35f, 0.72f, 0.92f),
        MapResourceType.Timber => new Color(0.45f, 0.32f, 0.18f),
        MapResourceType.Stone => new Color(0.62f, 0.62f, 0.66f),
        MapResourceType.Iron => new Color(0.55f, 0.58f, 0.62f),
        MapResourceType.Coal => new Color(0.22f, 0.22f, 0.24f),
        MapResourceType.Gold => new Color(0.95f, 0.82f, 0.22f),
        _ => Color.white
    };

    static Sprite resourceDotSprite;

    static Sprite CreateResourceSprite()
    {
        if (resourceDotSprite != null) return resourceDotSprite;

        const int size = 16;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= 6.5f ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        resourceDotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return resourceDotSprite;
    }

    static Sprite ringSprite;

    static Sprite CreateRingSprite()
    {
        if (ringSprite != null) return ringSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        float outer = size / 2f - 1f;
        float inner = outer - 2.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= outer && dist >= inner ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }

    static Color TerrainColor(TerrainType terrain) =>
        ArtEraPalette.TerrainColor(terrain, VisualArtEra.WoodcutPaper);

    static bool IsAppealHighlight(HighlightKind kind) =>
        kind is HighlightKind.AppealExcellent or HighlightKind.AppealGood;

    static bool IsPlacementHighlight(HighlightKind kind) =>
        kind is HighlightKind.PlacementExcellent or HighlightKind.PlacementGood;

    static bool ShouldDrawHighlight(FogVisibility visibility, HighlightKind kind) =>
        kind != HighlightKind.None &&
        (visibility == FogVisibility.Visible ||
         IsAppealHighlight(kind) ||
         IsPlacementHighlight(kind));
}

public enum HighlightKind
{
    None,
    Move,
    MovePath,
    Attack,
    Selected,
    PlacementExcellent,
    PlacementGood,
    AppealExcellent,
    AppealGood
}
