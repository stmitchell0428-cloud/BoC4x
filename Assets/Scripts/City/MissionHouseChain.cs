using UnityEngine;

/// <summary>Mission house unlocks the frontier colonist chain across a city cluster.</summary>
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

    public static bool CanTrainColonist(City city) =>
        city != null &&
        city.Faction == FactionId.LutheranSynod &&
        ClusterHasMissionHouse(city);

    public static int ColonistManuscriptDiscount(City city) =>
        CityHasMissionHouse(city) ? 1 : 0;

    public static int ColonistTurnReduction(City city) =>
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
