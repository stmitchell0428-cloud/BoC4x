using System.Collections.Generic;
using UnityEngine;

public class CityProduction : MonoBehaviour
{
    City city;
    readonly HashSet<CityBuildId> completedBuildings = new();
    CityBuildId? activeBuild;
    int turnsRemaining;
    int productionProgress;

    public event System.Action ProductionChanged;

    public void Bind(City owner) => city = owner;

    public bool HasBuilding(CityBuildId id) => completedBuildings.Contains(id);

    public bool IsProducing => activeBuild.HasValue;

    public CityBuildId? ActiveBuildId => activeBuild;

    public int ProductionProgress => productionProgress;

    public int TurnsRemainingOnProject => turnsRemaining;

    public int? EstimatedTurnsRemaining()
    {
        if (!activeBuild.HasValue || city == null)
            return null;

        var def = CityBuildDatabase.Get(activeBuild.Value);
        if (!def.UsesProduction)
            return turnsRemaining;

        int remaining = def.ProductionCost - productionProgress;
        if (remaining <= 0)
            return 0;

        int yield = city.GetProductionPerTurn();
        if (yield <= 0)
            return null;

        return Mathf.CeilToInt(remaining / (float)yield);
    }

    public string ActiveBuildLabel()
    {
        if (!activeBuild.HasValue)
            return "None";

        var def = CityBuildDatabase.Get(activeBuild.Value);
        int? eta = EstimatedTurnsRemaining();

        if (def.UsesProduction)
        {
            string etaText = eta.HasValue ? $", ~{eta.Value}t left" : "";
            return $"{def.Name} ({productionProgress}/{def.ProductionCost} prod{etaText})";
        }

        return $"{def.Name} ({turnsRemaining}t left)";
    }

    /// <summary>Compact label for HUD lines (name + turns remaining).</summary>
    public string ActiveBuildHudLabel()
    {
        if (!activeBuild.HasValue)
            return "None";

        var def = CityBuildDatabase.Get(activeBuild.Value);
        int? eta = EstimatedTurnsRemaining();
        if (eta.HasValue)
            return $"{def.Name} ({eta.Value}t left)";

        if (def.UsesProduction)
            return $"{def.Name} ({productionProgress}/{def.ProductionCost} prod)";

        return def.Name;
    }

    public string ActiveBuildProgressBlock()
    {
        if (!activeBuild.HasValue)
            return "<color=#888888>No project in the queue.</color>";

        var def = CityBuildDatabase.Get(activeBuild.Value);
        int? eta = EstimatedTurnsRemaining();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Current project</b>");
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

        return sb.ToString().TrimEnd();
    }

    public CityBuildStatus GetStatus(CityBuildId id)
    {
        var def = CityBuildDatabase.Get(id);
        if (def.UniquePerCity && completedBuildings.Contains(id))
            return CityBuildStatus.Completed;
        if (activeBuild == id)
            return CityBuildStatus.Building;

        if (city.Faction != FactionId.LutheranSynod)
            return CityBuildStatus.Locked;
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return CityBuildStatus.Locked;
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return CityBuildStatus.Locked;
        if (activeBuild.HasValue)
            return CityBuildStatus.Locked;

        if (def.UsesProduction && CityGrowthSystem.GetProductionWorkerMultiplier(city) <= 0.15f)
            return CityBuildStatus.Locked;

        if (def.RequiredTech.HasValue &&
            (ConfessionResearchManager.Instance == null ||
             !ConfessionResearchManager.Instance.IsTechUnlocked(def.RequiredTech.Value)))
            return CityBuildStatus.Locked;

        if (city.IsHamlet && !city.HasChosenSpecialty)
            return CityBuildStatus.Locked;

        if (!HamletSpecialtyDatabase.IsBuildAllowed(city, id))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.FoundHamlet)
            return CityBuildStatus.Locked;

        if (id == CityBuildId.BuildCathedral && !city.IsCapital)
            return CityBuildStatus.Locked;

        if (id == CityBuildId.TrainColonist && !MissionHouseChain.CanTrainColonist(city))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.TrainSiegeEngine &&
            (city.Production == null || !city.Production.HasBuilding(CityBuildId.BuildArmory)))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.TrainCoastalPatrol && !CityManager.Instance.CityTouchesNavalCoast(city))
            return CityBuildStatus.Locked;

        if ((id == CityBuildId.BuildDock || id == CityBuildId.TrainCoastalGalley) &&
            !CityManager.Instance.CityTouchesNavalCoast(city))
            return CityBuildStatus.Locked;

        if (id == CityBuildId.TrainCoastalGalley &&
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
                    CityBuildId.TrainColonist => MissionHouseChain.EffectiveColonistCost(city),
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
            if (id == CityBuildId.TrainColonist)
                manuscriptCost = MissionHouseChain.EffectiveColonistCost(city);
            else if (id == CityBuildId.TrainMissionary)
                manuscriptCost = MissionHouseChain.EffectiveMissionaryCost(city);

            if (faction.scriptureManuscripts < manuscriptCost)
                return false;
            faction.ScriptureManuscripts -= manuscriptCost;
            turnsRemaining = def.TurnsToComplete;
            if (id == CityBuildId.TrainColonist)
                turnsRemaining = Mathf.Max(1, turnsRemaining - MissionHouseChain.ColonistTurnReduction(city));
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
        }
        else
        {
            productionProgress = 0;
        }

        activeBuild = id;

        if (def.UsesProduction)
            Debug.Log($"{city.CityName}: started {def.Name} ({def.ProductionCost} production required).");
        else
            Debug.Log($"{city.CityName}: started {def.Name} ({turnsRemaining} turns).");

        ProductionChanged?.Invoke();
        faction.RefreshDashboard();
        return true;
    }

    public bool TryStartAiBuild(CityBuildId id)
    {
        if (city.Faction == FactionId.LutheranSynod || activeBuild.HasValue)
            return false;

        var def = CityBuildDatabase.Get(id);
        if (def.UniquePerCity && completedBuildings.Contains(id))
            return false;

        if (!def.UsesProduction)
            turnsRemaining = def.TurnsToComplete;
        else
            productionProgress = 0;

        activeBuild = id;
        Debug.Log($"{city.CityName} (AI): started {def.Name}.");
        ProductionChanged?.Invoke();
        return true;
    }

    public bool CancelActiveBuild()
    {
        if (!activeBuild.HasValue)
            return false;

        var def = CityBuildDatabase.Get(activeBuild.Value);
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

        activeBuild = null;
        turnsRemaining = 0;
        productionProgress = 0;
        ProductionChanged?.Invoke();
        FirstSteps.Instance?.RefreshDashboard();
        return true;
    }

    public void OnCityCaptured()
    {
        activeBuild = null;
        turnsRemaining = 0;
        productionProgress = 0;
        ProductionChanged?.Invoke();
    }

    public void AdvanceTurn()
    {
        int yield = city.GetProductionPerTurn();

        if (!activeBuild.HasValue)
        {
            ApplyPerTurnBuildingEffects();
            return;
        }

        var def = CityBuildDatabase.Get(activeBuild.Value);
        if (def.UsesProduction)
        {
            yield = Mathf.Max(1, Mathf.RoundToInt(yield * CityGrowthSystem.GetProductionWorkerMultiplier(city)));
            productionProgress += yield;
            string workerNote = CityGrowthSystem.GetProductionWorkerMultiplier(city) < 1f ? " (short workers)" : "";
            Debug.Log($"{city.CityName}: +{yield} production toward {def.Name} ({productionProgress}/{def.ProductionCost}){workerNote}.");

            if (productionProgress >= def.ProductionCost)
            {
                CompleteBuild(activeBuild.Value);
                activeBuild = null;
                productionProgress = 0;
            }
        }
        else
        {
            turnsRemaining--;
            if (turnsRemaining <= 0)
            {
                CompleteBuild(activeBuild.Value);
                activeBuild = null;
                turnsRemaining = 0;
            }
        }

        ApplyPerTurnBuildingEffects();
        ProductionChanged?.Invoke();
        FirstSteps.Instance?.RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshCityYield();
    }

    void CompleteBuild(CityBuildId id)
    {
        if (id == CityBuildId.FoundHamlet)
        {
            if (CityManager.Instance != null && CityManager.Instance.TryFoundHamlet(city))
                Debug.Log($"{city.CityName}: completed Found Hamlet.");
            else
                Debug.LogWarning($"{city.CityName}: Found Hamlet finished but no adjacent district site was free.");
            return;
        }

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
                faction.population += 5;
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
                faction.population += 3;
                city.Population += 3;
                city.RefreshAppearance();
                break;
            case CityBuildId.BuildMissionHouse:
                faction.AddFame(2);
                Debug.Log($"{city.CityName}: Mission House ready  -  colonists can deploy from this cluster.");
                break;
            case CityBuildId.BuildFortification:
                faction.confessionalAdherence = Mathf.Clamp(faction.confessionalAdherence + 5f, 0f, 100f);
                break;
            case CityBuildId.BuildOrphanage:
                faction.population += 3;
                break;
            case CityBuildId.BuildGranary:
                faction.population += 2;
                city.Population += 2;
                city.RefreshAppearance();
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

        if (HasBuilding(CityBuildId.BuildHospital) && Random.value < 0.35f)
        {
            city.Population += 1;
            faction.population += 1;
            city.RefreshAppearance();
            Debug.Log($"{city.CityName}: Hospital tended the sick (+1 population).");
        }
    }

    public int CompletedBuildingCount => completedBuildings.Count;
}
