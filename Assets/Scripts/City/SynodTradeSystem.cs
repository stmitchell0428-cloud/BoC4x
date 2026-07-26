using System.Collections.Generic;
using UnityEngine;

/// <summary>Synod-cluster trade links between market districts and coastal hubs (+1 mss/link/turn).</summary>
public static class SynodTradeSystem
{
    public const int MaxLinkDistance = 4;

    public static bool IsTradeHub(City city)
    {
        if (city == null || city.Production == null)
            return false;

        bool marketHall = city.Production.HasBuilding(CityBuildId.BuildMarketHall);
        bool dock = city.Production.HasBuilding(CityBuildId.BuildDock);

        if (city.IsHamlet && city.Specialty == HamletSpecialty.Market && (marketHall || dock))
            return true;

        if (!city.IsHamlet)
        {
            if (dock)
                return true;
            if (marketHall && CityManager.Instance != null && CityManager.Instance.CityTouchesNavalCoast(city))
                return true;
        }

        return false;
    }

    public static int CountTradeLinks(SynodPlayerId playerId)
    {
        if (CityManager.Instance == null || HexGridMap.Instance == null)
            return 0;

        var hubsByRoot = new Dictionary<City, List<City>>();
        foreach (var city in CityManager.Instance.AllCities)
        {
            if (city.Faction != FactionId.LutheranSynod || city.SynodPlayer != playerId)
                continue;
            if (!IsTradeHub(city))
                continue;

            var root = city.ControllingCity;
            if (!hubsByRoot.TryGetValue(root, out var list))
            {
                list = new List<City>();
                hubsByRoot[root] = list;
            }
            list.Add(city);
        }

        int links = 0;
        foreach (var hubs in hubsByRoot.Values)
        {
            for (int i = 0; i < hubs.Count; i++)
            {
                for (int j = i + 1; j < hubs.Count; j++)
                {
                    int dist = HexGridMap.Instance.WrappedDistance(hubs[i].HexPosition, hubs[j].HexPosition);
                    if (dist <= MaxLinkDistance)
                        links++;
                }
            }
        }

        return links;
    }

    public static void ProcessEndTurn(SynodPlayerId playerId)
    {
        if (playerId != SynodPlayerId.Player1)
            return;

        int links = CountTradeLinks(playerId);
        if (links <= 0 || FirstSteps.Instance == null)
            return;

        FirstSteps.Instance.ScriptureManuscripts += links;
        Debug.Log($"Synod trade: +{links} manuscripts from {links} market/coastal link(s).");
    }

    public static string FormatNetworkSummary(SynodPlayerId playerId)
    {
        int links = CountTradeLinks(playerId);
        if (links <= 0)
            return "";

        return $"<color=#AADDCC><b>Trade network</b></color>  {links} link(s)  -  +{links} mss/turn";
    }
}
