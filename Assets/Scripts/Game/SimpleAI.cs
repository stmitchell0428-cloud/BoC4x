using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimpleAI : MonoBehaviour
{
    public static SimpleAI Instance { get; private set; }

    const int MaxUnitActionsPerTurn = 12;

    FactionId pendingFinishFaction = FactionId.LutheranSynod;
    SynodPlayerId pendingFinishSynod = SynodPlayerId.None;
    SchismaticBlocId pendingFinishBloc = SchismaticBlocId.None;

    void Awake() => Instance = this;

    public void PlaySynodTurn(SynodPlayerId playerId)
    {
        CancelInvoke(nameof(FinishAiTurn));

        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
        {
            TurnManager.Instance?.EndTurn();
            return;
        }

        var tm = TurnManager.Instance;
        var map = HexGridMap.Instance;
        var aiCity = CityManager.Instance?.GetSynodPlayerCapital(playerId);
        if (aiCity != null)
        {
            CityManager.Instance?.AdvanceSynodPlayerCities(playerId);
            CityGrowthManager.Instance?.ProcessSynodPlayerEndTurn(playerId);
        }

        ManageSynodCityProduction(playerId);
        TryAiEmbarkTroopsForSynod(playerId);

        var units = tm.GetSynodUnits(playerId);
        if (units.Count == 0)
        {
            MatchController.Instance?.EvaluateConditions();
            tm.EndTurn();
            return;
        }

        var enemyUnits = CollectSynodAiTargets(playerId, tm);
        var enemyCities = CollectSynodAiCityTargets(playerId);
        var cityThreat = aiCity != null ? FindNearestThreatToCity(aiCity, enemyUnits, map, 4) : null;
        var personality = SynodPlayerDatabase.PersonalityFor(playerId);
        int actionsUsed = 0;

        foreach (var unit in units.OrderBy(u => u.Health / (float)u.MaxHealth))
        {
            if (actionsUsed >= MaxUnitActionsPerTurn)
                break;
            if (NavalMovementRules.IsNavalUnit(unit.Type))
            {
                if (TryExecuteNavalBlockade(unit, enemyCities, enemyUnits, map))
                {
                    actionsUsed++;
                    continue;
                }
                if (TryAiAmphibiousDisembark(unit, enemyCities, map))
                {
                    actionsUsed++;
                    continue;
                }
            }

            if (ShouldRetreat(unit, enemyUnits, map, aiCity))
            {
                RetreatTowardCity(unit, aiCity, map);
                actionsUsed++;
                continue;
            }

            bool martial = IsMartialUnit(unit);
            if (cityThreat != null && martial &&
                map.WrappedDistance(unit.HexPosition, cityThreat.HexPosition) <= 5)
            {
                ExecuteUnitAttackPlan(unit, cityThreat, map);
                actionsUsed++;
                continue;
            }

            City targetCity = FindBestCityTarget(unit, enemyCities, map);
            Unit targetUnit = FindNearestEnemy(unit, enemyUnits, map);

            if (targetUnit != null &&
                map.WrappedDistance(unit.HexPosition, targetUnit.HexPosition) <=
                map.WrappedDistance(unit.HexPosition, targetCity?.HexPosition ?? unit.HexPosition) + 1)
            {
                ExecuteUnitAttackPlan(unit, targetUnit, map);
                actionsUsed++;
                continue;
            }

            if (targetCity != null && martial &&
                (personality.PreferSoldiers || !personality.PreferMissionaries))
            {
                TryMoveToward(unit, targetCity.HexPosition, map);
                CityManager.Instance?.TryCaptureCityAt(unit, unit.HexPosition);
                actionsUsed++;
                continue;
            }

            if (targetUnit != null)
                ExecuteUnitAttackPlan(unit, targetUnit, map);
            else if (targetCity != null && personality.PreferMissionaries && unit.Type == UnitType.Missionary)
                TryMoveToward(unit, targetCity.HexPosition, map);
            actionsUsed++;
        }

        MatchController.Instance?.EvaluateConditions();
        CityMilitia.ProcessSynodPlayerEndTurn(playerId);
        CityLoyaltySystem.ProcessEndTurnOccupation(FactionId.LutheranSynod);
        CityLoyaltySystem.ProcessEndTurnRecovery(FactionId.LutheranSynod, playerId);
        AiSynodCrisisManager.ProcessEndTurn(playerId);
        ScheduleFinishAiTurn(FactionId.LutheranSynod, playerId, SchismaticBlocId.None);
    }

    static List<Unit> CollectSynodAiTargets(SynodPlayerId self, TurnManager tm)
    {
        var list = new List<Unit>();
        foreach (var unit in tm.GetUnits(FactionId.LutheranSynod))
        {
            if (!unit.IsAlive || unit.SynodPlayer == self)
                continue;
            if (unit.SynodPlayer == SynodPlayerId.Player1 &&
                SynodDiplomacyManager.Instance != null &&
                !SynodDiplomacyManager.Instance.AreHostile(self, SynodPlayerId.Player1))
                continue;
            list.Add(unit);
        }

        foreach (var unit in tm.GetUnits(FactionId.Schismatic))
        {
            if (unit.IsAlive)
                list.Add(unit);
        }

        return list;
    }

    static List<City> CollectSynodAiCityTargets(SynodPlayerId self)
    {
        var list = new List<City>();
        if (CityManager.Instance == null)
            return list;

        foreach (var city in CityManager.Instance.AllCities)
        {
            if (city.Faction == FactionId.LutheranSynod && city.SynodPlayer == self)
                continue;
            if (city.Faction == FactionId.LutheranSynod &&
                city.SynodPlayer == SynodPlayerId.Player1 &&
                SynodDiplomacyManager.Instance != null &&
                !SynodDiplomacyManager.Instance.AreHostile(self, SynodPlayerId.Player1))
                continue;
            if (city.Faction == FactionId.LutheranSynod || city.Faction == FactionId.Schismatic)
                list.Add(city);
        }

        return list;
    }

    void ManageSynodCityProduction(SynodPlayerId playerId)
    {
        var aiCity = CityManager.Instance?.GetSynodPlayerCapital(playerId);
        if (aiCity?.Production == null ||
            (aiCity.Production.IsTimerBusy && aiCity.Production.IsProdBusy))
            return;

        var personality = SynodPlayerDatabase.PersonalityFor(playerId);
        ManageCityProduction(
            aiCity,
            personality.PreferMissionaries,
            personality.PreferSoldiers,
            personality.PreferRanged,
            personality.PreferScouts,
            personality.PreferSiege,
            type => CountSynodUnits(playerId, type));
    }

    static void ManageCityProduction(
        City aiCity,
        bool preferMissionaries,
        bool preferSoldiers,
        bool preferRanged,
        bool preferScouts,
        bool preferSiege,
        System.Func<UnitType, int> countUnits)
    {
        int soldiers = countUnits(UnitType.Soldier);
        int slingers = countUnits(UnitType.Slinger);
        int missionaries = countUnits(UnitType.Missionary);
        int scouts = countUnits(UnitType.Scout);
        int siegeEngines = countUnits(UnitType.SiegeEngine);
        int galleys = countUnits(UnitType.CoastalGalley);
        int explorers = countUnits(UnitType.CoastalExplorer);
        int deepSea = countUnits(UnitType.DeepSeaShip);
        bool coastal = CityManager.Instance != null && CityManager.Instance.CityTouchesNavalCoast(aiCity);
        City navalBase = FindCoastalNavalBase(aiCity);
        bool navalCoastal = navalBase != null;
        bool wharfTech = ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.CoastalWharves) == true;
        bool dockTech = ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.NavalWarfare) == true;
        bool oceanTech = ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.OpenOceanNavigation) == true;
        bool archerTech = ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.ShepherdsSling) == true;
        bool horsemanTech = ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.MartinChemnitz) == true;
        bool missionHouseTech = ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.MissionarySending) == true;
        bool slingTech = archerTech;
        bool siegeTech = ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.JamesClerkMaxwell) == true
            && ConfessionResearchManager.Instance?.IsTechUnlocked(ConfessionTechId.DavidChytraeus) == true;
        int archers = countUnits(UnitType.Archer);
        int horsemen = countUnits(UnitType.Horseman);
        City garrisonCity = FindGarrisonDistrictCity(aiCity);
        bool localArmory = garrisonCity?.Production?.HasBuilding(CityBuildId.BuildArmory) == true;

        if (coastal && wharfTech && !aiCity.Production.HasBuilding(CityBuildId.BuildWharf) &&
            TryAiStartAndFull(aiCity, CityBuildId.BuildWharf))
            return;

        if (coastal && wharfTech && aiCity.Production.HasBuilding(CityBuildId.BuildWharf) &&
            !aiCity.Production.HasBuilding(CityBuildId.BuildFishingPost) &&
            TryAiStartAndFull(aiCity, CityBuildId.BuildFishingPost))
            return;

        if (navalCoastal && dockTech && navalBase.Production.HasBuilding(CityBuildId.BuildWharf) &&
            !navalBase.Production.HasBuilding(CityBuildId.BuildDock) &&
            HamletSpecialtyDatabase.IsBuildAllowed(navalBase, CityBuildId.BuildDock) &&
            TryAiStartAndFull(navalBase, CityBuildId.BuildDock))
            return;

        if (navalCoastal && dockTech && navalBase.Production.HasBuilding(CityBuildId.BuildDock) && galleys < 1 &&
            HamletSpecialtyDatabase.IsBuildAllowed(navalBase, CityBuildId.TrainCoastalGalley) &&
            TryAiStartAndFull(navalBase, CityBuildId.TrainCoastalGalley))
            return;

        if (navalCoastal && oceanTech && navalBase.Production.HasBuilding(CityBuildId.BuildDock) && deepSea < 1 &&
            HamletSpecialtyDatabase.IsBuildAllowed(navalBase, CityBuildId.TrainDeepSeaShip) &&
            TryAiStartAndFull(navalBase, CityBuildId.TrainDeepSeaShip))
            return;

        if (coastal && wharfTech && aiCity.Production.HasBuilding(CityBuildId.BuildWharf) && explorers < 1 &&
            HamletSpecialtyDatabase.IsBuildAllowed(aiCity, CityBuildId.TrainCoastalExplorer) &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainCoastalExplorer))
            return;

        if (!aiCity.Production.HasBuilding(CityBuildId.BuildChapel) &&
            TryAiStartAndFull(aiCity, CityBuildId.BuildChapel))
            return;

        if (preferScouts && scouts < 2 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainScout))
            return;

        if (preferMissionaries && missionaries < 3 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainMissionary))
            return;

        if (preferMissionaries && missionHouseTech && !aiCity.Production.HasBuilding(CityBuildId.BuildMissionHouse) &&
            HamletSpecialtyDatabase.IsBuildAllowed(aiCity, CityBuildId.BuildMissionHouse) &&
            TryAiStartAndFull(aiCity, CityBuildId.BuildMissionHouse))
            return;

        if (preferRanged && archerTech && archers < 2 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainArcher))
            return;

        if (preferSoldiers && horsemanTech && horsemen < 1 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainHorseman))
            return;

        if (preferSiege && siegeTech && garrisonCity != null && localArmory && siegeEngines < 1 &&
            TryAiStartAndFull(garrisonCity, CityBuildId.TrainSiegeEngine))
            return;

        if (preferSiege && garrisonCity != null && !localArmory &&
            HamletSpecialtyDatabase.IsBuildAllowed(garrisonCity, CityBuildId.BuildArmory) &&
            TryAiStartAndFull(garrisonCity, CityBuildId.BuildArmory))
            return;

        if (preferRanged && slingTech && slingers < 3 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainSlinger))
            return;

        if (preferSoldiers && soldiers < 4 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainSoldier))
            return;

        if (!preferSoldiers && missionaries < 2 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainMissionary))
            return;

        if (slingTech && slingers < 2 && (preferRanged || !preferMissionaries) &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainSlinger))
            return;

        if (soldiers < 3 &&
            TryAiStartAndFull(aiCity, CityBuildId.TrainSoldier))
            return;

        if (!aiCity.Production.HasBuilding(CityBuildId.BuildScriptorium) &&
            TryAiStartAndFull(aiCity, CityBuildId.BuildScriptorium))
            return;

        if (preferMissionaries && Random.value < 0.55f)
            aiCity.Production.TryStartAiBuild(CityBuildId.TrainMissionary);
        else if (preferRanged && slingTech && Random.value < 0.45f)
            aiCity.Production.TryStartAiBuild(CityBuildId.TrainSlinger);
        else if (preferScouts && scouts < 3 && Random.value < 0.35f)
            aiCity.Production.TryStartAiBuild(CityBuildId.TrainScout);
        else
            aiCity.Production.TryStartAiBuild(CityBuildId.TrainSoldier);
    }

    /// <summary>Start a job; return true only when both tracks are now full (stop shopping).</summary>
    static bool TryAiStartAndFull(City aiCity, CityBuildId id)
    {
        if (!aiCity.Production.TryStartAiBuild(id))
            return false;
        return aiCity.Production.IsTimerBusy && aiCity.Production.IsProdBusy;
    }

    static City FindGarrisonDistrictCity(City reference)
    {
        if (reference == null || CityManager.Instance == null)
            return null;

        foreach (var city in CityManager.Instance.GetSynodPlayerCities(reference.SynodPlayer))
        {
            if (city.Specialty == HamletSpecialty.Garrison)
                return city;
        }

        return null;
    }

    static City FindCoastalNavalBase(City reference)
    {
        if (reference == null || CityManager.Instance == null)
            return null;

        var garrison = FindGarrisonDistrictCity(reference);
        if (garrison != null && CityManager.Instance.CityTouchesNavalCoast(garrison))
            return garrison;

        if (CityManager.Instance.CityTouchesNavalCoast(reference))
            return reference;

        foreach (var city in CityManager.Instance.GetSynodPlayerCities(reference.SynodPlayer))
        {
            if (CityManager.Instance.CityTouchesNavalCoast(city))
                return city;
        }

        return null;
    }

    static int CountSynodUnits(SynodPlayerId playerId, UnitType type) =>
        TurnManager.Instance == null
            ? 0
            : TurnManager.Instance.GetSynodUnits(playerId).Count(u => u.Type == type);

    static void TryAiEmbarkTroopsForSynod(SynodPlayerId playerId)
    {
        if (TurnManager.Instance == null || HexGridMap.Instance == null)
            return;

        foreach (var unit in TurnManager.Instance.GetSynodUnits(playerId))
        {
            if (!AmphibiousTransport.IsAmphibiousCargo(unit) || unit.MovementRemaining <= 0)
                continue;

            var galley = AmphibiousTransport.FindAdjacentGalley(unit);
            if (galley != null)
                AmphibiousTransport.TryEmbark(unit, galley);
        }
    }

    public void PlayTurn() => PlayTurn(TurnManager.Instance?.ActiveSchismaticBloc ?? SchismaticBlocId.Bloc1);

    public void PlayTurn(SchismaticBlocId blocId)
    {
        CancelInvoke(nameof(FinishAiTurn));

        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
        {
            TurnManager.Instance?.EndTurn();
            return;
        }

        var tm = TurnManager.Instance;
        var map = HexGridMap.Instance;
        var aiCity = CityManager.Instance?.GetAiCity(blocId);
        if (aiCity != null)
        {
            CityManager.Instance?.AdvanceBlocCity(blocId);
            CityGrowthManager.Instance?.ProcessBlocEndTurn(blocId);
        }

        ManageAiCityProduction(blocId);
        TryAiEmbarkTroops(blocId);

        var units = tm.GetBlocUnits(blocId);
        if (units.Count == 0)
        {
            MatchController.Instance?.EvaluateConditions();
            tm.EndTurn();
            return;
        }

        var playerUnits = tm.GetUnits(FactionId.LutheranSynod).Where(u => u.IsAlive).ToList();
        var playerCities = CityManager.Instance?.GetPlayerCities() ?? new List<City>();
        var cityThreat = aiCity != null ? FindNearestThreatToCity(aiCity, playerUnits, map, 4) : null;
        var profile = SchismaticBlocRegistry.Instance?.ProfileForBloc(blocId)
                      ?? HeresyDatabase.ProfileFor(HeresyType.DoctrinalDrift);
        int actionsUsed = 0;

        foreach (var unit in units.OrderBy(u => u.Health / (float)u.MaxHealth))
        {
            if (actionsUsed >= MaxUnitActionsPerTurn)
                break;
            if (NavalMovementRules.IsNavalUnit(unit.Type))
            {
                if (TryExecuteNavalBlockade(unit, playerCities, playerUnits, map))
                {
                    actionsUsed++;
                    continue;
                }
                if (TryAiAmphibiousDisembark(unit, playerCities, map))
                {
                    actionsUsed++;
                    continue;
                }
            }

            if (ShouldRetreat(unit, playerUnits, map, aiCity))
            {
                RetreatTowardCity(unit, aiCity, map);
                actionsUsed++;
                continue;
            }

            bool martial = IsMartialUnit(unit);
            if (cityThreat != null && martial &&
                map.WrappedDistance(unit.HexPosition, cityThreat.HexPosition) <= 5)
            {
                ExecuteUnitAttackPlan(unit, cityThreat, map);
                actionsUsed++;
                continue;
            }

            City targetCity = FindBestCityTarget(unit, playerCities, map);
            Unit targetUnit = FindNearestEnemy(unit, playerUnits, map);

            if (targetUnit != null &&
                map.WrappedDistance(unit.HexPosition, targetUnit.HexPosition) <=
                map.WrappedDistance(unit.HexPosition, targetCity?.HexPosition ?? unit.HexPosition) + 1)
            {
                ExecuteUnitAttackPlan(unit, targetUnit, map);
                actionsUsed++;
                continue;
            }

            if (targetCity != null && martial && (profile.PreferSoldiers || !profile.PreferMissionaries))
            {
                TryMoveToward(unit, targetCity.HexPosition, map);
                CityManager.Instance?.TryCaptureCityAt(unit, unit.HexPosition);
                actionsUsed++;
                continue;
            }

            if (targetUnit != null)
                ExecuteUnitAttackPlan(unit, targetUnit, map);
            else if (targetCity != null && profile.PreferMissionaries && unit.Type == UnitType.Missionary)
                TryMoveToward(unit, targetCity.HexPosition, map);
            actionsUsed++;
        }

        MatchController.Instance?.EvaluateConditions();
        CityMilitia.ProcessEndTurn(FactionId.Schismatic, blocId);
        CityLoyaltySystem.ProcessEndTurnOccupation(FactionId.Schismatic);
        CityLoyaltySystem.ProcessEndTurnRecovery(FactionId.Schismatic, SynodPlayerId.None, blocId);
        ScheduleFinishAiTurn(FactionId.Schismatic, SynodPlayerId.None, blocId);
    }

    void ManageAiCityProduction(SchismaticBlocId blocId)
    {
        var aiCity = CityManager.Instance?.GetAiCity(blocId);
        if (aiCity?.Production == null ||
            (aiCity.Production.IsTimerBusy && aiCity.Production.IsProdBusy))
            return;

        var profile = SchismaticBlocRegistry.Instance?.ProfileForBloc(blocId)
                      ?? HeresyDatabase.ProfileFor(HeresyType.DoctrinalDrift);

        ManageCityProduction(
            aiCity,
            profile.PreferMissionaries,
            profile.PreferSoldiers,
            profile.PreferRanged,
            preferScouts: false,
            preferSiege: profile.PreferSoldiers && profile.PreferRanged,
            type => CountBlocUnits(blocId, type));
    }

    static bool IsMartialUnit(Unit unit) =>
        unit.Type is UnitType.Soldier or UnitType.Defender or UnitType.Slinger or UnitType.Archer
            or UnitType.Horseman or UnitType.SiegeEngine or UnitType.CoastalGalley or UnitType.DeepSeaShip;

    static int CountBlocUnits(SchismaticBlocId blocId, UnitType type)
    {
        if (TurnManager.Instance == null) return 0;
        return TurnManager.Instance.GetBlocUnits(blocId).Count(u => u.Type == type);
    }

    static bool TryExecuteNavalBlockade(
        Unit unit,
        List<City> playerCities,
        List<Unit> playerUnits,
        HexGridMap map)
    {
        if (unit == null || map == null || playerCities.Count == 0)
            return false;

        var blockadeHex = FindBlockadeHex(unit, playerCities, map);
        if (!blockadeHex.HasValue)
            return false;

        if (unit.HexPosition == blockadeHex.Value)
        {
            foreach (var enemy in playerUnits)
            {
                if (!enemy.IsAlive)
                    continue;
                if (CombatSystem.AreInAttackRange(unit.HexPosition, enemy.HexPosition, unit) && !unit.HasAttacked)
                {
                    CombatSystem.Resolve(unit, enemy);
                    return true;
                }
            }
            return true;
        }

        TryMoveToward(unit, blockadeHex.Value, map);
        CityManager.Instance?.TryCaptureCityAt(unit, unit.HexPosition);
        return true;
    }

    static HexCoordinates? FindBlockadeHex(Unit unit, List<City> playerCities, HexGridMap map)
    {
        City bestCity = null;
        int bestScore = int.MaxValue;
        foreach (var city in playerCities)
        {
            int score = map.WrappedDistance(unit.HexPosition, city.HexPosition);
            if (city.IsCapital)
                score -= 2;
            if (score < bestScore)
            {
                bestScore = score;
                bestCity = city;
            }
        }

        if (bestCity == null)
            return null;

        HexCoordinates? bestHex = null;
        int bestHexScore = int.MaxValue;
        foreach (var neighbor in map.GetWrappedNeighbors(bestCity.HexPosition))
        {
            if (!map.TryGetTile(neighbor, out var tile))
                continue;
            if (!tile.IsNavigableWater || tile.Occupant != null)
                continue;
            if (!map.TryGetMovementCost(
                    unit.HexPosition, neighbor, unit.MovementRemaining, unit.Faction, unit.Type, out _))
                continue;

            int score = map.WrappedDistance(neighbor, bestCity.HexPosition);
            if (tile.Terrain == TerrainType.River)
                score -= 1;
            if (score < bestHexScore)
            {
                bestHexScore = score;
                bestHex = neighbor;
            }
        }

        return bestHex;
    }

    static void TryAiEmbarkTroops(SchismaticBlocId blocId)
    {
        if (TurnManager.Instance == null || HexGridMap.Instance == null)
            return;

        foreach (var unit in TurnManager.Instance.GetBlocUnits(blocId))
        {
            if (!AmphibiousTransport.IsAmphibiousCargo(unit) || unit.MovementRemaining <= 0)
                continue;

            var galley = AmphibiousTransport.FindAdjacentGalley(unit);
            if (galley != null)
                AmphibiousTransport.TryEmbark(unit, galley);
        }
    }

    static bool TryAiAmphibiousDisembark(Unit galley, List<City> playerCities, HexGridMap map)
    {
        if (!AmphibiousTransport.IsGalleyTransporter(galley) || galley.EmbarkedCount == 0)
            return false;

        var targets = AmphibiousTransport.GetDisembarkHexes(galley);
        if (targets.Count == 0)
            return false;

        HexCoordinates best = targets[0];
        int bestScore = int.MinValue;
        foreach (var city in playerCities)
        {
            foreach (var hex in targets)
            {
                int score = -map.WrappedDistance(hex, city.HexPosition);
                if (city.IsCapital) score += 4;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = hex;
                }
            }
        }

        return AmphibiousTransport.TryDisembark(galley, best);
    }

    static bool ShouldRetreat(Unit self, List<Unit> enemies, HexGridMap map, City home)
    {
        if (!IsMartialUnit(self) || home == null)
            return false;
        if (self.Health > self.MaxHealth * 0.45f)
            return false;

        foreach (var enemy in enemies)
        {
            if (map.WrappedDistance(self.HexPosition, enemy.HexPosition) <= 2)
                return true;
        }
        return false;
    }

    static void RetreatTowardCity(Unit unit, City city, HexGridMap map)
    {
        if (city == null) return;
        TryMoveToward(unit, city.HexPosition, map);
    }

    static City FindBestCityTarget(Unit self, List<City> playerCities, HexGridMap map)
    {
        if (playerCities.Count == 0 || map == null) return null;

        City best = null;
        int bestScore = int.MaxValue;
        foreach (var city in playerCities)
        {
            int dist = map.WrappedDistance(self.HexPosition, city.HexPosition);
            int score = dist + (city.IsCapital ? 0 : 2) + (city.Population < City.MediumPopulation ? 1 : 0);
            if (score < bestScore)
            {
                bestScore = score;
                best = city;
            }
        }
        return best;
    }

    static Unit FindNearestThreatToCity(City city, List<Unit> enemies, HexGridMap map, int range)
    {
        Unit nearest = null;
        int best = int.MaxValue;
        foreach (var enemy in enemies)
        {
            int d = map.WrappedDistance(city.HexPosition, enemy.HexPosition);
            if (d <= range && d < best)
            {
                best = d;
                nearest = enemy;
            }
        }
        return nearest;
    }

    static void ExecuteUnitAttackPlan(Unit unit, Unit target, HexGridMap map)
    {
        if (CombatSystem.AreInAttackRange(unit.HexPosition, target.HexPosition, unit))
        {
            if (!unit.HasAttacked)
                CombatSystem.Resolve(unit, target);
            return;
        }

        TryMoveToward(unit, target.HexPosition, map);
        CityManager.Instance?.TryCaptureCityAt(unit, unit.HexPosition);

        if (target.IsAlive &&
            CombatSystem.AreInAttackRange(unit.HexPosition, target.HexPosition, unit) &&
            !unit.HasAttacked)
        {
            CombatSystem.Resolve(unit, target);
        }
    }

    static bool TryMoveToward(Unit unit, HexCoordinates goal, HexGridMap map)
    {
        var path = map.GetReachableHexes(unit.HexPosition, unit.MovementRemaining, unit.Faction, unit.Type);
        HexCoordinates? best = null;
        int bestDist = int.MaxValue;

        foreach (var coords in path)
        {
            int dist = map.WrappedDistance(coords, goal);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = coords;
            }
        }

        if (!best.HasValue)
            return false;

        return unit.TryMoveTo(best.Value);
    }

    void ScheduleFinishAiTurn(FactionId faction, SynodPlayerId synodPlayer, SchismaticBlocId blocId)
    {
        CancelInvoke(nameof(FinishAiTurn));
        pendingFinishFaction = faction;
        pendingFinishSynod = synodPlayer;
        pendingFinishBloc = blocId;
        Invoke(nameof(FinishAiTurn), 0.4f);
    }

    void FinishAiTurn()
    {
        var tm = TurnManager.Instance;
        if (tm == null)
            return;

        // Stale Invoke after a skipped/nested AI turn must not end the player's turn
        // (that was advancing the counter by 2 and skipping unit refresh/phases).
        if (tm.IsPlayerTurn)
        {
            Debug.LogWarning("SimpleAI.FinishAiTurn ignored — player turn already active.");
            return;
        }

        if (tm.ActiveFaction != pendingFinishFaction)
        {
            Debug.LogWarning(
                $"SimpleAI.FinishAiTurn ignored — expected {pendingFinishFaction}, active {tm.ActiveFaction}.");
            return;
        }

        if (pendingFinishFaction == FactionId.LutheranSynod &&
            tm.ActiveSynodPlayer != pendingFinishSynod)
        {
            Debug.LogWarning("SimpleAI.FinishAiTurn ignored — synod slot mismatch.");
            return;
        }

        if (pendingFinishFaction == FactionId.Schismatic &&
            tm.ActiveSchismaticBloc != pendingFinishBloc)
        {
            Debug.LogWarning("SimpleAI.FinishAiTurn ignored — bloc slot mismatch.");
            return;
        }

        tm.EndTurn();
    }

    static Unit FindNearestEnemy(Unit self, List<Unit> enemies, HexGridMap map)
    {
        if (map == null) return null;

        Unit nearest = null;
        int best = int.MaxValue;
        foreach (var enemy in enemies)
        {
            int d = map.WrappedDistance(self.HexPosition, enemy.HexPosition);
            if (d < best)
            {
                best = d;
                nearest = enemy;
            }
        }
        return nearest;
    }
}
