using UnityEngine;

/// <summary>Toggle G  -  highlight appeal hotspots in synod territory.</summary>
public class AppealOverlayController : MonoBehaviour
{
    public static AppealOverlayController Instance { get; private set; }

    public bool IsActive { get; private set; }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        Debug.Log(IsActive
            ? "Appeal overlay ON  -  gold/lavender hexes show district/growth potential (G to hide)."
            : "Appeal overlay OFF.");
    }

    public void Refresh()
    {
        if (!IsActive || HexGridMap.Instance == null || CityManager.Instance == null)
            return;

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
                if (score >= 42f)
                    tile.SetHighlight(HighlightKind.AppealExcellent);
                else if (score >= 28f)
                    tile.SetHighlight(HighlightKind.AppealGood);
            }
        }
    }
}
