using UnityEngine;

/// <summary>Toggle G  -  highlight appeal hotspots in synod territory.</summary>
public class AppealOverlayController : MonoBehaviour
{
    public static AppealOverlayController Instance { get; private set; }

    public bool IsActive { get; private set; }

    const float ExcellentThreshold = 28f;
    const float GoodThreshold = 16f;

    int lastExcellentCount;
    int lastGoodCount;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void FlashDistrictSite(HexCoordinates hex, float score)
    {
        if (HexGridMap.Instance == null || !HexGridMap.Instance.TryGetTile(hex, out var tile))
            return;

        var kind = score >= ExcellentThreshold
            ? HighlightKind.AppealExcellent
            : score >= GoodThreshold
                ? HighlightKind.AppealGood
                : HighlightKind.None;

        if (kind != HighlightKind.None)
            tile.SetHighlight(kind);
        TurnPhaseBanner.Instance?.Refresh(
            $"<color=#DDCC88><b>District site appeal</b></color>  -  " +
            $"{(score >= ExcellentThreshold ? "excellent" : score >= GoodThreshold ? "good" : "fair")} ({score:F0})  |  " +
            "<color=#AABBCC>G toggles appeal map</color>");
    }

    public void Toggle()
    {
        IsActive = !IsActive;
        if (IsActive)
            Refresh();
        else
            HexSelectionController.Instance?.ClearHighlights();

        var selected = TurnManager.Instance?.SelectedUnit;
        if (!IsActive && selected != null)
            HexSelectionController.Instance?.ShowReachableForUnit(selected);

        TurnPhaseBanner.Instance?.Refresh(IsActive ? BuildBannerMessage() : null);

        Debug.Log(IsActive
            ? "Appeal overlay ON  -  gold = best district sites, lavender = good (G to hide)."
            : "Appeal overlay OFF.");
    }

    public void Refresh()
    {
        if (!IsActive || HexGridMap.Instance == null || CityManager.Instance == null)
            return;

        HexSelectionController.Instance?.ClearHighlights();

        lastExcellentCount = 0;
        lastGoodCount = 0;

        foreach (var city in CityManager.Instance.GetPlayerCities())
        {
            if (city.IsHamlet)
                continue;

            var snap = CityGrowthSystem.Evaluate(city);
            if (TerritoryManager.Instance == null)
                continue;

            foreach (var hex in TerritoryManager.Instance.GetTerritory(city))
            {
                if (!HexGridMap.Instance.TryGetTile(hex, out var tile))
                    continue;
                if (tile.Occupant != null && tile.Settlement == null)
                    continue;

                float score = CityGrowthSystem.ScoreLocalAppealHex(city, hex, snap);
                if (score >= ExcellentThreshold)
                {
                    tile.SetHighlight(HighlightKind.AppealExcellent);
                    lastExcellentCount++;
                }
                else if (score >= GoodThreshold)
                {
                    tile.SetHighlight(HighlightKind.AppealGood);
                    lastGoodCount++;
                }
            }
        }

        TurnPhaseBanner.Instance?.Refresh(BuildBannerMessage());
    }

    string BuildBannerMessage()
    {
        if (lastExcellentCount + lastGoodCount == 0)
        {
            return "<color=#DDCC88><b>Appeal map (G)</b></color>  -  " +
                   "no strong sites yet; expand borders and raise food surplus first";
        }

        return "<color=#DDCC88><b>Appeal map (G)</b></color>  -  " +
               $"<color=#EECC55>{lastExcellentCount} excellent</color>, " +
               $"<color=#AA88DD>{lastGoodCount} good</color> district hexes in synod territory";
    }
}
