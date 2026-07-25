using UnityEngine;

/// <summary>Chaplain is a specialty pastor  -  parish, military escort, or hospital ministry.</summary>
public enum ChaplainAssignment
{
    Parish,
    MilitaryEscort,
    Hospital
}

public static class ChaplainSpecialty
{
    public const int EscortDefenseBonus = 2;
    public const int EscortAttackBonus = 1;
    public const int EscortEndTurnHeal = 2;
    public const int HospitalUnitHeal = 2;
    public const int HospitalComfortBonus = 5;

    public static bool IsMilitaryUnit(Unit unit) =>
        unit != null && unit.IsAlive && unit.Type is
            UnitType.Soldier or
            UnitType.Slinger or
            UnitType.Archer or
            UnitType.Horseman or
            UnitType.Defender;

    public static int GetAttackBonus(Unit attacker)
    {
        if (attacker == null || !attacker.IsAlive)
            return 0;

        foreach (var chaplain in FindEscortChaplains(attacker))
        {
            if (chaplain.ChaplainRole == ChaplainAssignment.MilitaryEscort &&
                chaplain.EscortUnit == attacker)
                return EscortAttackBonus;
        }

        return 0;
    }

    public static int GetDefenseBonus(Unit defender)
    {
        if (defender == null || !defender.IsAlive)
            return 0;

        foreach (var chaplain in FindEscortChaplains(defender))
        {
            if (chaplain.ChaplainRole == ChaplainAssignment.MilitaryEscort &&
                chaplain.EscortUnit == defender)
                return EscortDefenseBonus;
        }

        return 0;
    }

    static System.Collections.Generic.List<Unit> FindEscortChaplains(Unit escorted)
    {
        var list = new System.Collections.Generic.List<Unit>();
        if (TurnManager.Instance == null || escorted == null)
            return list;

        foreach (var unit in TurnManager.Instance.GetUnits(escorted.Faction))
        {
            if (!unit.IsAlive || unit.Type != UnitType.Chaplain)
                continue;
            if (unit.ChaplainRole != ChaplainAssignment.MilitaryEscort)
                continue;
            if (unit.EscortUnit != escorted)
                continue;
            if (!AreLinked(unit, escorted))
                continue;
            list.Add(unit);
        }

        return list;
    }

    static bool AreLinked(Unit chaplain, Unit escort)
    {
        if (HexGridMap.Instance == null)
            return chaplain.HexPosition == escort.HexPosition;
        return HexGridMap.Instance.WrappedDistance(chaplain.HexPosition, escort.HexPosition) <= 1;
    }

    public static bool TryAssignParish(Unit chaplain)
    {
        if (!ValidateChaplain(chaplain))
            return false;
        chaplain.SetChaplainAssignment(ChaplainAssignment.Parish, null);
        Debug.Log($"{chaplain.Type} assigned to parish ministry at {chaplain.RosterCity?.CityName ?? "cluster"}.");
        return true;
    }

    public static bool TryAssignEscort(Unit chaplain, Unit militaryUnit)
    {
        if (!ValidateChaplain(chaplain) || !IsMilitaryUnit(militaryUnit))
            return false;
        if (militaryUnit.Faction != chaplain.Faction)
            return false;

        chaplain.SetChaplainAssignment(ChaplainAssignment.MilitaryEscort, militaryUnit);
        Debug.Log($"Chaplain escorting {Unit.TypeDisplayName(militaryUnit.Type)}  -  +{EscortAttackBonus} atk, +{EscortDefenseBonus} def while adjacent.");
        return true;
    }

    public static bool TryAssignHospital(Unit chaplain, City rootCity)
    {
        if (!ValidateChaplain(chaplain) || rootCity == null)
            return false;
        if (rootCity.Faction != chaplain.Faction)
            return false;

        var root = ClergyRoster.GetControllingRoot(rootCity);
        if (root?.Production?.HasBuilding(CityBuildId.BuildHospital) != true)
        {
            Debug.Log("Hospital ministry requires a Parish Hospital in the cluster.");
            return false;
        }

        chaplain.SetRosterCity(root);
        chaplain.SetChaplainAssignment(ChaplainAssignment.Hospital, null);
        Debug.Log($"Chaplain installed at {root.CityName} hospital  -  heals garrison and aids the sick each turn.");
        return true;
    }

    static bool ValidateChaplain(Unit chaplain) =>
        chaplain != null && chaplain.IsAlive && chaplain.Type == UnitType.Chaplain;

    public static float GetPreachAdherenceBonus(Unit chaplain)
    {
        if (chaplain == null || chaplain.Type != UnitType.Chaplain)
            return 0f;

        return chaplain.ChaplainRole switch
        {
            ChaplainAssignment.Hospital => 2.5f,
            ChaplainAssignment.MilitaryEscort => 2f,
            _ => 3f
        };
    }

    public static int GetPreachPressure(Unit chaplain)
    {
        if (chaplain == null || chaplain.Type != UnitType.Chaplain)
            return 0;

        return chaplain.ChaplainRole switch
        {
            ChaplainAssignment.MilitaryEscort => 13,
            ChaplainAssignment.Hospital => 10,
            _ => 12
        };
    }

    public static void ProcessEndTurn(FactionId faction)
    {
        if (TurnManager.Instance == null)
            return;

        foreach (var chaplain in TurnManager.Instance.GetUnits(faction))
        {
            if (!chaplain.IsAlive || chaplain.Type != UnitType.Chaplain)
                continue;

            switch (chaplain.ChaplainRole)
            {
                case ChaplainAssignment.MilitaryEscort:
                    ProcessMilitaryEscort(chaplain);
                    break;
                case ChaplainAssignment.Hospital:
                    ProcessHospital(chaplain);
                    break;
            }
        }
    }

    static void ProcessMilitaryEscort(Unit chaplain)
    {
        var escort = chaplain.EscortUnit;
        if (!IsMilitaryUnit(escort) || !AreLinked(chaplain, escort))
            return;

        if (escort.Health < escort.MaxHealth)
        {
            escort.Heal(EscortEndTurnHeal);
            Debug.Log($"Chaplain tended {Unit.TypeDisplayName(escort.Type)} (+{EscortEndTurnHeal} HP).");
        }
    }

    static void ProcessHospital(Unit chaplain)
    {
        var root = chaplain.RosterCity;
        if (root == null || root.Production?.HasBuilding(CityBuildId.BuildHospital) != true)
            return;

        if (HexGridMap.Instance != null &&
            HexGridMap.Instance.WrappedDistance(chaplain.HexPosition, root.HexPosition) > 1)
            return;

        if (TurnManager.Instance != null)
        {
            foreach (var unit in TurnManager.Instance.GetUnits(chaplain.Faction))
            {
                if (!unit.IsAlive || unit == chaplain)
                    continue;
                if (unit.HexPosition != root.HexPosition)
                    continue;
                if (unit.Health >= unit.MaxHealth)
                    continue;

                unit.Heal(HospitalUnitHeal);
            }
        }

        if (FirstSteps.Instance != null)
        {
            FirstSteps.Instance.AdjustSpiritualComfort(HospitalComfortBonus);
        }

        if (Random.value < 0.25f)
        {
            root.Population += 1;
            Debug.Log($"Hospital chaplain at {root.CityName} aided recovery (+comfort, patient care).");
        }
    }

    public static string FormatAssignment(Unit chaplain)
    {
        if (chaplain == null || chaplain.Type != UnitType.Chaplain)
            return "";

        return chaplain.ChaplainRole switch
        {
            ChaplainAssignment.MilitaryEscort when chaplain.EscortUnit != null =>
                $"Escorting {Unit.TypeDisplayName(chaplain.EscortUnit.Type)} (+{EscortAttackBonus} atk / +{EscortDefenseBonus} def)",
            ChaplainAssignment.MilitaryEscort => "Military escort (no unit linked)",
            ChaplainAssignment.Hospital =>
                $"Hospital  -  {chaplain.RosterCity?.CityName ?? "cluster"} (+{HospitalUnitHeal} HP on city hex, +comfort)",
            _ => "Parish specialty pastor"
        };
    }
}
