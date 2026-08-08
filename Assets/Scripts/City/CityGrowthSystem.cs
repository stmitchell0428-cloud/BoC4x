using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>Two Kingdoms city growth: food balance, secular/spiritual appeal, migration, workers.</summary>
public static class CityGrowthSystem
{
    public const int FoodConsumptionPerPop = 1;
    public const int UrbanFoodBaseline = 6;
    public const int CapitalUrbanFoodBaseline = 10;
    public const int FoundingCapitalPopulation = 15;
    public const int CapitalDeficitGraceTurns = 8;
    public const float MigrationAppealThreshold = 18f;
    public const int MinSurplusStreakForDistrict = 2;
    public const int MinTurnsBeforeDistrictOffer = 8;
    public const int MaxMigrationPerCityPerTurn = 4;
    public const int WorkerPopulationDivisor = 3;
    public const int WorkersPerActiveProject = 1;
    public const int MaxDistrictsPerCity = 6;

    public struct GrowthSnapshot
    {
        public int FoodProduced;
        public int FoodConsumed;
        public int FoodSurplus;
        public float SecularAppeal;
        public float SpiritualAppeal;
        public float BlendedAppeal;
        public int HousingCap;
        public int HousingRoom;
        public int TotalWorkers;
        public int BusyWorkers;
        public int AvailableWorkers;
        public string TensionLabel;
        public float MigrationMultiplier;
    }

    public struct DistrictSiteOffer
    {
        public City Parent;
        public HexCoordinates Hex;
        public HamletSpecialty SuggestedSpecialty;
        public float SiteScore;
        public string FlavorReason;
    }

    public struct FactionGrowthMetrics
    {
        public float Adherence;
        public float SpiritualComfort;
        public float CivicRestraint;
        public bool UseWaltherTension;
    }

    public static FactionGrowthMetrics GetFactionMetrics(City city)
    {
        if (city != null && city.Faction == FactionId.LutheranSynod && city.SynodPlayer != SynodPlayerId.Player1)
        {
            return AiSynodCrisisManager.GetMetrics(city.SynodPlayer);
        }

        if (city != null && city.Faction == FactionId.LutheranSynod && FirstSteps.Instance != null)
        {
            return new FactionGrowthMetrics
            {
                Adherence = FirstSteps.Instance.confessionalAdherence,
                SpiritualComfort = FirstSteps.Instance.spiritualComfort,
                CivicRestraint = FirstSteps.Instance.civicRestraint,
                UseWaltherTension = true
            };
        }

        if (city != null && city.Faction == FactionId.Schismatic &&
            city.SchismaticBloc != SchismaticBlocId.None &&
            SchismaticBlocRegistry.Instance != null)
        {
            return SchismaticBlocRegistry.Instance.GetGrowthMetrics(city.SchismaticBloc);
        }

        return new FactionGrowthMetrics
        {
            Adherence = 38f,
            SpiritualComfort = 42f,
            CivicRestraint = 62f,
            UseWaltherTension = true
        };
    }

    public static GrowthSnapshot Evaluate(City city)
    {
        var snap = new GrowthSnapshot
        {
            FoodProduced = GetFoodProduction(city),
            FoodConsumed = GetFoodConsumption(city),
            HousingCap = GetHousingCap(city),
            TotalWorkers = GetTotalWorkers(city),
            BusyWorkers = GetBusyWorkers(city)
        };
        snap.FoodSurplus = snap.FoodProduced - snap.FoodConsumed;
        int clusterPop = city.IsHamlet ? city.Population : GetControllingPopulation(city);
        snap.HousingRoom = Mathf.Max(0, snap.HousingCap - clusterPop);
        snap.AvailableWorkers = Mathf.Max(0, snap.TotalWorkers - snap.BusyWorkers);
        snap.SecularAppeal = ComputeSecularAppeal(city, snap.FoodSurplus);
        snap.SpiritualAppeal = ComputeSpiritualAppeal(city);
        snap.BlendedAppeal = ComputeBlendedAppeal(snap.SecularAppeal, snap.SpiritualAppeal);
        ApplyWaltherTension(ref snap, city);
        return snap;
    }

    public static int ProcessMigration(City city, GrowthSnapshot snap)
    {
        if (city == null || city.IsHamlet)
            return 0;
        if (city.Faction != FactionId.LutheranSynod && city.Faction != FactionId.Schismatic)
            return 0;

        if (snap.FoodSurplus <= 0 || snap.HousingRoom <= 0 || snap.BlendedAppeal < MigrationAppealThreshold)
            return 0;

        int fromSurplus = Mathf.Max(1, snap.FoodSurplus / 2);
        int fromAppeal = Mathf.Max(1, Mathf.FloorToInt(snap.BlendedAppeal / 18f));
        int gain = Mathf.Min(fromSurplus, fromAppeal, snap.HousingRoom, MaxMigrationPerCityPerTurn);
        gain = Mathf.Max(1, Mathf.RoundToInt(gain * snap.MigrationMultiplier));

        if (gain <= 0)
            return 0;

        city.Population += gain;
        city.RefreshAppearance();

        if (city.Faction == FactionId.LutheranSynod && city.SynodPlayer == SynodPlayerId.Player1)
            ApplyAntinomianGrowthTax(gain);

        return gain;
    }

    static void ApplyAntinomianGrowthTax(int gain)
    {
        var faction = FirstSteps.Instance;
        if (faction == null || gain <= 0)
            return;

        var mods = ConfessionResearchManager.Instance?.GetEffectiveModifiers() ?? default;
        if (faction.spiritualComfort > 60f && faction.confessionalAdherence < 65f && !mods.AntinomianGuard)
            faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence - gain * 0.4f, 0f, 100f);
    }

    public static int GetFoodProduction(City city)
    {
        if (city == null)
            return 0;

        var root = city.IsHamlet ? city.ControllingCity : city.ControllingCity;
        if (root == null)
            root = city;

        int food = 0;
        if (TerritoryManager.Instance != null)
            food += TerritoryManager.Instance.GetWorkedYieldTotal(root).Food;

        food += GetBuildingFoodBonus(root);

        foreach (var hamlet in GetChildDistricts(root))
            food += GetBuildingFoodBonus(hamlet);

        if (!city.IsHamlet && city.IsIndependentCity)
            food += GetUrbanFoodBaseline(city);

        return food;
    }

    static int GetUrbanFoodBaseline(City city)
    {
        if (city == null || city.IsHamlet || !city.IsIndependentCity)
            return 0;
        return city.IsCapital ? CapitalUrbanFoodBaseline : UrbanFoodBaseline;
    }

    /// <summary>Estimate turn-1 food at founding (worked-tile priority, no buildings).</summary>
    public static void ProjectCapitalFoundingFood(
        HexCoordinates hex,
        out int foodProduced,
        out int foodConsumed,
        out int foodSurplus)
    {
        foodConsumed = FoundingCapitalPopulation;
        foodProduced = CapitalUrbanFoodBaseline;
        foodSurplus = foodProduced - foodConsumed;
        int workerCap = Mathf.Max(1, FoundingCapitalPopulation / WorkerPopulationDivisor);

        if (HexGridMap.Instance == null)
            return;

        hex = HexGridMap.Instance.Wrap(hex);
        var candidates = new List<(int food, int prod, int mss, bool isCenter)>();

        var visited = new HashSet<HexCoordinates>();
        var queue = new Queue<(HexCoordinates coords, int dist)>();
        queue.Enqueue((hex, 0));

        while (queue.Count > 0)
        {
            var (coords, dist) = queue.Dequeue();
            if (!visited.Add(coords))
                continue;
            if (dist > CityManager.MaxTerritoryRadius)
                continue;
            if (!HexGridMap.Instance.TryGetTile(coords, out var tile))
                continue;
            if (!TerrainRules.IsPassable(tile.Terrain))
                continue;

            var yield = TileYieldDatabase.GetVisibleTileYield(tile);
            candidates.Add((yield.Food, yield.Production, yield.Manuscripts, coords == hex));

            if (dist >= CityManager.MaxTerritoryRadius)
                continue;
            foreach (var n in HexGridMap.Instance.GetWrappedNeighbors(coords))
                queue.Enqueue((n, dist + 1));
        }

        foreach (var entry in candidates
                     .OrderByDescending(c => c.food)
                     .ThenByDescending(c => c.prod)
                     .ThenByDescending(c => c.mss)
                     .ThenByDescending(c => c.isCenter)
                     .Take(workerCap))
            foodProduced += entry.food;

        foodSurplus = foodProduced - foodConsumed;
    }

    static int GetBuildingFoodBonus(City city)
    {
        var production = city?.Production;
        if (production == null)
            return 0;

        int food = 0;
        if (production.HasBuilding(CityBuildId.BuildGranary)) food += 5;
        if (production.HasBuilding(CityBuildId.BuildMill)) food += 1;
        if (production.HasBuilding(CityBuildId.BuildMarketHall)) food += 1;
        if (production.HasBuilding(CityBuildId.BuildWharf) &&
            CityManager.Instance != null &&
            CityManager.Instance.CityTouchesNavalCoast(city))
            food += 1;
        if (production.HasBuilding(CityBuildId.BuildFishingPost) &&
            CityManager.Instance != null &&
            CityManager.Instance.CityTouchesNavalCoast(city))
            food += 2;

        int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;
        if (production.HasBuilding(CityBuildId.BuildWharf) &&
            CityManager.Instance != null &&
            CityManager.Instance.CityTouchesNavalCoast(city) &&
            ChurchYearCalendar.IsBugenhagenCommemorationWindow(turn))
            food += 1;

        return food;
    }

    public static int GetDistrictLocalFoodContribution(City district)
    {
        if (district == null || !district.IsHamlet)
            return 0;

        int food = GetBuildingFoodBonus(district);
        var parent = district.ParentCity;
        if (parent == null || HexGridMap.Instance == null || TerritoryManager.Instance == null)
            return food;

        var hex = HexGridMap.Instance.Wrap(district.HexPosition);
        if (TerritoryManager.Instance.IsWorkedBy(hex, parent) &&
            HexGridMap.Instance.TryGetTile(hex, out var center))
        {
            food += TileYieldDatabase.GetTileYield(center).Food;
        }

        foreach (var neighbor in HexGridMap.Instance.GetWrappedNeighbors(hex))
        {
            if (!TerritoryManager.Instance.IsWorkedBy(neighbor, parent))
                continue;
            if (HexGridMap.Instance.TryGetTile(neighbor, out var tile))
                food += TileYieldDatabase.GetTileYield(tile).Food;
        }

        return food;
    }

    public static int GetDistrictLocalFoodConsumption(City district) =>
        district == null ? 0 : Mathf.Max(1, district.Population * FoodConsumptionPerPop);

    public static int GetFoodConsumption(City city)
    {
        if (city == null)
            return 0;

        int pop = city.IsHamlet ? city.Population : GetControllingPopulation(city);
        return Mathf.Max(1, pop * FoodConsumptionPerPop);
    }

    public static int GetControllingPopulation(City city)
    {
        if (city == null)
            return 0;
        var root = city.ControllingCity;
        int total = root.Population;
        foreach (var d in GetChildDistricts(root))
            total += d.Population;
        return total;
    }

    public static int GetHousingCap(City city)
    {
        if (city == null)
            return 0;

        var root = city.ControllingCity;
        int cap = 12 + root.Population / 3;

        if (root.Production != null)
        {
            if (root.Production.HasBuilding(CityBuildId.BuildChapel)) cap += 3;
            if (root.Production.HasBuilding(CityBuildId.BuildParishChurch)) cap += 5;
            if (root.Production.HasBuilding(CityBuildId.BuildGranary)) cap += 4;
            if (root.Production.HasBuilding(CityBuildId.BuildOrphanage)) cap += 3;
            if (root.Production.HasBuilding(CityBuildId.BuildHospital)) cap += 2;
        }

        foreach (var d in GetChildDistricts(root))
            cap += 5 + d.Population / 4;

        if (root.IsIndependentCity)
            cap = Mathf.Max(cap, root.Population);

        return cap;
    }

    public static int GetTotalWorkers(City city)
    {
        var root = city?.ControllingCity;
        if (root == null)
            return 0;
        return Mathf.Max(1, GetControllingPopulation(root) / WorkerPopulationDivisor);
    }

    public static int GetBusyWorkers(City city)
    {
        if (CityManager.Instance == null || city == null)
            return 0;

        var root = city.ControllingCity;
        int busy = 0;
        foreach (var c in CityManager.Instance.AllCities)
        {
            if (c == null || c.Faction != root.Faction)
                continue;
            if (c.ControllingCity != root)
                continue;
            if (c.Production != null && c.Production.IsProdBusy)
                busy += WorkersPerActiveProject;
        }
        return busy;
    }

    public static bool HasAvailableWorkers(City city) =>
        GetAvailableWorkers(city) >= WorkersPerActiveProject;

    public static int GetAvailableWorkers(City city) =>
        Evaluate(city).AvailableWorkers;

    public static float GetProductionWorkerMultiplier(City city)
    {
        if (city == null || city.Production == null || !city.Production.IsProdBusy)
            return 1f;

        var snap = Evaluate(city);
        if (snap.TotalWorkers <= 0)
            return 0f;

        float ratio = Mathf.Clamp01((float)snap.AvailableWorkers / snap.TotalWorkers);
        return Mathf.Max(0.15f, ratio);
    }

    static float ComputeSecularAppeal(City city, int foodSurplus)
    {
        var root = city.ControllingCity;
        float appeal = 18f;

        if (foodSurplus > 0)
            appeal += Mathf.Min(25f, foodSurplus * 3f);
        else
            appeal -= 15f;

        var breakdown = root.GetProductionBreakdown();
        appeal += breakdown.FromProduction * 2f;
        appeal += breakdown.FromFood * 1.5f;

        if (root.Production != null)
        {
            if (root.Production.HasBuilding(CityBuildId.BuildGranary)) appeal += 8f;
            if (root.Production.HasBuilding(CityBuildId.BuildMill)) appeal += 5f;
            if (root.Production.HasBuilding(CityBuildId.BuildMarketHall)) appeal += 7f;
            if (root.Production.HasBuilding(CityBuildId.BuildGuildWorkshop)) appeal += 4f;
            if (root.Production.HasBuilding(CityBuildId.BuildFortification)) appeal += 4f;
            if (root.Production.HasBuilding(CityBuildId.BuildBarracks)) appeal += 3f;
        }

        var faction = GetFactionMetrics(root);
        appeal += faction.CivicRestraint * 0.12f;
        if (faction.CivicRestraint > 75f && faction.SpiritualComfort < 45f)
            appeal *= 0.65f;

        return Mathf.Clamp(appeal, 0f, 100f);
    }

    static float ComputeSpiritualAppeal(City city)
    {
        var root = city.ControllingCity;
        float appeal = 15f;

        if (root.Production != null)
        {
            if (root.Production.HasBuilding(CityBuildId.BuildChapel)) appeal += 12f;
            if (root.Production.HasBuilding(CityBuildId.BuildParishChurch)) appeal += 10f;
            if (root.Production.HasBuilding(CityBuildId.BuildCathedral)) appeal += 15f;
            if (root.Production.HasBuilding(CityBuildId.BuildParishSchool)) appeal += 8f;
            if (root.Production.HasBuilding(CityBuildId.BuildScriptorium)) appeal += 5f;
            if (root.Production.HasBuilding(CityBuildId.BuildHospital)) appeal += 6f;
            if (root.Production.HasBuilding(CityBuildId.BuildOrphanage)) appeal += 5f;
        }

        foreach (var d in GetChildDistricts(root))
        {
            if (d.Specialty == HamletSpecialty.Seminary) appeal += 6f;
            if (d.Specialty == HamletSpecialty.Scholastic) appeal += 4f;
        }

        var faction = GetFactionMetrics(root);
        appeal += faction.Adherence * 0.15f;
        appeal += faction.SpiritualComfort * 0.1f;

        bool hasChapel = root.Production != null &&
            (root.Production.HasBuilding(CityBuildId.BuildChapel) ||
             root.Production.HasBuilding(CityBuildId.BuildParishChurch) ||
             root.Production.HasBuilding(CityBuildId.BuildCathedral));
        if (!hasChapel && appeal > 40f)
            appeal = 40f;

        return Mathf.Clamp(appeal, 0f, 100f);
    }

    public static float ComputeBlendedAppeal(float secular, float spiritual) =>
        Mathf.Sqrt(Mathf.Max(0f, secular) * Mathf.Max(0f, spiritual));

    static void ApplyWaltherTension(ref GrowthSnapshot snap, City city)
    {
        snap.MigrationMultiplier = 1f;
        snap.TensionLabel = "Balanced";

        var metrics = GetFactionMetrics(city);
        if (!metrics.UseWaltherTension)
            return;

        var mods = ConfessionResearchManager.Instance != null
            ? ConfessionResearchManager.Instance.GetEffectiveModifiers()
            : default;

        if (metrics.CivicRestraint > 65f && metrics.SpiritualComfort < 50f)
        {
            snap.TensionLabel = city.Faction == FactionId.Schismatic
                ? GetSchismaticTensionLabel(city)
                : "Legalism";
            snap.BlendedAppeal *= city.Faction == FactionId.LutheranSynod && mods.LegalismGuard ? 0.75f : 0.5f;
            snap.MigrationMultiplier *= city.Faction == FactionId.LutheranSynod && mods.LegalismGuard ? 0.85f : 0.6f;
        }
        else if (metrics.SpiritualComfort > 60f && metrics.Adherence < 65f)
        {
            snap.TensionLabel = "Antinomian drift";
            snap.MigrationMultiplier *= city.Faction == FactionId.LutheranSynod && mods.AntinomianGuard ? 1.1f : 1.35f;
        }
        else if (snap.SecularAppeal > 55f && snap.SpiritualAppeal < 30f)
        {
            snap.TensionLabel = "Secular prosperity";
            snap.BlendedAppeal *= 0.8f;
        }
        else if (snap.SpiritualAppeal > 55f && snap.SecularAppeal < 25f)
        {
            snap.TensionLabel = "Ghetto church";
            snap.MigrationMultiplier *= 0.5f;
        }

        ApplyHeresyFlavor(ref snap, city);
    }

    static void ApplyHeresyFlavor(ref GrowthSnapshot snap, City city)
    {
        if (city == null || city.Faction != FactionId.Schismatic ||
            city.SchismaticBloc == SchismaticBlocId.None ||
            SchismaticBlocRegistry.Instance == null)
            return;

        var profile = SchismaticBlocRegistry.Instance.ProfileForBloc(city.SchismaticBloc);
        snap.MigrationMultiplier *= profile.MigrationMultiplier;
        snap.SecularAppeal = Mathf.Clamp(snap.SecularAppeal + profile.SecularAppealBonus, 0f, 100f);
        snap.SpiritualAppeal = Mathf.Clamp(snap.SpiritualAppeal + profile.SpiritualAppealBonus, 0f, 100f);
        snap.BlendedAppeal = ComputeBlendedAppeal(snap.SecularAppeal, snap.SpiritualAppeal);
    }

    static string GetSchismaticTensionLabel(City city)
    {
        if (SchismaticBlocRegistry.Instance != null && city.SchismaticBloc != SchismaticBlocId.None)
            return SchismaticBlocRegistry.Instance.ProfileForBloc(city.SchismaticBloc).TensionLabel;
        return "Rigid dissent";
    }

    public static int GetMaxDistrictCount(City parent)
    {
        if (parent == null || parent.IsHamlet)
            return 0;

        int fromPop = parent.SizeTier switch
        {
            City.CitySizeTier.Capital => 3,
            City.CitySizeTier.Large => 3,
            City.CitySizeTier.Medium => 2,
            _ => 1
        };

        int territoryTiles = TerritoryManager.Instance?.GetTerritoryTileCount(parent) ?? 0;
        int fromTerritory = territoryTiles >= 16 ? 3 : territoryTiles >= 12 ? 2 : territoryTiles >= 8 ? 1 : 0;

        return Mathf.Clamp(fromPop + fromTerritory, 0, MaxDistrictsPerCity);
    }

    public static int CountChildDistricts(City parent)
    {
        int count = 0;
        foreach (var _ in GetChildDistricts(parent))
            count++;
        return count;
    }

    public static int RequiredSurplusStreakForDistrict(City parent) =>
        MinSurplusStreakForDistrict;

    public static bool CityEligibleForDistrictOffer(City parent)
    {
        if (parent == null || parent.IsHamlet)
            return false;
        if (TurnManager.Instance == null)
            return true;
        return TurnManager.Instance.TurnNumber - parent.FoundedOnTurn >= MinTurnsBeforeDistrictOffer;
    }

    public static bool MeetsDistrictFoodGate(GrowthSnapshot snap) =>
        snap.FoodSurplus > 0 || (snap.FoodSurplus == 0 && snap.HousingRoom <= 0);

    public static bool MeetsDistrictStreakCondition(GrowthSnapshot snap) =>
        MeetsDistrictFoodGate(snap);

    public static DistrictSiteOffer? FindBestDistrictOffer(City parent, int surplusStreak)
    {
        if (parent == null || parent.IsHamlet ||
            CityManager.Instance == null || HexGridMap.Instance == null ||
            TerritoryManager.Instance == null)
            return null;
        if (!CityEligibleForDistrictOffer(parent))
            return null;
        if (surplusStreak < RequiredSurplusStreakForDistrict(parent))
            return null;

        if (CountChildDistricts(parent) >= GetMaxDistrictCount(parent))
            return null;

        var snap = Evaluate(parent);
        if (!MeetsDistrictFoodGate(snap) || snap.BlendedAppeal < MigrationAppealThreshold)
            return null;

        var territory = TerritoryManager.Instance.GetTerritory(parent);
        if (territory == null || territory.Count == 0)
            return null;

        DistrictSiteOffer? best = null;
        float bestScore = 0f;

        foreach (var hex in territory)
        {
            if (!CityManager.Instance.IsValidHamletDistrictSite(hex, parent))
                continue;

            float score = ScoreDistrictHex(parent, hex, snap);
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = new DistrictSiteOffer
            {
                Parent = parent,
                Hex = hex,
                SuggestedSpecialty = SuggestSpecialty(parent, hex),
                SiteScore = score,
                FlavorReason = BuildDistrictFlavor(parent, hex, snap)
            };
        }

        return bestScore >= 20f ? best : null;
    }

    /// <summary>Why food+streak are met but FindBestDistrictOffer still fails.</summary>
    public static string ExplainDistrictBlocker(City parent, GrowthSnapshot snap)
    {
        if (parent == null)
            return "district blocked";
        if (snap.BlendedAppeal < MigrationAppealThreshold)
            return $"need appeal {MigrationAppealThreshold:F0}+ (now {snap.BlendedAppeal:F0})";

        if (CityManager.Instance == null || TerritoryManager.Instance == null || HexGridMap.Instance == null)
            return "no valid district site";

        var territory = TerritoryManager.Instance.GetTerritory(parent);
        if (territory == null || territory.Count == 0)
            return "no valid district site";

        float bestScore = 0f;
        bool anySite = false;
        foreach (var hex in territory)
        {
            if (!CityManager.Instance.IsValidHamletDistrictSite(hex, parent))
                continue;
            anySite = true;
            bestScore = Mathf.Max(bestScore, ScoreDistrictHex(parent, hex, snap));
        }

        if (!anySite)
            return "no valid district site";
        if (bestScore < 20f)
            return $"best site score {bestScore:F0}/20";
        return "district blocked";
    }

    public static float ScoreLocalAppealHex(City parent, HexCoordinates hex, GrowthSnapshot snap) =>
        ScoreDistrictHex(parent, hex, snap);

    static float ScoreDistrictHex(City parent, HexCoordinates hex, GrowthSnapshot snap)
    {
        float score = snap.BlendedAppeal * 0.35f;

        if (HexGridMap.Instance.TryGetTile(hex, out var tile))
        {
            var yield = TileYieldDatabase.GetTileYield(tile);
            score += yield.Food * 3f;
            score += yield.Production * 2f;
            score += yield.Manuscripts * 2.5f;
        }

        int dist = HexGridMap.Instance.WrappedDistance(hex, parent.HexPosition);
        score += Mathf.Max(0, 4 - dist) * 2f;

        return score;
    }

    public static HamletSpecialty SuggestSpecialty(City parent, HexCoordinates hex)
    {
        int seminary = 0, garrison = 0, market = 0, scholastic = 0;

        var identity = FirstSteps.Instance != null
            ? FirstSteps.Instance.confessionalIdentity
            : ConfessionalIdentityId.None;
        ApplyIdentityLean(identity, ref seminary, ref garrison, ref market, ref scholastic);

        HamletSpecialty identityPrimary = IdentityPrimarySpecialty(identity);
        int existingOfPrimary = 0;
        foreach (var child in GetChildDistricts(parent))
        {
            switch (child.Specialty)
            {
                case HamletSpecialty.Seminary: seminary -= 4; break;
                case HamletSpecialty.Garrison: garrison -= 4; break;
                case HamletSpecialty.Market: market -= 4; break;
                case HamletSpecialty.Scholastic: scholastic -= 4; break;
            }

            if (identityPrimary != HamletSpecialty.None && child.Specialty == identityPrimary)
                existingOfPrimary++;
        }

        if (identityPrimary != HamletSpecialty.None && existingOfPrimary == 0)
            AddToBucket(identityPrimary, 2, ref seminary, ref garrison, ref market, ref scholastic);

        if (parent.Production != null)
        {
            CountBuilding(parent.Production, CityBuildId.BuildChapel, ref seminary, 2);
            CountBuilding(parent.Production, CityBuildId.BuildParishSchool, ref seminary, 2);
            CountBuilding(parent.Production, CityBuildId.BuildParishChurch, ref seminary, 3);
            CountBuilding(parent.Production, CityBuildId.BuildOrganLoft, ref seminary, 2);
            CountBuilding(parent.Production, CityBuildId.BuildBarracks, ref garrison, 3);
            CountBuilding(parent.Production, CityBuildId.BuildArcheryRange, ref garrison, 2);
            CountBuilding(parent.Production, CityBuildId.BuildStable, ref garrison, 2);
            CountBuilding(parent.Production, CityBuildId.BuildArmory, ref garrison, 2);
            CountBuilding(parent.Production, CityBuildId.BuildFortification, ref garrison, 3);
            CountBuilding(parent.Production, CityBuildId.BuildGuildWorkshop, ref market, 2);
            CountBuilding(parent.Production, CityBuildId.BuildGranary, ref market, 2);
            CountBuilding(parent.Production, CityBuildId.BuildMill, ref market, 2);
            CountBuilding(parent.Production, CityBuildId.BuildMarketHall, ref market, 3);
            CountBuilding(parent.Production, CityBuildId.BuildScriptorium, ref scholastic, 2);
            CountBuilding(parent.Production, CityBuildId.BuildLibrary, ref scholastic, 3);
            CountBuilding(parent.Production, CityBuildId.BuildUniversity, ref scholastic, 4);
            CountBuilding(parent.Production, CityBuildId.BuildObservatory, ref scholastic, 2);
        }

        if (HexGridMap.Instance != null && HexGridMap.Instance.TryGetTile(hex, out var tile))
        {
            var y = TileYieldDatabase.GetTileYield(tile);
            market += y.Food + y.Production;
            scholastic += y.Manuscripts * 2;
        }

        int max = Mathf.Max(seminary, Mathf.Max(garrison, Mathf.Max(market, scholastic)));
        if (max <= 0)
            return identityPrimary != HamletSpecialty.None ? identityPrimary : HamletSpecialty.Market;

        // Ties: identity primary first, then Seminary → Garrison → Scholastic → Market.
        if (identityPrimary != HamletSpecialty.None && ScoreOf(identityPrimary, seminary, garrison, market, scholastic) == max)
            return identityPrimary;
        if (seminary == max) return HamletSpecialty.Seminary;
        if (garrison == max) return HamletSpecialty.Garrison;
        if (scholastic == max) return HamletSpecialty.Scholastic;
        return HamletSpecialty.Market;
    }

    static void ApplyIdentityLean(
        ConfessionalIdentityId identity,
        ref int seminary, ref int garrison, ref int market, ref int scholastic)
    {
        switch (identity)
        {
            case ConfessionalIdentityId.Magisterial:
                garrison += 5;
                seminary += 2;
                break;
            case ConfessionalIdentityId.PastoralCare:
                seminary += 5;
                market += 2;
                break;
            case ConfessionalIdentityId.MissionarySending:
                market += 5;
                seminary += 2;
                break;
            case ConfessionalIdentityId.ChemnitzConfessional:
                scholastic += 5;
                seminary += 2;
                break;
        }
    }

    static HamletSpecialty IdentityPrimarySpecialty(ConfessionalIdentityId identity) => identity switch
    {
        ConfessionalIdentityId.Magisterial => HamletSpecialty.Garrison,
        ConfessionalIdentityId.PastoralCare => HamletSpecialty.Seminary,
        ConfessionalIdentityId.MissionarySending => HamletSpecialty.Market,
        ConfessionalIdentityId.ChemnitzConfessional => HamletSpecialty.Scholastic,
        _ => HamletSpecialty.None
    };

    static void AddToBucket(
        HamletSpecialty specialty, int amount,
        ref int seminary, ref int garrison, ref int market, ref int scholastic)
    {
        switch (specialty)
        {
            case HamletSpecialty.Seminary: seminary += amount; break;
            case HamletSpecialty.Garrison: garrison += amount; break;
            case HamletSpecialty.Market: market += amount; break;
            case HamletSpecialty.Scholastic: scholastic += amount; break;
        }
    }

    static int ScoreOf(
        HamletSpecialty specialty, int seminary, int garrison, int market, int scholastic) => specialty switch
    {
        HamletSpecialty.Seminary => seminary,
        HamletSpecialty.Garrison => garrison,
        HamletSpecialty.Market => market,
        HamletSpecialty.Scholastic => scholastic,
        _ => int.MinValue
    };

    static void CountBuilding(CityProduction prod, CityBuildId id, ref int bucket, int weight)
    {
        if (prod.HasBuilding(id))
            bucket += weight;
    }

    static string BuildDistrictFlavor(City parent, HexCoordinates hex, GrowthSnapshot snap)
    {
        if (snap.SpiritualAppeal > snap.SecularAppeal + 10f)
            return "Parish families gather where the Word is strong and the Gospel draws settlers.";
        if (snap.SecularAppeal > snap.SpiritualAppeal + 10f)
            return "Craftsmen and traders seek land near prosperous fields and orderly civic life.";
        return "Both kingdoms align here  -  bread and Word enough for a new district.";
    }

    public static IEnumerable<City> GetChildDistricts(City parent)
    {
        if (CityManager.Instance == null || parent == null)
            yield break;

        foreach (var c in CityManager.Instance.AllCities)
        {
            if (c != null && c.IsHamlet && c.ParentCity == parent)
                yield return c;
        }
    }

    public static void ApplyHybridFoodDeficit(City root, GrowthSnapshot snap)
    {
        if (root == null || snap.FoodSurplus >= 0)
            return;

        if (root.IsCapital && TurnManager.Instance != null &&
            TurnManager.Instance.TurnNumber - root.FoundedOnTurn < CapitalDeficitGraceTurns)
            return;

        int remaining = Mathf.CeilToInt(-snap.FoodSurplus / 2f);
        remaining = Mathf.Clamp(remaining, 1, 4);

        foreach (var district in GetChildDistricts(root).OrderBy(d =>
                     GetDistrictLocalFoodConsumption(d) - GetDistrictLocalFoodContribution(d)).Reverse())
        {
            int localGap = GetDistrictLocalFoodConsumption(district) - GetDistrictLocalFoodContribution(district);
            if (localGap <= 0 || district.Population <= 2)
                continue;

            int loss = Mathf.Min(remaining, Mathf.Min(2, district.Population - 2));
            if (loss <= 0)
                continue;

            district.Population -= loss;
            district.RefreshAppearance();
            remaining -= loss;
            Debug.LogWarning($"{district.CityName}: local food shortfall  -  {loss} departed.");

            if (remaining <= 0)
                return;
        }

        if (remaining > 0 && root.Population > 5)
        {
            int loss = Mathf.Min(remaining, root.Population - 5);
            root.Population -= loss;
            root.RefreshAppearance();
            Debug.LogWarning($"{root.CityName}: cluster food deficit {snap.FoodSurplus}  -  {loss} settlers departed.");
        }

        if (root.Faction == FactionId.LutheranSynod && root.SynodPlayer == SynodPlayerId.Player1)
            PopulationSync.SyncPlayerFactionFromCities();
    }

    public static string FormatGrowthLine(City city)
    {
        if (city == null || city.IsHamlet)
            return null;

        var s = Evaluate(city);
        var sb = new StringBuilder();
        sb.Append("<b>Growth</b> ");
        sb.Append(s.FoodSurplus >= 0
            ? $"<color=#88DDAA>food +{s.FoodSurplus}</color> ({s.FoodProduced}/{s.FoodConsumed})"
            : $"<color=#FF9988>food {s.FoodSurplus}</color> ({s.FoodProduced}/{s.FoodConsumed})");
        sb.Append($"  |  appeal {s.BlendedAppeal:F0} (L{s.SecularAppeal:F0}/G{s.SpiritualAppeal:F0})");
        sb.Append($"  |  housing {city.Population}/{s.HousingCap}");
        sb.Append($"  |  workers {s.AvailableWorkers}/{s.TotalWorkers}");

        int streak = CityGrowthManager.Instance?.GetSurplusStreak(city) ?? 0;
        int streakNeed = RequiredSurplusStreakForDistrict(city);
        if (CountChildDistricts(city) < GetMaxDistrictCount(city))
        {
            int cooldown = CityGrowthManager.Instance?.GetDistrictOfferCooldown(city) ?? 0;
            bool pendingHere = CityGrowthManager.Instance != null &&
                               CityGrowthManager.Instance.HasPendingOffer;
            if (cooldown > 0)
                sb.Append($"  |  <color=#99AABB>district offer cooldown {cooldown}t</color>");
            else if (pendingHere && DistrictOfferPanel.Instance != null && DistrictOfferPanel.Instance.IsVisible)
                sb.Append("  |  <color=#DDEE88>district offer open</color>");
            else if (!CityEligibleForDistrictOffer(city))
            {
                int need = MinTurnsBeforeDistrictOffer;
                int age = TurnManager.Instance != null
                    ? TurnManager.Instance.TurnNumber - city.FoundedOnTurn
                    : 0;
                sb.Append($"  |  <color=#99AABB>districts unlock in {Mathf.Max(0, need - age)}t</color>");
            }
            else if (FindBestDistrictOffer(city, streak).HasValue)
                sb.Append("  |  <color=#DDEE88>district offer ready (end turn)</color>");
            else if (MeetsDistrictFoodGate(s) && streak >= streakNeed)
                sb.Append($"  |  <color=#99AABB>{ExplainDistrictBlocker(city, s)}</color>");
            else if (MeetsDistrictFoodGate(s) && streak > 0)
                sb.Append($"  |  district streak {streak}/{streakNeed}");
            else if (!MeetsDistrictFoodGate(s))
                sb.Append("  |  <color=#99AABB>need food surplus for districts</color>");
            else if (MeetsDistrictFoodGate(s) && streak < streakNeed)
                sb.Append($"  |  district streak {streak}/{streakNeed}");
        }

        if (s.TensionLabel != "Balanced")
            sb.Append($"  |  <color=#FFCC88>{s.TensionLabel}</color>");
        return sb.ToString();
    }
}
