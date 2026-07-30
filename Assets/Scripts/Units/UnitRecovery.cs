using UnityEngine;

/// <summary>Parish hospitality: units recover HP when ending the turn on a synod city or district.</summary>
public static class UnitRecovery
{
    public const int CityHexHeal = 4;

    public static void ProcessPlayerEndTurn()
    {
        if (TurnManager.Instance == null || HexGridMap.Instance == null)
            return;

        foreach (var unit in TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1))
        {
            if (unit == null || !unit.IsAlive || !unit.IsOnMap)
                continue;
            if (unit.Health >= unit.MaxHealth)
                continue;
            if (!IsOnOwnSettlement(unit))
                continue;

            int before = unit.Health;
            unit.Heal(CityHexHeal);
            if (unit.Health > before)
            {
                Debug.Log(
                    $"{Unit.TypeDisplayName(unit.Type)} rested in parish care " +
                    $"(+{unit.Health - before} HP → {unit.Health}/{unit.MaxHealth}).");
            }
        }

        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        TerrainInfoPanel.Instance?.RefreshSelection();
    }

    static bool IsOnOwnSettlement(Unit unit)
    {
        if (unit == null || HexGridMap.Instance == null)
            return false;
        if (!HexGridMap.Instance.TryGetTile(unit.HexPosition, out var tile) || tile.Settlement == null)
            return false;

        var settlement = tile.Settlement;
        return settlement.Faction == FactionId.LutheranSynod &&
               settlement.SynodPlayer == SynodPlayerId.Player1;
    }
}
