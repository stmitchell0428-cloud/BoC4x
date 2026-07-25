using UnityEngine;

public static class UnitUpgradeService
{
    public static UnitUpgradeStatus GetStatus(Unit unit, UnitUpgradeId id, City city = null)
    {
        if (unit == null || !unit.IsAlive || unit.Faction != FactionId.LutheranSynod)
            return UnitUpgradeStatus.WrongUnit;

        var def = UnitUpgradeDatabase.Get(id);
        if (unit.Type != def.FromType)
            return UnitUpgradeStatus.WrongUnit;

        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
            return UnitUpgradeStatus.Locked;

        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return UnitUpgradeStatus.Locked;

        if (city == null)
            city = CityManager.Instance?.GetCityForUnit(unit);

        if (city == null || city.Faction != FactionId.LutheranSynod ||
            !CityManager.Instance.IsUnitOnCity(unit, city))
            return UnitUpgradeStatus.NotOnCity;

        if (!HamletSpecialtyDatabase.IsUpgradeAllowed(city, id))
            return UnitUpgradeStatus.Locked;

        if (def.ToType is UnitType.Cantor && !ClergyRoster.HasSeminaryAccess(city))
            return UnitUpgradeStatus.Locked;

        if (def.ToType == UnitType.Chaplain && !ClergyRoster.HasSeminaryAccess(city))
            return UnitUpgradeStatus.Locked;

        if (def.ToType == UnitType.Bishop && !ClergyRoster.HasSeminaryAccess(city))
            return UnitUpgradeStatus.Locked;

        if (def.Id == UnitUpgradeId.BishopToArchbishop)
        {
            if (ClergyRoster.CountIndependentSynodCities(FactionId.LutheranSynod) < 2)
                return UnitUpgradeStatus.Locked;
            var root = ClergyRoster.GetControllingRoot(city);
            if (root == null || city != root || city.IsHamlet)
                return UnitUpgradeStatus.NotOnCity;
        }

        if (def.ToType == UnitType.Bishop &&
            !ClergyRoster.CanUpgradeToClergy(city, UnitType.Bishop, def.FromType))
            return UnitUpgradeStatus.ClergySlotsFull;

        if (def.ToType == UnitType.Archbishop &&
            !ClergyRoster.CanUpgradeToClergy(city, UnitType.Archbishop, def.FromType))
            return UnitUpgradeStatus.ClergySlotsFull;

        if (def.ToType == UnitType.Pastor &&
            !ClergyRoster.CanUpgradeToClergy(city, UnitType.Pastor, def.FromType))
            return UnitUpgradeStatus.ClergySlotsFull;

        if (def.ToType == UnitType.Chaplain &&
            !ClergyRoster.CanUpgradeToClergy(city, UnitType.Chaplain, def.FromType))
            return UnitUpgradeStatus.ClergySlotsFull;

        if (ConfessionResearchManager.Instance == null ||
            !ConfessionResearchManager.Instance.IsTechUnlocked(def.RequiredTech))
            return UnitUpgradeStatus.Locked;

        var faction = FirstSteps.Instance;
        if (faction == null || faction.ScriptureManuscripts < def.ManuscriptCost)
            return UnitUpgradeStatus.InsufficientManuscripts;

        if (ClergyRoster.IsClergyUnit(def.ToType) && city != null &&
            !ClergyRoster.CanUpgradeToClergy(city, def.ToType, def.FromType))
            return UnitUpgradeStatus.ClergySlotsFull;

        return UnitUpgradeStatus.Available;
    }

    public static bool TryUpgrade(Unit unit, UnitUpgradeId id)
    {
        var status = GetStatus(unit, id);
        if (status != UnitUpgradeStatus.Available)
        {
            Debug.LogWarning($"Upgrade failed ({id}): {status}");
            return false;
        }

        var def = UnitUpgradeDatabase.Get(id);
        var faction = FirstSteps.Instance;
        faction.ScriptureManuscripts -= def.ManuscriptCost;

        unit.ReconfigureAs(def.ToType, consumeTurn: true);
        if (ClergyRoster.IsClergyUnit(def.ToType))
        {
            var city = CityManager.Instance?.GetCityForUnit(unit);
            if (city != null)
                ClergyRoster.RegisterUnit(unit, city);
            if (def.ToType == UnitType.Chaplain)
                unit.SetChaplainAssignment(ChaplainAssignment.Parish, null);
        }
        ConfessionResearchManager.Instance?.ApplyBonusesToAllPlayerUnits();

        Debug.Log($"{def.FromType} upgraded to {def.ToType}: {def.Name}");
        faction.RefreshDashboard();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        PlayerUnitCycle.Instance?.OnUnitOrdersChanged();
        TurnPhaseBanner.Instance?.Refresh($"Upgraded: {Unit.TypeDisplayName(def.ToType)}");
        return true;
    }

    public static bool SelectedUnitCanUpgrade(out UnitUpgradeId? firstAvailable)
    {
        firstAvailable = null;
        var unit = TurnManager.Instance?.SelectedUnit;
        if (unit == null) return false;

        foreach (var def in UnitUpgradeDatabase.All)
        {
            if (GetStatus(unit, def.Id) == UnitUpgradeStatus.Available)
            {
                firstAvailable = def.Id;
                return true;
            }
        }
        return false;
    }
}
