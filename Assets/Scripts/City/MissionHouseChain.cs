using UnityEngine;

/// <summary>Mission house unlocks a frontier settler for a second independent city (Decision 2).</summary>
public static class MissionHouseChain
{
    public static bool CityHasMissionHouse(City city) =>
        city?.Production?.HasBuilding(CityBuildId.BuildMissionHouse) == true;

    public static bool ClusterHasMissionHouse(City city)
    {
        if (city == null)
            return false;

        var root = ClergyRoster.GetControllingRoot(city);
        if (CityHasMissionHouse(root))
            return true;

        if (CityManager.Instance == null)
            return false;

        foreach (var member in CityManager.Instance.GetPlayerCities())
        {
            if (ClergyRoster.GetControllingRoot(member) != root)
                continue;
            if (CityHasMissionHouse(member))
                return true;
        }

        return false;
    }

    public static int CountIndependentSynodCities(SynodPlayerId playerId = SynodPlayerId.Player1)
    {
        if (CityManager.Instance == null)
            return 0;

        int count = 0;
        foreach (var city in CityManager.Instance.GetSynodPlayerCities(playerId))
        {
            if (city.IsIndependentCity)
                count++;
        }

        return count;
    }

    public static bool HasFrontierSettlerInField(SynodPlayerId playerId = SynodPlayerId.Player1)
    {
        if (TurnManager.Instance == null)
            return false;

        foreach (var unit in TurnManager.Instance.GetSynodUnits(playerId))
        {
            if (unit.IsAlive && unit.IsFrontierSettler)
                return true;
        }

        return false;
    }

    public static bool CanTrainFrontierSettler(City city) =>
        city != null &&
        city.Faction == FactionId.LutheranSynod &&
        city.SynodPlayer == SynodPlayerId.Player1 &&
        ClusterHasMissionHouse(city) &&
        CountIndependentSynodCities() == 1 &&
        !HasFrontierSettlerInField();

    public static int SettlerManuscriptDiscount(City city) =>
        CityHasMissionHouse(city) ? 1 : 0;

    public static int SettlerTurnReduction(City city) =>
        CityHasMissionHouse(city) ? 1 : 0;

    public static int MissionaryManuscriptDiscount(City city) =>
        CityHasMissionHouse(city) ? 1 : 0;

    public static int MissionaryTurnReduction(City city) =>
        CityHasMissionHouse(city) ? 1 : 0;

    public static int EffectiveMissionaryCost(City city)
    {
        var def = CityBuildDatabase.Get(CityBuildId.TrainMissionary);
        return Mathf.Max(1, def.ManuscriptCost - MissionaryManuscriptDiscount(city));
    }

    public static int EffectiveFrontierSettlerCost(City city)
    {
        var def = CityBuildDatabase.Get(CityBuildId.TrainFrontierSettler);
        return Mathf.Max(1, def.ManuscriptCost - SettlerManuscriptDiscount(city));
    }

    /// <summary>+1 fame per turn from each completed mission house.</summary>
    public static void ProcessEndTurnFame()
    {
        var faction = FirstSteps.Instance;
        if (faction == null || CityManager.Instance == null)
            return;

        int houses = 0;
        foreach (var city in CityManager.Instance.GetPlayerCities())
        {
            if (CityHasMissionHouse(city))
                houses++;
        }

        if (houses > 0)
        {
            faction.AddFame(houses);
            Debug.Log($"Mission houses: +{houses} fame from frontier witness.");
        }
    }
}
