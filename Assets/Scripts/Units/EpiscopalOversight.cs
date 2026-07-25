using UnityEngine;

/// <summary>Passive bonuses from bishops (city cluster) and archbishops (synod-wide).</summary>
public static class EpiscopalOversight
{
    public const float BishopClusterPreachBonus = 1f;
    public const float ArchbishopSynodPreachBonus = 1f;
    public const float ArchbishopBishopAmplifier = 2f;
    public const int BishopClusterSiegeBonus = 2;
    public const int ArchbishopSiegeBonus = 1;
    public const float BishopClusterComfortPerTurn = 2f;
    public const float ArchbishopComfortPerTurn = 3f;
    public const float ArchbishopAdherencePerTurn = 1f;
    public const float BishopCantorComfortBonus = 2f;

    public static bool ReceivesBishopOversight(UnitType type) =>
        type is UnitType.Pastor or UnitType.Chaplain or UnitType.Deaconess or UnitType.Cantor;

    public static Unit FindBishopForCluster(City root)
    {
        if (root == null || TurnManager.Instance == null)
            return null;

        foreach (var unit in TurnManager.Instance.GetUnits(root.Faction))
        {
            if (!unit.IsAlive || unit.Type != UnitType.Bishop)
                continue;
            if (ClergyRoster.GetControllingRoot(ClergyRoster.GetAssignedCity(unit)) == root)
                return unit;
        }

        return null;
    }

    public static Unit FindArchbishop(FactionId faction)
    {
        if (TurnManager.Instance == null || faction == FactionId.None)
            return null;

        foreach (var unit in TurnManager.Instance.GetUnits(faction))
        {
            if (unit.IsAlive && unit.Type == UnitType.Archbishop)
                return unit;
        }

        return null;
    }

    static City GetClusterRoot(Unit unit)
    {
        if (unit?.RosterCity != null)
            return ClergyRoster.GetControllingRoot(unit.RosterCity);
        var near = CityManager.Instance?.GetNearestPlayerCity(unit.HexPosition);
        return near != null ? ClergyRoster.GetControllingRoot(near) : null;
    }

    static bool BishopIsPresent(Unit bishop, City root)
    {
        if (bishop == null || root == null || HexGridMap.Instance == null)
            return false;
        return HexGridMap.Instance.WrappedDistance(bishop.HexPosition, root.HexPosition) <= 2;
    }

    public static float GetPassivePreachBonus(Unit preacher)
    {
        if (preacher == null || !preacher.IsAlive || preacher.Faction != FactionId.LutheranSynod)
            return 0f;

        float bonus = 0f;
        if (FindArchbishop(preacher.Faction) != null)
            bonus += ArchbishopSynodPreachBonus;

        if (!ReceivesBishopOversight(preacher.Type))
            return bonus;

        var root = GetClusterRoot(preacher);
        var bishop = FindBishopForCluster(root);
        if (bishop == null || !BishopIsPresent(bishop, root))
            return bonus;

        float clusterBonus = FindArchbishop(preacher.Faction) != null
            ? ArchbishopBishopAmplifier
            : BishopClusterPreachBonus;
        return bonus + clusterBonus;
    }

    public static float GetPassiveHymnComfortBonus(Unit cantor)
    {
        if (cantor == null || cantor.Type != UnitType.Cantor)
            return 0f;

        var root = GetClusterRoot(cantor);
        var bishop = FindBishopForCluster(root);
        if (bishop == null || !BishopIsPresent(bishop, root))
            return 0f;

        return BishopCantorComfortBonus;
    }

    public static int GetPassivePreachPressureBonus(Unit preacher)
    {
        if (preacher == null || !preacher.CanPreachOrHymn)
            return 0;

        int bonus = 0;
        if (FindArchbishop(preacher.Faction) != null)
            bonus += ArchbishopSiegeBonus;

        if (!ReceivesBishopOversight(preacher.Type) && preacher.Type != UnitType.Missionary)
            return bonus;

        var root = GetClusterRoot(preacher);
        var bishop = FindBishopForCluster(root);
        if (bishop != null && BishopIsPresent(bishop, root))
            bonus += BishopClusterSiegeBonus;

        return bonus;
    }

    public static void ProcessEndTurn(FactionId faction)
    {
        if (faction != FactionId.LutheranSynod || FirstSteps.Instance == null || TurnManager.Instance == null)
            return;

        var factionState = FirstSteps.Instance;
        bool logged = false;

        foreach (var unit in TurnManager.Instance.GetUnits(faction))
        {
            if (!unit.IsAlive || unit.Type != UnitType.Bishop)
                continue;

            var root = ClergyRoster.GetControllingRoot(ClergyRoster.GetAssignedCity(unit));
            if (root == null || !BishopIsPresent(unit, root))
                continue;

            factionState.AdjustSpiritualComfort(BishopClusterComfortPerTurn);
            if (!logged)
            {
                Debug.Log($"Bishop at {root.CityName} tended the cluster (+{BishopClusterComfortPerTurn} comfort).");
                logged = true;
            }
        }

        var archbishop = FindArchbishop(faction);
        if (archbishop == null)
            return;

        factionState.AdjustSpiritualComfort(ArchbishopComfortPerTurn);
        factionState.AdjustConfessionalAdherence(ArchbishopAdherencePerTurn);
        Debug.Log($"Archbishop shepherds the synod (+{ArchbishopComfortPerTurn} comfort, +{ArchbishopAdherencePerTurn} adherence).");
    }

    public static string FormatPassiveSummary(Unit unit)
    {
        if (unit == null || !unit.IsAlive)
            return "";

        if (unit.Type == UnitType.Bishop)
        {
            var root = ClergyRoster.GetControllingRoot(ClergyRoster.GetAssignedCity(unit));
            string city = root?.CityName ?? "cluster";
            return $"Oversees {city}: +{(FindArchbishop(unit.Faction) != null ? ArchbishopBishopAmplifier : BishopClusterPreachBonus):F0} preach to clergy, +{BishopClusterSiegeBonus} siege preach, +{BishopClusterComfortPerTurn} comfort/turn nearby";
        }

        if (unit.Type == UnitType.Archbishop)
        {
            return $"Synod-wide: +{ArchbishopSynodPreachBonus} preach, amplifies bishops (+{ArchbishopBishopAmplifier}), +{ArchbishopComfortPerTurn} comfort & +{ArchbishopAdherencePerTurn} adherence/turn";
        }

        if (!ReceivesBishopOversight(unit.Type))
            return "";

        float preach = GetPassivePreachBonus(unit);
        if (preach <= 0f)
            return "";

        return $"+{preach:F0} episcopal oversight";
    }
}
