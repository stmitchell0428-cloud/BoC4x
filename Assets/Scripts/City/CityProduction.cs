using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Dual tracks: mss/turn-timer jobs can run alongside production-point jobs (one each).
/// </summary>
public class CityProduction : MonoBehaviour
{
    City city;
    readonly HashSet<CityBuildId> completedBuildings = new();
    CityBuildId? activeTimerBuild;
    int turnsRemaining;
    CityBuildId? activeProdBuild;
    int productionProgress;

    public event System.Action ProductionChanged;

    public void Bind(City owner) => city = owner;

    public bool HasBuilding(CityBuildId id) => completedBuildings.Contains(id);

    public bool IsProducing => activeTimerBuild.HasValue || activeProdBuild.HasValue;
    public bool IsTimerBusy => activeTimerBuild.HasValue;
    public bool IsProdBusy => activeProdBuild.HasValue;

    public CityBuildId? ActiveTimerBuildId => activeTimerBuild;
    public CityBuildId? ActiveProdBuildId => activeProdBuild;

    /// <summary>Prefer prod slot, else timer — for single-slot UI fallbacks.</summary>
    public CityBuildId? ActiveBuildId => activeProdBuild ?? activeTimerBuild;

    public int ProductionProgress => productionProgress;
    public int TurnsRemainingOnProject => turnsRemaining;

    public bool IsBuilding(CityBuildId id) =>
        activeTimerBuild == id || activeProdBuild == id;

    public int? EstimatedTurnsRemaining() =>
        ActiveBuildId.HasValue ? EstimatedTurnsRemainingFor(ActiveBuildId.Value) : null;

    public int? EstimatedTurnsRemainingFor(CityBuildId id)
    {
        if (city == null)
            return null;

        var def = CityBuildDatabase.Get(id);
        if (def.UsesProduction)
        {
            if (activeProdBuild != id)
                return null;
            int remaining = def.ProductionCost - productionProgress;
            if (remaining <= 0)
                return 0;
            int yield = city.GetProductionPerTurn();
            if (yield <= 0)
                return null;
            yield = Mathf.Max(1, Mathf.RoundToInt(yield * CityGrowthSystem.GetProductionWorkerMultiplier(city)));
            return Mathf.CeilToInt(remaining / (float)yield);
        }

        if (activeTimerBuild != id)
            return null;
        return turnsRemaining;
    }

    public string ActiveBuildLabel()
    {
        if (!IsProducing)
            return "None";

        var parts = new List<string>(2);
        if (activeTimerBuild.HasValue)
            parts.Add(FormatSlotLabel(activeTimerBuild.Value, hud: false));
        if (activeProdBuild.HasValue)
            parts.Add(FormatSlotLabel(activeProdBuild.Value, hud: false));
        return string.Join(" · ", parts);
    }

    public string ActiveBuildHudLabel()
    {
        if (!IsProducing)
            return "None";

        var parts = new List<string>(2);
        if (activeTimerBuild.HasValue)
            parts.Add(FormatSlotLabel(activeTimerBuild.Value, hud: true));
        if (activeProdBuild.HasValue)
            parts.Add(FormatSlotLabel(activeProdBuild.Value, hud: true));
        return string.Join(" · ", parts);
    }

    string FormatSlotLabel(CityBuildId id, bool hud)
    {
        var def = CityBuildDatabase.Get(id);
        int? eta = EstimatedTurnsRemainingFor(id);
        if (hud)
        {
            if (eta.HasValue)
                return $"{def.Name} ({eta.Value}t left)";
            if (def.UsesProduction)
                return $"{def.Name} ({productionProgress}/{def.ProductionCost} prod)";
            return def.Name;
        }

        if (def.UsesProduction)
        {
            string etaText = eta.HasValue ? $", ~{eta.Value}t left" : "";
            return $"{def.Name} ({productionProgress}/{def.ProductionCost} prod{etaText})";
        }

        return $"{def.Name} ({turnsRemaining}t left)";
    }

    public string ActiveBuildProgressBlock()
    {
        if (!IsProducing)
            return "<color=#888888>No project in the queue.</color>";

        var sb = new StringBuilder();
        if (activeTimerBuild.HasValue)
            AppendProgressBlock(sb, activeTimerBuild.Value);
        if (activeProdBuild.HasValue)
        {
            if (activeTimerBuild.HasValue)
                sb.AppendLine();
            AppendProgressBlock(sb, activeProdBuild.Value);
        }

        return sb.ToString().TrimEnd();
    }

    void AppendProgressBlock(StringBuilder sb, CityBuildId id)
    {
        var def = CityBuildDatabase.Get(id);
        int? eta = EstimatedTurnsRemainingFor(id);
        sb.AppendLine(def.UsesProduction ? "<b>Production project</b>" : "<b>Manuscript / turn project</b>");
        sb.AppendLine(def.Name);

        if (def.UsesProduction)
        {
            sb.AppendLine($"Progress: {productionProgress} / {def.ProductionCost} production");
            sb.AppendLine($"City yield: {city.ProductionYieldLabel()}");
            if (eta.HasValue)
                sb.AppendLine($"About {eta.Value} turn{(eta.Value == 1 ? "" : "s")} remaining");
            else
                sb.AppendLine("Need production yield to finish");
        }
        else
        {
            sb.AppendLine($"Turns remaining: {turnsRemaining}");
            if (def.ManuscriptCost > 0)
                sb.AppendLine($"Paid: {def.ManuscriptCost} manuscripts");
        }
    }

    public CityBuildStatus GetStatus(CityBuildId id)
    {
        var def = CityBuildDatabase.Get(id);
        if (def.UniquePerCity && completedBuildings.Contains(id))
            return CityBuildStatus.Completed;
        if (IsBuilding(id))
            return CityBuildStatus.Building;

        if (city.Faction != FactionId.LutheranSynod)
            return CityBuildStatus.Locked;
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return CityBuildStatus.Locked;
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return CityBuildStatus.Locked;

        if (def.UsesProduction)
        {
            if (activeProdBuild.HasValue)
                return CityBuildStatus.Locked;
            if (CityGrowthSystem.GetProductionWorkerMultiplier(city) <= 0.15f)
                return CityBuildStatus.Locked;
        }
        else if (activeTimerBuild.HasValue)
        {
            return CityBuildStatus.Locked;
        }

        if (!CityBuildDatabase.MeetsTechRequirements(def))
            return CityBuildStatus.Locked;

        if (city.IsHamlet && !city.HasChosenSpecialty)
            return CityBuildStatus.Locked;

        if (!HamletSpecialtyDatabase.IsBuildAllowed(city, id))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.BuildCathedral && !city.IsCapital)
            return CityBuildStatus.Locked;

        if (id == CityBuildId.TrainFrontierSettler && !MissionHouseChain.CanTrainFrontierSettler(city))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.TrainSiegeEngine &&
            (city.Production == null || !city.Production.HasBuilding(CityBuildId.BuildArmory)))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.TrainCoastalExplorer && !CityManager.Instance.CityTouchesNavalCoast(city))
            return CityBuildStatus.Locked;

        if ((id == CityBuildId.BuildWharf || id == CityBuildId.BuildFishingPost ||
             id == CityBuildId.BuildDock || id == CityBuildId.TrainCoastalGalley ||
             id == CityBuildId.TrainCoastalExplorer || id == CityBuildId.TrainDeepSeaShip) &&
            !CityManager.Instance.CityTouchesNavalCoast(city))
            return CityBuildStatus.Locked;

        if (NavalMovementRules.RequiresWharf(id) &&
            id != CityBuildId.BuildWharf &&
            (city.Production == null || !city.Production.HasBuilding(CityBuildId.BuildWharf)))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.BuildDock &&
            (city.Production == null || !city.Production.HasBuilding(CityBuildId.BuildWharf)))
            return CityBuildStatus.Locked;

        if (NavalMovementRules.RequiresDock(id) &&
            (city.Production == null || !city.Production.HasBuilding(CityBuildId.BuildDock)))
            return CityBuildStatus.Locked;

        if (def.SpawnsUnit.HasValue && !ClergyRoster.CanTrainBuild(city, id))
            return CityBuildStatus.Locked;

        if (def.SpawnsUnit.HasValue && ClergyRoster.IsClergyUnit(def.SpawnsUnit.Value) &&
            !ClergyRoster.CanTrainClergy(city, def.SpawnsUnit.Value))
            return CityBuildStatus.ClergySlotsFull;

        if (!def.UsesProduction)
        {
            var faction = FirstSteps.Instance;
            if (faction != null)
            {
                int cost = id switch
                {
                    CityBuildId.TrainFrontierSettler => MissionHouseChain.EffectiveFrontierSettlerCost(city),
                    CityBuildId.TrainMissionary => MissionHouseChain.EffectiveMissionaryCost(city),
                    _ => def.ManuscriptCost
                };
                if (faction.scriptureManuscripts < cost)
                    return CityBuildStatus.Locked;
            }
        }

        return CityBuildStatus.Available;
    }

    public bool TryStartBuild(CityBuildId id)
    {
        if (GetStatus(id) != CityBuildStatus.Available)
            return false;

        var faction = FirstSteps.Instance;
        if (faction == null)
            return false;

        var def = CityBuildDatabase.Get(id);
        if (!def.UsesProduction)
        {
            int manuscriptCost = def.ManuscriptCost;
            if (id == CityBuildId.TrainFrontierSettler)
                manuscriptCost = MissionHouseChain.EffectiveFrontierSettlerCost(city);
            else if (id == CityBuildId.TrainMissionary)
                manuscriptCost = MissionHouseChain.EffectiveMissionaryCost(city);

            if (faction.scriptureManuscripts < manuscriptCost)
                return false;
            faction.ScriptureManuscripts -= manuscriptCost;
            turnsRemaining = def.TurnsToComplete;
            if (id == CityBuildId.TrainFrontierSettler)
                turnsRemaining = Mathf.Max(1, turnsRemaining - MissionHouseChain.SettlerTurnReduction(city));
            if (id == CityBuildId.TrainMissionary)
                turnsRemaining = Mathf.Max(1, turnsRemaining - MissionHouseChain.MissionaryTurnReduction(city));
            if (id == CityBuildId.TrainSoldier && HasBuilding(CityBuildId.BuildBarracks))
                turnsRemaining = Mathf.Max(1, turnsRemaining - 1);
            if (id == CityBuildId.TrainArcher && HasBuilding(CityBuildId.BuildArcheryRange))
                turnsRemaining = Mathf.Max(1, turnsRemaining - 1);
            if (id == CityBuildId.TrainHorseman && HasBuilding(CityBuildId.BuildStable))
                turnsRemaining = Mathf.Max(1, turnsRemaining - 1);
            if (id == CityBuildId.TrainSiegeEngine && HasBuilding(CityBuildId.BuildArmory))
                turnsRemaining = Mathf.Max(1, turnsRemaining - 1);

            activeTimerBuild = id;
            Debug.Log($"{city.CityName}: started {def.Name} ({turnsRemaining} turns).");
        }
        else
        {
            productionProgress = 0;
            activeProdBuild = id;
            Debug.Log($"{city.CityName}: started {def.Name} ({def.ProductionCost} production required).");
        }

        ProductionChanged?.Invoke();
        faction.RefreshDashboard();
        return true;
    }

    public bool TryStartAiBuild(CityBuildId id)
    {
        if (city.Faction == FactionId.LutheranSynod && city.SynodPlayer == SynodPlayerId.Player1)
            return false;

        var def = CityBuildDatabase.Get(id);
        if (def.UniquePerCity && completedBuildings.Contains(id))
            return false;

        if (!CityBuildDatabase.MeetsTechRequirements(def))
            return false;

        if (def.UsesProduction)
        {
            if (activeProdBuild.HasValue)
                return false;
            productionProgress = 0;
            activeProdBuild = id;
        }
        else
        {
            if (activeTimerBuild.HasValue)
                return false;
            turnsRemaining = def.TurnsToComplete;
            activeTimerBuild = id;
        }

        Debug.Log($"{city.CityName} (AI): started {def.Name}.");
        ProductionChanged?.Invoke();
        return true;
    }

    public bool CancelActiveBuild()
    {
        bool any = false;
        if (activeTimerBuild.HasValue)
            any |= CancelBuild(activeTimerBuild.Value);
        if (activeProdBuild.HasValue)
            any |= CancelBuild(activeProdBuild.Value);
        return any;
    }

    public bool CancelBuild(CityBuildId id)
    {
        if (!IsBuilding(id))
            return false;

        var def = CityBuildDatabase.Get(id);
        if (!def.UsesProduction && def.ManuscriptCost > 0 && city.Faction == FactionId.LutheranSynod)
        {
            var faction = FirstSteps.Instance;
            if (faction != null)
            {
                int refund = Mathf.Max(1, def.ManuscriptCost / 2);
                faction.ScriptureManuscripts += refund;
                Debug.Log($"{city.CityName}: cancelled {def.Name}, refunded {refund} manuscripts.");
            }
        }
        else
        {
            Debug.Log($"{city.CityName}: cancelled {def.Name}.");
        }

        if (activeTimerBuild == id)
        {
            activeTimerBuild = null;
            turnsRemaining = 0;
        }

        if (activeProdBuild == id)
        {
            activeProdBuild = null;
            productionProgress = 0;
        }

        ProductionChanged?.Invoke();
        FirstSteps.Instance?.RefreshDashboard();
        return true;
    }

    public void OnCityCaptured()
    {
        activeTimerBuild = null;
        turnsRemaining = 0;
        activeProdBuild = null;
        productionProgress = 0;
        ProductionChanged?.Invoke();
    }

    public void AdvanceTurn()
    {
        bool changed = false;

        if (activeProdBuild.HasValue)
        {
            var def = CityBuildDatabase.Get(activeProdBuild.Value);
            int yield = city.GetProductionPerTurn();
            yield = Mathf.Max(1, Mathf.RoundToInt(yield * CityGrowthSystem.GetProductionWorkerMultiplier(city)));
            productionProgress += yield;
            string workerNote = CityGrowthSystem.GetProductionWorkerMultiplier(city) < 1f ? " (short workers)" : "";
            Debug.Log($"{city.CityName}: +{yield} production toward {def.Name} ({productionProgress}/{def.ProductionCost}){workerNote}.");

            if (productionProgress >= def.ProductionCost)
            {
                var completed = activeProdBuild.Value;
                activeProdBuild = null;
                productionProgress = 0;
                CompleteBuild(completed);
            }

            changed = true;
        }

        if (activeTimerBuild.HasValue)
        {
            turnsRemaining--;
            if (turnsRemaining <= 0)
            {
                var completed = activeTimerBuild.Value;
                activeTimerBuild = null;
                turnsRemaining = 0;
                CompleteBuild(completed);
            }

            changed = true;
        }

        ApplyPerTurnBuildingEffects();
        if (changed)
        {
            ProductionChanged?.Invoke();
            FirstSteps.Instance?.RefreshDashboard();
            TerrainInfoPanel.Instance?.RefreshCityYield();
        }
    }

    void CompleteBuild(CityBuildId id)
    {
        if (id == CityBuildId.BindCatechism)
        {
            var faction = FirstSteps.Instance;
            if (faction != null)
            {
                faction.AddBoundCatechism(1);
                faction.AddFame(3);
                Debug.Log($"{city.CityName}: bound a catechism (+1 catechism stock).");
            }
            return;
        }

        if (id == CityBuildId.TrainFrontierSettler)
        {
            if (CityManager.Instance != null && CityManager.Instance.TrySpawnFrontierSettler(city))
                Debug.Log($"{city.CityName}: frontier settler ready to found a second city.");
            return;
        }

        var def = CityBuildDatabase.Get(id);
        if (def.SpawnsUnit.HasValue)
        {
            if (CityManager.Instance != null &&
                CityManager.Instance.TrySpawnUnit(city, def.SpawnsUnit.Value))
            {
                Debug.Log($"{city.CityName}: completed {def.Name}  -  unit deployed.");
            }
            else
            {
                Debug.LogWarning($"{city.CityName}: {def.Name} finished but no adjacent hex was free.");
            }
            return;
        }

        completedBuildings.Add(id);
        ApplyInstantBuildingEffect(id);
        if (id == CityBuildId.BuildLibrary && city.Faction == FactionId.LutheranSynod)
            TestimonyColloquyManager.Instance?.OnLibraryBuilt(city);
        Debug.Log($"{city.CityName}: completed {def.Name}  -  {def.EffectSummary}");
    }

    void ApplyInstantBuildingEffect(CityBuildId id)
    {
        if (city.Faction != FactionId.LutheranSynod) return;

        var faction = FirstSteps.Instance;
        if (faction == null) return;

        switch (id)
        {
            case CityBuildId.BuildParishSchool:
                city.Population += 5;
                city.RefreshAppearance();
                break;
            case CityBuildId.BuildChapel:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 5f, 0f, 100f);
                faction.AddFame(5);
                break;
            case CityBuildId.BuildCathedral:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 10f, 0f, 100f);
                faction.AddFame(10);
                break;
            case CityBuildId.BuildHospital:
                city.Population += 3;
                city.RefreshAppearance();
                break;
            case CityBuildId.BuildMissionHouse:
                faction.AddFame(2);
                Debug.Log($"{city.CityName}: Mission House ready  -  settlers can deploy from this cluster.");
                break;
            case CityBuildId.BuildFortification:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 5f, 0f, 100f);
                break;
            case CityBuildId.BuildOrphanage:
                city.Population += 3;
                city.RefreshAppearance();
                break;
            case CityBuildId.BuildGranary:
                Debug.Log($"{city.CityName}: Parish Granary ready  -  +3 food/turn from stored grain.");
                break;
            case CityBuildId.BuildParishChurch:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 8f, 0f, 100f);
                faction.AddFame(4);
                break;
            case CityBuildId.BuildOrganLoft:
                city.ControllingCity.AddCulturePoints(3f);
                faction.spiritualComfort = Mathf.Clamp(faction.spiritualComfort + 2f, 0f, 100f);
                break;
            case CityBuildId.BuildWatchtower:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 5f, 0f, 100f);
                break;
        }
    }

    void ApplyPerTurnBuildingEffects()
    {
        if (city.Faction != FactionId.LutheranSynod) return;

        var faction = FirstSteps.Instance;
        if (faction == null) return;

        if (HasBuilding(CityBuildId.BuildScriptorium))
        {
            faction.ScriptureManuscripts += 1;
            Debug.Log($"{city.CityName}: Scriptorium produced 1 manuscript.");
        }

        if (HasBuilding(CityBuildId.BuildPrintingPress))
        {
            faction.ScriptureManuscripts += 1;
            Debug.Log($"{city.CityName}: Printing Press produced 1 manuscript.");
        }

        if (HasBuilding(CityBuildId.BuildLibrary))
        {
            faction.ScriptureManuscripts += 1;
            Debug.Log($"{city.CityName}: Library produced 1 manuscript.");
        }

        if (HasBuilding(CityBuildId.BuildHospital))
        {
            city.Population += 1;
            city.RefreshAppearance();
            Debug.Log($"{city.CityName}: Hospital tended the sick (+1 population).");
        }

        if (HasBuilding(CityBuildId.BuildGranary) && Random.value < 0.5f)
        {
            city.Population += 1;
            city.RefreshAppearance();
            Debug.Log($"{city.CityName}: Granary tithe fed the needy (+1 population).");
        }
    }

    public int CompletedBuildingCount => completedBuildings.Count;
}
