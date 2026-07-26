using UnityEngine;

/// <summary>Faction population is derived from city totals (Decision 7).</summary>
public static class PopulationSync
{
    public static int SumSynodPopulation(SynodPlayerId playerId = SynodPlayerId.Player1)
    {
        if (CityManager.Instance == null)
            return 0;

        int sum = 0;
        foreach (var city in CityManager.Instance.GetSynodPlayerCities(playerId))
            sum += city.Population;

        return sum;
    }

    public static void SyncPlayerFactionFromCities()
    {
        var faction = FirstSteps.Instance;
        if (faction == null || CityManager.Instance == null)
            return;

        // Nomadic start: no cities yet — keep wandering-band population until Wittenberg is founded.
        if (CityManager.Instance.GetSynodPlayerCities(SynodPlayerId.Player1).Count == 0)
            return;

        faction.population = SumSynodPopulation(SynodPlayerId.Player1);
    }

    public static void ApplyDeltaToPrimaryCity(int delta)
    {
        if (delta == 0)
            return;

        var city = CityManager.Instance?.GetPrimaryPlayerCity();
        if (city == null)
        {
            if (FirstSteps.Instance != null)
                FirstSteps.Instance.population = Mathf.Max(0, FirstSteps.Instance.population + delta);
            return;
        }

        city.Population = Mathf.Max(0, city.Population + delta);
        city.RefreshAppearance();
        SyncPlayerFactionFromCities();
    }

    public static void ApplyLossAcrossPlayerCities(int loss)
    {
        if (loss <= 0 || CityManager.Instance == null)
            return;

        int remaining = loss;
        foreach (var city in CityManager.Instance.GetSynodPlayerCities(SynodPlayerId.Player1))
        {
            if (remaining <= 0)
                break;

            int floor = city.IsIndependentCity && city.IsCapital ? 5 : 2;
            int take = Mathf.Min(remaining, Mathf.Max(0, city.Population - floor));
            if (take <= 0)
                continue;

            city.Population -= take;
            city.RefreshAppearance();
            remaining -= take;
        }

        SyncPlayerFactionFromCities();
    }
}
