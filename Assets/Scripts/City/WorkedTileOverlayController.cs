using UnityEngine;

/// <summary>City-screen toggle — emphasize worked vs unworked tiles in synod territory.</summary>
public class WorkedTileOverlayController : MonoBehaviour
{
    public static WorkedTileOverlayController Instance { get; private set; }

    public bool IsActive { get; private set; }
    public City FocusCity { get; private set; }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static City ResolveTerritoryCity(City city) =>
        city == null ? null : city.IsHamlet ? city.ControllingCity ?? city : city;

    public void Toggle(City city)
    {
        city = ResolveTerritoryCity(city);
        if (IsActive && FocusCity == city)
            SetActive(false);
        else
            SetActive(true, city);
    }

    public void SetActive(bool active, City city = null)
    {
        IsActive = active;
        FocusCity = active ? ResolveTerritoryCity(city) : null;

        if (IsActive && FocusCity == null && CityManager.Instance != null)
            FocusCity = CityManager.Instance.GetPrimaryPlayerCity();

        TerritoryManager.Instance?.RefreshTileVisuals();

        if (!IsActive)
        {
            var selected = TurnManager.Instance?.SelectedUnit;
            if (selected != null)
                HexSelectionController.Instance?.ShowReachableForUnit(selected);
        }

        TurnPhaseBanner.Instance?.Refresh(IsActive ? BuildBannerMessage() : null);
        Debug.Log(IsActive
            ? $"Worked-tile overlay ON  -  gold = worked, dim = unworked ({FocusCity?.CityName ?? "city"})."
            : "Worked-tile overlay OFF.");
    }

    public bool IsOverlayHex(HexCoordinates hex)
    {
        if (!IsActive || FocusCity == null || TerritoryManager.Instance == null ||
            HexGridMap.Instance == null)
            return false;

        var wrapped = HexGridMap.Instance.Wrap(hex);
        return TerritoryManager.Instance.GetOwner(wrapped) == FocusCity;
    }

    string BuildBannerMessage()
    {
        if (FocusCity == null || TerritoryManager.Instance == null)
            return "Worked tiles highlighted (city panel toggle).";

        int worked = TerritoryManager.Instance.GetWorkedTiles(FocusCity).Count;
        int cap = TerritoryManager.Instance.GetWorkedTileCap(FocusCity);
        return $"Worked tiles: {worked}/{cap}  -  gold border = worked, dim = unworked territory";
    }
}
