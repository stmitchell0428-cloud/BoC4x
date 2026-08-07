using System.Collections.Generic;

using UnityEngine;



/// <summary>End-of-turn migration and organic district offers.</summary>

public class CityGrowthManager : MonoBehaviour

{

    public static CityGrowthManager Instance { get; private set; }



    readonly Dictionary<City, int> foodSurplusStreak = new();

    readonly Dictionary<City, int> districtOfferCooldown = new();



    CityGrowthSystem.DistrictSiteOffer? pendingOffer;



    public bool HasPendingOffer => pendingOffer.HasValue;



    void Awake() => Instance = this;



    void OnDestroy()

    {

        if (Instance == this)

            Instance = null;

    }



    public void ProcessPlayerEndTurn() => ProcessFactionEndTurn(FactionId.LutheranSynod, offerDistricts: true);

    public void ProcessGrowthFoodPhase(FactionId faction)
    {
        if (CityManager.Instance == null)
            return;

        foreach (var city in CityManager.Instance.GetCitiesForFaction(faction))
        {
            if (city.IsHamlet)
                continue;

            var snap = CityGrowthSystem.Evaluate(city);
            UpdateSurplusStreak(city, snap);

            if (snap.FoodSurplus < 0)
                CityGrowthSystem.ApplyHybridFoodDeficit(city, snap);
        }
    }

    public void ProcessMigrationPhase(FactionId faction, bool offerDistricts = false)
    {
        if (CityManager.Instance == null)
            return;

        int totalMigration = 0;
        foreach (var city in CityManager.Instance.GetCitiesForFaction(faction))
        {
            if (city.IsHamlet)
                continue;

            var snap = CityGrowthSystem.Evaluate(city);
            int gain = CityGrowthSystem.ProcessMigration(city, snap);
            if (gain > 0)
                Debug.Log($"{city.CityName} ({faction}): +{gain} settlers (appeal {snap.BlendedAppeal:F0}).");
            totalMigration += gain;
        }

        if (faction == FactionId.LutheranSynod && totalMigration > 0)
        {
            FirstSteps.Instance?.RefreshDashboard();
            TerrainInfoPanel.Instance?.RefreshCityYield();
        }

        if (offerDistricts && faction == FactionId.LutheranSynod)
        {
            TickCooldowns();
            EnsurePendingDistrictOfferVisible();
            if (!HasPendingOffer && DistrictOfferPanel.Instance != null && !DistrictOfferPanel.Instance.IsVisible)
                TryQueueDistrictOffer();
        }
    }



    public void ProcessFactionEndTurn(FactionId faction, bool offerDistricts = false)

    {

        if (CityManager.Instance == null)

            return;



        int totalMigration = 0;

        foreach (var city in CityManager.Instance.GetCitiesForFaction(faction))

        {

            if (city.IsHamlet)

                continue;



            var snap = CityGrowthSystem.Evaluate(city);

            UpdateSurplusStreak(city, snap);



            if (snap.FoodSurplus < 0)

                CityGrowthSystem.ApplyHybridFoodDeficit(city, snap);



            int gain = CityGrowthSystem.ProcessMigration(city, snap);

            if (gain > 0)

                Debug.Log($"{city.CityName} ({faction}): +{gain} settlers (food {snap.FoodSurplus:+0;-#}, appeal {snap.BlendedAppeal:F0}, {snap.TensionLabel}).");

            totalMigration += gain;

        }



        if (faction == FactionId.LutheranSynod && totalMigration > 0)

        {

            FirstSteps.Instance?.RefreshDashboard();

            TerrainInfoPanel.Instance?.RefreshCityYield();

        }



        if (offerDistricts && faction == FactionId.LutheranSynod)
        {
            TickCooldowns();
            EnsurePendingDistrictOfferVisible();
            if (!HasPendingOffer && DistrictOfferPanel.Instance != null && !DistrictOfferPanel.Instance.IsVisible)
                TryQueueDistrictOffer();
        }
    }

    public void ProcessSynodPlayerEndTurn(SynodPlayerId playerId)
    {
        if (CityManager.Instance == null)
            return;

        foreach (var city in CityManager.Instance.GetSynodPlayerCities(playerId))
        {
            if (city.IsHamlet)
                continue;

            var snap = CityGrowthSystem.Evaluate(city);
            UpdateSurplusStreak(city, snap);

            if (snap.FoodSurplus < 0)
                CityGrowthSystem.ApplyHybridFoodDeficit(city, snap);

            int gain = CityGrowthSystem.ProcessMigration(city, snap);
            if (gain > 0)
                Debug.Log($"{city.CityName} ({SynodPlayerDatabase.DisplayName(playerId)}): +{gain} settlers.");
        }
    }

    public void ProcessBlocEndTurn(SchismaticBlocId blocId)
    {
        var city = CityManager.Instance?.GetAiCity(blocId);
        if (city == null || city.IsHamlet)
            return;

        var snap = CityGrowthSystem.Evaluate(city);
        UpdateSurplusStreak(city, snap);

        if (snap.FoodSurplus < 0)
            CityGrowthSystem.ApplyHybridFoodDeficit(city, snap);

        int gain = CityGrowthSystem.ProcessMigration(city, snap);
        if (gain > 0)
            Debug.Log($"{city.CityName} ({blocId}): +{gain} settlers (appeal {snap.BlendedAppeal:F0}, {snap.TensionLabel}).");
    }



    void UpdateSurplusStreak(City city, CityGrowthSystem.GrowthSnapshot snap)
    {
        if (CityGrowthSystem.MeetsDistrictStreakCondition(snap))
            foodSurplusStreak[city] = foodSurplusStreak.GetValueOrDefault(city) + 1;
        else
            foodSurplusStreak[city] = 0;
    }



    public int GetSurplusStreak(City city) =>

        foodSurplusStreak.GetValueOrDefault(city);



    public void EnsurePendingDistrictOfferVisible()
    {
        if (!pendingOffer.HasValue)
            return;

        var panel = DistrictOfferPanel.Instance;
        if (panel == null)
        {
            pendingOffer = null;
            return;
        }

        if (panel.IsVisible)
            return;

        panel.Show(pendingOffer.Value);
        if (!panel.IsVisible)
        {
            Debug.LogWarning("Pending district offer could not be shown — clearing.");
            pendingOffer = null;
        }
    }

    void TryQueueDistrictOffer()

    {

        if (CityManager.Instance == null)

            return;



        CityGrowthSystem.DistrictSiteOffer? best = null;

        float bestScore = 0f;



        foreach (var city in CityManager.Instance.GetPlayerCities())

        {

            if (city.IsHamlet)

                continue;

            if (districtOfferCooldown.GetValueOrDefault(city) > 0)

                continue;



            var offer = CityGrowthSystem.FindBestDistrictOffer(city, GetSurplusStreak(city));

            if (!offer.HasValue || offer.Value.SiteScore <= bestScore)

                continue;



            bestScore = offer.Value.SiteScore;

            best = offer;

        }



        if (!best.HasValue)
            return;

        pendingOffer = best;
        var panel = DistrictOfferPanel.Instance;
        if (panel == null)
        {
            Debug.LogWarning("District offer queued but DistrictOfferPanel missing.");
            pendingOffer = null;
            return;
        }

        panel.Show(best.Value);
        if (!panel.IsVisible)
        {
            Debug.LogWarning("DistrictOfferPanel.Show failed — clearing pending offer so it can retry next turn.");
            pendingOffer = null;
        }
    }

    public int GetDistrictOfferCooldown(City city) =>
        city == null ? 0 : districtOfferCooldown.GetValueOrDefault(city);



    public void AcceptPendingOffer()

    {

        if (!pendingOffer.HasValue || CityManager.Instance == null)

            return;



        var offer = pendingOffer.Value;

        pendingOffer = null;



        if (CityManager.Instance.TryFoundOrganicDistrict(offer.Parent, offer.Hex, offer.SuggestedSpecialty))

            districtOfferCooldown[offer.Parent] = 0;

        else

            districtOfferCooldown[offer.Parent] = 2;

    }



    public void DeclinePendingOffer()

    {

        if (!pendingOffer.HasValue)

            return;



        districtOfferCooldown[pendingOffer.Value.Parent] = 5;

        pendingOffer = null;

        DistrictOfferPanel.Instance?.Hide();

    }



    public void DeferPendingOffer()

    {

        if (!pendingOffer.HasValue)

            return;



        districtOfferCooldown[pendingOffer.Value.Parent] = 3;

        pendingOffer = null;

        DistrictOfferPanel.Instance?.Hide();

    }



    public void ClearPendingOffer() => pendingOffer = null;



    public void TickCooldowns()

    {

        var keys = new List<City>(districtOfferCooldown.Keys);

        foreach (var city in keys)

        {

            int v = districtOfferCooldown[city];

            if (v > 0)

                districtOfferCooldown[city] = v - 1;

        }

    }

}


