using UnityEngine;

public class City : MonoBehaviour
{
    public FactionId Faction { get; private set; }
    public SynodPlayerId SynodPlayer { get; private set; } = SynodPlayerId.Player1;
    public SchismaticBlocId SchismaticBloc { get; private set; } = SchismaticBlocId.None;
    public HexCoordinates HexPosition { get; private set; }
    public string CityName { get; private set; }
    public bool IsCapital { get; private set; }
    public bool IsHamlet { get; private set; }
    public City ParentCity { get; private set; }
    /// <summary>Independent city that owns this settlement's lands (self if not a district).</summary>
    public City ControllingCity => ParentCity != null ? ParentCity : this;
    public bool IsIndependentCity => !IsHamlet;
    public HamletSpecialty Specialty { get; private set; } = HamletSpecialty.None;
    public bool HasChosenSpecialty => !IsHamlet || Specialty != HamletSpecialty.None;
    public int Population { get; set; } = 20;
    public int FoundedOnTurn { get; private set; } = 1;
    public float Loyalty { get; private set; } = 85f;
    public float CulturePoints { get; private set; } = 10f;
    public CityProduction Production { get; private set; }

    public enum CitySizeTier
    {
        Small,
        Medium,
        Large,
        Capital
    }

    public const int MediumPopulation = 15;
    public const int LargePopulation = 30;

    public CitySizeTier SizeTier
    {
        get
        {
            if (IsCapital)
                return CitySizeTier.Capital;
            if (Population >= LargePopulation)
                return CitySizeTier.Large;
            if (Population >= MediumPopulation)
                return CitySizeTier.Medium;
            return CitySizeTier.Small;
        }
    }

    SpriteRenderer spriteRenderer;
    BoxCollider2D clickCollider;

    public void Initialize(
        FactionId faction,
        HexCoordinates hex,
        string name,
        bool isCapital = false,
        int startingPopulation = 20,
        City parentCity = null,
        SynodPlayerId synodPlayer = SynodPlayerId.Player1)
    {
        Faction = faction;
        SynodPlayer = synodPlayer;
        SchismaticBloc = SchismaticBlocId.None;
        CityName = name;
        IsCapital = isCapital;
        ParentCity = parentCity;
        if (parentCity != null)
            IsHamlet = true;
        else
            IsHamlet = !isCapital && startingPopulation <= 10;
        Population = startingPopulation;
        CulturePoints = isCapital ? 12f : IsHamlet ? 4f : 10f;
        FoundedOnTurn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;

        if (HexGridMap.Instance != null)
            hex = HexGridMap.Instance.Wrap(hex);
        HexPosition = hex;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sortingOrder = 5;
        transform.position = HexGridMap.Instance.HexToWorld(hex);

        clickCollider = GetComponent<BoxCollider2D>();
        if (clickCollider == null)
            clickCollider = gameObject.AddComponent<BoxCollider2D>();
        clickCollider.size = Vector2.one * 0.9f;

        Production = GetComponent<CityProduction>();
        if (Production == null)
            Production = gameObject.AddComponent<CityProduction>();
        Production.Bind(this);

        if (HexGridMap.Instance.TryGetTile(hex, out var tile))
            tile.SetSettlement(this);

        CityManager.Instance?.Register(this);
        Loyalty = CityLoyaltySystem.GetStartingLoyalty(this);
        RefreshAppearance();
    }

    public void AdjustLoyalty(float delta) =>
        Loyalty = Mathf.Clamp(Loyalty + delta, 0f, 100f);

    public void ResetLoyaltyForOwner() =>
        Loyalty = CityLoyaltySystem.GetStartingLoyalty(this);

    public string LoyaltySummaryLabel()
    {
        if (Faction == FactionId.LutheranSynod)
            return CityLoyaltySystem.FormatLoyaltyBar(Loyalty);
        return CityLoyaltySystem.FormatLoyaltyLine(this);
    }

    public void RefreshAppearance()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = ArtEraSpriteFactory.StyleSprite(
            GetCityMaskSprite(SizeTier),
            Unit.FactionColor(Faction, SynodPlayer),
            ArtEraVisualController.CurrentEra,
            $"city_{SizeTier}",
            hostileOutline: Faction == FactionId.Schismatic);
        spriteRenderer.color = Color.white;
        transform.localScale = Vector3.one * SizeTier switch
        {
            CitySizeTier.Capital => 0.72f,
            CitySizeTier.Large => 0.66f,
            CitySizeTier.Medium => 0.60f,
            _ => 0.54f
        };
    }

    public static string SizeTierLabel(CitySizeTier tier) => tier switch
    {
        CitySizeTier.Capital => "Capital",
        CitySizeTier.Large => "Large city",
        CitySizeTier.Medium => "Town",
        _ => "Town"
    };

    public string SettlementDisplayName()
    {
        if (IsHamlet && ParentCity != null)
        {
            string spec = HasChosenSpecialty ? HamletSpecialtyDatabase.DisplayName(Specialty) : "Unspecialized";
            return $"{ParentCity.CityName}  -  {CityName} ({spec})";
        }
        return CityName;
    }

    public string SettlementKindLabel() => IsHamlet ? "District" : SizeTierLabel(SizeTier);

    public void SetSpecialty(HamletSpecialty specialty)
    {
        if (!IsHamlet || Specialty != HamletSpecialty.None || specialty == HamletSpecialty.None)
            return;
        Specialty = specialty;
        RefreshAppearance();
    }

    public void AddCulturePoints(float amount) => CulturePoints += amount;

    void OnDestroy() => CityManager.Instance?.Unregister(this);

    public int GetProductionPerTurn() => GetProductionBreakdown().Total;

    public CityYieldBreakdown GetProductionBreakdown()
    {
        int fromPop = Mathf.Max(1, Population / 5);
        int fromFood = 0;
        int fromProduction = 0;
        int fromManuscripts = 0;
        int fromBuildings = 0;
        string terrainLabel = "tiles";
        var tileParts = new System.Collections.Generic.List<string>();

        if (TerritoryManager.Instance != null)
        {
            var workedYield = TerritoryManager.Instance.GetWorkedYieldTotal(this);
            fromFood = workedYield.Food;
            fromProduction = workedYield.Production;
            fromManuscripts = workedYield.Manuscripts;

            int workedCount = TerritoryManager.Instance.GetWorkedTiles(this).Count;
            int territoryCount = TerritoryManager.Instance.GetTerritoryTileCount(this);
            terrainLabel = $"{workedCount}/{territoryCount} worked";

            if (fromProduction > 0)
                tileParts.Add($"{fromProduction} prod");
            if (fromFood > 0)
                tileParts.Add($"{fromFood} food");
            if (fromManuscripts > 0)
                tileParts.Add($"{fromManuscripts} mss");
        }
        else if (HexGridMap.Instance != null &&
                 HexGridMap.Instance.TryGetTile(HexPosition, out var tile))
        {
            var centerYield = TileYieldDatabase.GetTileYield(tile);
            fromFood = centerYield.Food;
            fromProduction = centerYield.Production;
            fromManuscripts = centerYield.Manuscripts;
            terrainLabel = HexGridMap.TerrainDisplayName(tile.Terrain).ToLowerInvariant();
            tileParts.Add(centerYield.FormatCompact());
        }

        var buildingParts = new System.Collections.Generic.List<string>();
        if (Production != null)
        {
            if (Production.HasBuilding(CityBuildId.BuildGuildWorkshop))
            {
                fromBuildings += 2;
                buildingParts.Add("workshop +2");
            }
            if (Production.HasBuilding(CityBuildId.BuildObservatory))
            {
                fromBuildings += 3;
                buildingParts.Add("observatory +3");
            }
            if (Production.HasBuilding(CityBuildId.BuildPrintingPress))
            {
                fromBuildings += 1;
                buildingParts.Add("press +1");
            }
            if (Production.HasBuilding(CityBuildId.BuildLibrary))
            {
                fromBuildings += 2;
                buildingParts.Add("library +2");
            }
            if (Production.HasBuilding(CityBuildId.BuildPotteryWorkshop))
            {
                fromBuildings += 1;
                buildingParts.Add("pottery +1");
            }
            if (Production.HasBuilding(CityBuildId.BuildUniversity))
            {
                fromBuildings += 3;
                buildingParts.Add("university +3");
            }
            if (Production.HasBuilding(CityBuildId.BuildBarracks))
            {
                fromBuildings += 1;
                buildingParts.Add("barracks +1");
            }
            if (Production.HasBuilding(CityBuildId.BuildArmory))
            {
                fromBuildings += 1;
                buildingParts.Add("armory +1");
            }
            if (Production.HasBuilding(CityBuildId.BuildMarketHall))
            {
                fromBuildings += 2;
                buildingParts.Add("market +2");
            }
            if (Production.HasBuilding(CityBuildId.BuildMill))
            {
                fromBuildings += 1;
                buildingParts.Add("mill +1");
            }
        }

        return new CityYieldBreakdown
        {
            FromPopulation = fromPop,
            FromFood = fromFood,
            FromProduction = fromProduction,
            FromManuscripts = fromManuscripts,
            FromBuildings = fromBuildings,
            TerrainLabel = terrainLabel,
            TileDetail = tileParts.Count > 0 ? string.Join(", ", tileParts) : null,
            BuildingDetail = buildingParts.Count > 0 ? string.Join(", ", buildingParts) : null
        };
    }

    public string ProductionYieldLabel() => $"{GetProductionPerTurn()} prod/turn";

    public string ProductionBreakdownLabel()
    {
        var b = GetProductionBreakdown();
        var sb = new System.Text.StringBuilder();
        if (IsHamlet)
            sb.Append(CityName).Append(": <b>hamlet</b>  -  ").Append(b.Total).Append(" tribute/turn");
        else
            sb.Append(CityName).Append(": <b>").Append(b.Total).Append(" prod/turn</b>");
        sb.Append(" (").Append(b.FromPopulation).Append(" pop");
        if (b.FromProduction > 0 || b.FromFood > 0 || b.FromManuscripts > 0)
        {
            sb.Append(" + ").Append(b.TerrainLabel);
            if (!string.IsNullOrEmpty(b.TileDetail))
                sb.Append(" [").Append(b.TileDetail).Append(']');
        }
        if (b.FromBuildings > 0)
            sb.Append(" + ").Append(b.BuildingDetail);
        if (b.FromManuscripts > 0)
            sb.Append("  |  <color=#EECC66>Mss tiles: +").Append(b.FromManuscripts).Append("/turn</color>");
        sb.Append(')');
        return sb.ToString();
    }

    public string ManuscriptTilesLabel()
    {
        if (TerritoryManager.Instance == null)
            return null;

        string active = TerritoryManager.Instance.FormatManuscriptWorkedTiles(this);
        if (!string.IsNullOrEmpty(active))
            return $"<color=#EECC66>Manuscript tiles (worked): {active}</color>";

        string latent = TerritoryManager.Instance.FormatManuscriptTilesInTerritory(this);
        return !string.IsNullOrEmpty(latent)
            ? $"<color=#CCAA55>Manuscript tiles in borders: {latent}</color>"
            : null;
    }

    public string CultureSummaryLabel()
    {
        if (IsHamlet && ParentCity != null)
            return $"District of {ParentCity.CityName}  |  culture flows to parent (+35%)";
        return $"Culture: {CulturePoints:F0}  |  borders max {CityManager.MaxTerritoryRadius} hexes out";
    }

    public void AdvanceCulture(float synodAdherence)
    {
        float gain = Population / 30f;

        if (Faction == FactionId.LutheranSynod)
            gain += synodAdherence / 25f;
        else if (Faction == FactionId.Schismatic)
            gain += 0.75f;

        if (Production != null)
        {
            if (Production.HasBuilding(CityBuildId.BuildChapel)) gain += 1.2f;
            if (Production.HasBuilding(CityBuildId.BuildCathedral)) gain += 2.5f;
            if (Production.HasBuilding(CityBuildId.BuildParishSchool)) gain += 0.6f;
            if (Production.HasBuilding(CityBuildId.BuildLibrary)) gain += 0.4f;
        }

        if (IsHamlet && ParentCity != null)
        {
            ParentCity.AddCulturePoints(gain * 0.35f);
            return;
        }

        if (TerritoryManager.Instance != null)
        {
            foreach (var hex in TerritoryManager.Instance.GetWorkedTiles(this))
            {
                if (HexGridMap.Instance != null &&
                    HexGridMap.Instance.TryGetTile(hex, out var tile) &&
                    TileYieldDatabase.GetTileYield(tile).Manuscripts > 0)
                {
                    gain += 0.5f;
                }
            }
        }

        CulturePoints += gain;
    }

    public string TerritorySummaryLabel()
    {
        if (IsHamlet && ParentCity != null)
            return $"District hex  -  lands owned by {ParentCity.CityName}  |  tribute district";

        if (TerritoryManager.Instance == null)
            return "Territory:  - ";

        int tiles = TerritoryManager.Instance.GetTerritoryTileCount(this);
        int cap = TerritoryManager.Instance.GetTerritoryCap(this);
        int worked = TerritoryManager.Instance.GetWorkedTiles(this).Count;
        int workCap = TerritoryManager.Instance.GetWorkedTileCap(this);
        var yield = TerritoryManager.Instance.GetWorkedYieldTotal(this);
        var sb = new System.Text.StringBuilder();
        sb.Append($"Territory: {tiles}/{cap} tiles (culture {CulturePoints:F0})  |  {worked}/{workCap} worked  |  {yield.FormatCompact()}");
        string growth = GrowthSummaryLabel();
        if (!string.IsNullOrEmpty(growth))
            sb.Append("\n").Append(growth);
        string mssTiles = TerritoryManager.Instance.FormatManuscriptWorkedTiles(this);
        if (!string.IsNullOrEmpty(mssTiles))
            sb.Append("  |  <color=#EECC66>Mss: ").Append(mssTiles).Append("</color>");
        else
        {
            string latent = TerritoryManager.Instance.FormatManuscriptTilesInTerritory(this);
            if (!string.IsNullOrEmpty(latent))
                sb.Append("  |  <color=#CCAA55>Unworked mss: ").Append(latent).Append("</color>");
        }
        return sb.ToString();
    }

    public string GrowthSummaryLabel() => CityGrowthSystem.FormatGrowthLine(this);

    public void Capture(FactionId newOwner, SynodPlayerId synodPlayer = SynodPlayerId.Player1)
    {
        if (Faction == newOwner &&
            (newOwner != FactionId.LutheranSynod || SynodPlayer == synodPlayer))
            return;

        var previousFaction = Faction;
        var previousBloc = SchismaticBloc;
        bool wasSchismaticCapital = previousFaction == FactionId.Schismatic && IsCapital;

        Faction = newOwner;
        if (newOwner == FactionId.LutheranSynod)
        {
            SchismaticBloc = SchismaticBlocId.None;
            SynodPlayer = synodPlayer;
        }
        ResetLoyaltyForOwner();
        RefreshAppearance();
        Production?.OnCityCaptured();
        TerritoryManager.Instance?.RefreshAll();
        Debug.Log($"{CityName} captured by {newOwner}.");

        if (wasSchismaticCapital && newOwner == FactionId.LutheranSynod && previousBloc != SchismaticBlocId.None)
        {
            SchismaticBlocRegistry.Instance?.UnregisterBloc(previousBloc);
            TurnPhaseBanner.Instance?.Refresh(
                $"<color=#88EEAA><b>{CityName}</b></color> reclaimed — a dissenting capital falls; a schism slot opens.");
            UnionStrifeManager.AddStrife(-20);
        }

        if (previousFaction == FactionId.LutheranSynod &&
            SynodPlayer == SynodPlayerId.Player1 &&
            newOwner == FactionId.Schismatic)
            UnionStrifeManager.NotifyPlayerCityLostToSchism();

        MatchController.Instance?.EvaluateConditions();
        FirstSteps.Instance?.RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        FogOfWarManager.Instance?.Refresh();
    }

    public void SetFogHidden(bool hidden)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = !hidden;
    }

    public void SetSchismaticBloc(SchismaticBlocId blocId) => SchismaticBloc = blocId;

    public void SetSynodPlayer(SynodPlayerId playerId)
    {
        SynodPlayer = playerId;
        RefreshAppearance();
    }

    public string FormatOwnerLabel()
    {
        if (Faction == FactionId.Schismatic)
        {
            if (SchismaticBloc != SchismaticBlocId.None && SchismaticBlocRegistry.Instance != null)
                return SchismaticBlocRegistry.Instance.ProfileForBloc(SchismaticBloc).DisplayName;
            return "Schismatic dissent";
        }

        if (Faction == FactionId.LutheranSynod)
            return SynodPlayerDatabase.DisplayName(SynodPlayer);

        return Faction.ToString();
    }

    static Sprite diamondSprite;
    static Sprite houseSprite;
    static Sprite circleSprite;
    static Sprite starSprite;

    static Sprite GetCityMaskSprite(CitySizeTier tier) => tier switch
    {
        CitySizeTier.Capital => CreateStarSprite(),
        CitySizeTier.Large => CreateCircleSprite(),
        CitySizeTier.Medium => CreateHouseSprite(),
        _ => CreateDiamondSprite()
    };

    static Sprite CreateDiamondSprite()
    {
        if (diamondSprite != null) return diamondSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                tex.SetPixel(x, y, dx + dy <= radius ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        diamondSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return diamondSprite;
    }

    static Sprite CreateHouseSprite()
    {
        if (houseSprite != null) return houseSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        const float centerX = 16f;
        const int roofTop = 4;
        const int roofBase = 13;
        const int baseLeft = 9;
        const int baseRight = 22;
        const int baseBottom = 26;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inBase = x >= baseLeft && x <= baseRight && y >= roofBase && y <= baseBottom;
                bool inRoof = false;
                if (y >= roofTop && y <= roofBase)
                {
                    float halfWidth = (y - roofTop) / (float)(roofBase - roofTop) * 11f;
                    inRoof = Mathf.Abs(x - centerX) <= halfWidth;
                }

                tex.SetPixel(x, y, inBase || inRoof ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        houseSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return houseSprite;
    }

    static Sprite CreateCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    static Sprite CreateStarSprite()
    {
        if (starSprite != null) return starSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var center = new Vector2(size / 2f, size / 2f);
        const float outerRadius = 13f;
        const float innerRadius = 5.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float angle = Mathf.Atan2(dy, dx);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float sector = (angle + Mathf.PI * 0.5f) / (Mathf.PI * 2f / 5f);
                sector -= Mathf.Floor(sector);
                float allowedRadius = sector < 0.5f
                    ? Mathf.Lerp(outerRadius, innerRadius, sector * 2f)
                    : Mathf.Lerp(innerRadius, outerRadius, (sector - 0.5f) * 2f);
                tex.SetPixel(x, y, dist <= allowedRadius + 0.6f ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return starSprite;
    }
}

public struct CityYieldBreakdown
{
    public int FromPopulation;
    public int FromFood;
    public int FromProduction;
    public int FromManuscripts;
    public int FromBuildings;
    public string TerrainLabel;
    public string TileDetail;
    public string BuildingDetail;
    public int Total => FromPopulation + FromProduction + FromBuildings;
}
