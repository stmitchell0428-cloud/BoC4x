using System.Collections.Generic;

using UnityEngine;



/// <summary>Clergy role slots per city cluster  -  pastors, deaconesses, cantors, chaplains.</summary>

public enum ClergyRole

{

    Pastor,

    Bishop,

    Archbishop,

    Deaconess,

    Cantor,

    Chaplain

}



public static class ClergyRoster

{

    /// <summary>Field clergy roam freely and do not consume roster slots.</summary>

    public static bool IsFieldClergy(UnitType type) =>

        type == UnitType.Missionary;



    /// <summary>Installed clergy consume a cluster roster slot.</summary>

    public static bool IsStationaryClergy(UnitType type) => IsClergyUnit(type);



    public static ClergyRole? RoleForUnitType(UnitType type) => type switch

    {

        UnitType.Pastor => ClergyRole.Pastor,

        UnitType.Bishop => ClergyRole.Bishop,

        UnitType.Archbishop => ClergyRole.Archbishop,

        UnitType.Deaconess => ClergyRole.Deaconess,

        UnitType.Cantor => ClergyRole.Cantor,

        UnitType.Chaplain => ClergyRole.Chaplain,

        _ => null

    };



    static bool UsesGenericClusterSlot(ClergyRole role) =>

        role is ClergyRole.Deaconess or ClergyRole.Cantor or ClergyRole.Chaplain;



    public static int CountIndependentSynodCities(FactionId faction)

    {

        if (CityManager.Instance == null)

            return 0;



        int count = 0;

        foreach (var city in CityManager.Instance.AllCities)

        {

            if (city == null || city.Faction != faction || city.IsHamlet)

                continue;

            count++;

        }



        return count;

    }



    public static int CountParishChurchesInCluster(City city)

    {

        var root = GetControllingRoot(city);

        if (root == null)

            return 0;



        int count = 0;

        if (root.Production?.HasBuilding(CityBuildId.BuildParishChurch) == true)

            count++;

        if (root.Production?.HasBuilding(CityBuildId.BuildCathedral) == true)

            count++;



        if (CityManager.Instance == null)

            return count;



        foreach (var c in CityManager.Instance.AllCities)

        {

            if (c == null || c.Faction != root.Faction || !c.IsHamlet)

                continue;

            if (c.ControllingCity != root)

                continue;

            if (c.Production?.HasBuilding(CityBuildId.BuildParishChurch) == true)

                count++;

        }



        return count;

    }



    public static bool IsClergyUnit(UnitType type) => RoleForUnitType(type).HasValue;



    public static City GetControllingRoot(City city) => city?.ControllingCity ?? city;



    public static int GetMaxSlots(City city)

    {

        var root = GetControllingRoot(city);

        if (root == null)

            return 0;



        if (root.Faction == FactionId.Schismatic)

            return 2;



        int slots = root.IsCapital ? 1 : 0;



        if (root.Production?.HasBuilding(CityBuildId.BuildParishChurch) == true)

            slots += 1;

        if (root.Production?.HasBuilding(CityBuildId.BuildSeminary) == true)

            slots += 1;



        if (CityManager.Instance != null)

        {

            foreach (var c in CityManager.Instance.AllCities)

            {

                if (c == null || c.Faction != root.Faction || !c.IsHamlet)

                    continue;

                if (c.ControllingCity != root)

                    continue;

                if (c.Specialty == HamletSpecialty.Seminary)

                    slots += 1;

            }

        }



        if (root.Population >= City.MediumPopulation)

            slots += 1;

        if (root.Population >= City.LargePopulation)

            slots += 1;



        return Mathf.Clamp(slots, root.IsCapital ? 1 : 0, 5);

    }



    public static int GetRoleCap(City city, ClergyRole role)

    {

        var root = GetControllingRoot(city);

        if (root == null)

            return 0;



        return role switch

        {

            ClergyRole.Pastor => CountParishChurchesInCluster(root),

            ClergyRole.Bishop => 1,

            ClergyRole.Archbishop => CountIndependentSynodCities(root.Faction) >= 2 ? 1 : 0,

            ClergyRole.Cantor => HasOrganLoftInCluster(root) ? 2 : 1,

            _ => 1

        };

    }



    static bool HasOrganLoftInCluster(City root)

    {

        if (root.Production?.HasBuilding(CityBuildId.BuildOrganLoft) == true)

            return true;



        if (CityManager.Instance == null)

            return false;



        foreach (var c in CityManager.Instance.AllCities)

        {

            if (c == null || c.Faction != root.Faction || !c.IsHamlet)

                continue;

            if (c.ControllingCity != root)

                continue;

            if (c.Production?.HasBuilding(CityBuildId.BuildOrganLoft) == true)

                return true;

        }



        return false;

    }



    public static int CountAssigned(City city)

    {

        var root = GetControllingRoot(city);

        if (root == null || TurnManager.Instance == null)

            return 0;



        int count = 0;

        foreach (var unit in TurnManager.Instance.GetUnits(root.Faction))

        {

            if (!unit.IsAlive || !IsClergyUnit(unit.Type))

                continue;



            var role = RoleForUnitType(unit.Type);

            if (!role.HasValue || !UsesGenericClusterSlot(role.Value))

                continue;



            if (GetControllingRoot(GetAssignedCity(unit)) == root)

                count++;

        }



        return count;

    }



    public static int CountRole(City city, ClergyRole role)

    {

        if (role == ClergyRole.Archbishop)

            return CountRoleFactionWide(city?.Faction ?? FactionId.None, role);



        var root = GetControllingRoot(city);

        if (root == null || TurnManager.Instance == null)

            return 0;



        int count = 0;

        foreach (var unit in TurnManager.Instance.GetUnits(root.Faction))

        {

            if (!unit.IsAlive || RoleForUnitType(unit.Type) != role)

                continue;

            if (GetControllingRoot(GetAssignedCity(unit)) == root)

                count++;

        }



        return count;

    }



    public static int CountRoleFactionWide(FactionId faction, ClergyRole role)

    {

        if (TurnManager.Instance == null || faction == FactionId.None)

            return 0;



        int count = 0;

        foreach (var unit in TurnManager.Instance.GetUnits(faction))

        {

            if (!unit.IsAlive || RoleForUnitType(unit.Type) != role)

                continue;

            count++;

        }



        return count;

    }



    public static City GetAssignedCity(Unit unit)

    {

        if (unit == null)

            return null;

        return unit.RosterCity ?? CityManager.Instance?.GetCityForUnit(unit) ??

               CityManager.Instance?.GetNearestPlayerCity(unit.HexPosition);

    }



    public static bool CanAssign(City city, ClergyRole role)

    {

        if (city?.Faction == FactionId.Schismatic)

            return CountRole(city, role) < GetRoleCap(city, role);



        if (role == ClergyRole.Pastor)

            return CountRole(city, role) < GetRoleCap(city, role);



        if (role == ClergyRole.Bishop)

            return CountRole(city, role) < GetRoleCap(city, role);



        if (role == ClergyRole.Archbishop)

            return CountRoleFactionWide(city.Faction, role) < GetRoleCap(city, role);



        if (CountAssigned(city) >= GetMaxSlots(city))

            return false;

        return CountRole(city, role) < GetRoleCap(city, role);

    }



    public static bool CanTrainClergy(City city, UnitType type)

    {

        var role = RoleForUnitType(type);

        return role.HasValue && CanAssign(city, role.Value);

    }



    public static bool CanUpgradeToClergy(City city, UnitType toType, UnitType? fromType = null)

    {

        if (IsFieldClergy(toType))

            return true;



        var role = RoleForUnitType(toType);

        if (!role.HasValue)

            return false;



        // Role swap (e.g. pastor -> chaplain) frees one installed slot  -  only check target role cap.

        if (fromType.HasValue && IsStationaryClergy(fromType.Value))

        {

            var fromRole = RoleForUnitType(fromType.Value);

            if (fromRole.HasValue && fromRole.Value != role.Value)

                return CountRole(city, role.Value) < GetRoleCap(city, role.Value);

        }



        return CanAssign(city, role.Value);

    }



    /// <summary>Where clergy may be trained  -  capital pastor with church; seminary district for office-holders.</summary>

    public static bool CanTrainBuild(City city, CityBuildId id)

    {

        var def = CityBuildDatabase.Get(id);

        if (!def.SpawnsUnit.HasValue)

            return true;



        var type = def.SpawnsUnit.Value;



        // Chaplain is upgrade-only from pastor.

        if (type == UnitType.Chaplain)

            return false;



        if (type == UnitType.Cantor)

            return city.IsHamlet && city.Specialty == HamletSpecialty.Seminary;



        if (type == UnitType.Deaconess)

            return city.IsHamlet && city.Specialty == HamletSpecialty.Seminary;



        if (type == UnitType.Pastor)

        {

            var root = GetControllingRoot(city);

            if (root == null || CountParishChurchesInCluster(root) <= CountRole(root, ClergyRole.Pastor))

                return false;



            if (city.IsHamlet)

                return city.Specialty == HamletSpecialty.Seminary;



            if (city != root)

                return false;



            return root.Production?.HasBuilding(CityBuildId.BuildParishChurch) == true ||

                   root.Production?.HasBuilding(CityBuildId.BuildSeminary) == true ||

                   root.Production?.HasBuilding(CityBuildId.BuildCathedral) == true;

        }



        if (type is UnitType.Bishop or UnitType.Archbishop)

            return false;



        return true;

    }



    public static CityBuildStatus GetTrainBuildStatus(City city, CityBuildId id)

    {

        if (!CanTrainBuild(city, id))

        {

            var def = CityBuildDatabase.Get(id);

            if (def.SpawnsUnit is UnitType.Cantor or UnitType.Chaplain)

                return CityBuildStatus.Locked;

            if (def.SpawnsUnit == UnitType.Pastor && !city.IsHamlet)

                return CityBuildStatus.Locked;

            if (def.SpawnsUnit == UnitType.Deaconess)

                return CityBuildStatus.Locked;

        }



        return CityBuildStatus.Available;

    }



    public static bool HasSeminaryAccess(City city)

    {

        if (city == null)

            return false;



        if (city.IsHamlet && city.Specialty == HamletSpecialty.Seminary)

            return true;



        var root = GetControllingRoot(city);

        if (root?.Production?.HasBuilding(CityBuildId.BuildSeminary) == true)

            return true;



        if (CityManager.Instance == null)

            return false;



        foreach (var c in CityManager.Instance.AllCities)

        {

            if (c == null || c.Faction != root.Faction || !c.IsHamlet)

                continue;

            if (c.ControllingCity != root)

                continue;

            if (c.Specialty == HamletSpecialty.Seminary)

                return true;

        }



        return false;

    }



    public static void RegisterUnit(Unit unit, City city)

    {

        if (unit == null || city == null || !IsClergyUnit(unit.Type))

            return;

        unit.SetRosterCity(GetControllingRoot(city));

    }



    public static bool TryReassign(Unit unit, City rootCity)

    {

        if (unit == null || rootCity == null || !IsStationaryClergy(unit.Type))

            return false;

        if (unit.Faction != rootCity.Faction)

            return false;



        var role = RoleForUnitType(unit.Type);

        if (!role.HasValue)

            return false;



        var root = GetControllingRoot(rootCity);

        var currentRoot = GetControllingRoot(GetAssignedCity(unit));

        if (currentRoot == root)

            return false;



        if (role.Value == ClergyRole.Archbishop)

        {

            if (root.IsHamlet || GetRoleCap(root, ClergyRole.Archbishop) < 1)

                return false;

            unit.SetRosterCity(root);

            Debug.Log($"Archbishop assigned to oversee {root.CityName}.");

            return true;

        }



        if (!CanAssign(root, role.Value))

            return false;



        unit.SetRosterCity(root);

        Debug.Log($"{Unit.TypeDisplayName(unit.Type)} assigned to {root.CityName} clergy roster.");

        return true;

    }



    public static float GetParishPreachBonus(Unit unit)

    {

        if (unit == null || !IsStationaryClergy(unit.Type) || unit.RosterCity == null)

            return 0f;



        float bonus = unit.Type switch

        {

            UnitType.Bishop => 1f,

            UnitType.Archbishop => 2f,

            _ => 0f

        };



        if (HexGridMap.Instance == null)

            return bonus;



        int dist = HexGridMap.Instance.WrappedDistance(unit.HexPosition, unit.RosterCity.HexPosition);

        if (dist == 0)

            return 3f + bonus;

        if (dist <= 2)

            return 1f + bonus * 0.5f;

        return bonus > 0f ? bonus * 0.25f : 0f;

    }



    public static string FormatRosterLine(City city)

    {

        if (city == null || city.IsHamlet)

            return "";



        int assigned = CountAssigned(city);

        int max = GetMaxSlots(city);

        if (max <= 0)

            return "";



        var parts = new List<string>();

        foreach (ClergyRole role in System.Enum.GetValues(typeof(ClergyRole)))

        {

            int n = CountRole(city, role);

            int cap = GetRoleCap(city, role);

            if (n > 0 || cap > 1)

                parts.Add($"{RoleLabel(role)} {n}/{cap}");

            else if (n > 0)

                parts.Add($"{RoleLabel(role)} {n}");

        }



        string roster = parts.Count > 0 ? string.Join(", ", parts) : "none assigned";

        return $"<b>Clergy</b> {assigned}/{max}  -  {roster}";

    }



    public static string FormatRosterDetail(City city)

    {

        if (city == null)

            return "";



        var root = GetControllingRoot(city);

        if (root == null)

            return "";



        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"<b>{root.CityName}</b> cluster  -  {CountAssigned(root)}/{GetMaxSlots(root)} slots");

        sb.AppendLine("<size=13><i>Field:</i> Missionary (roam  -  no slot)</size>");

        sb.AppendLine("<size=13><i>Per church:</i> Pastor (1/church)  |  <i>Per city:</i> Bishop (1)  |  <i>Synod (2+ cities):</i> Archbishop (1)</size>");

        sb.AppendLine("<size=13><i>Cluster support:</i> Deaconess, Cantor, Chaplain (slot-limited)</size>");



        int churches = CountParishChurchesInCluster(root);

        sb.AppendLine($"<size=13>Parish churches in cluster: {churches}  |  Synod cities: {CountIndependentSynodCities(root.Faction)}</size>");



        foreach (ClergyRole role in System.Enum.GetValues(typeof(ClergyRole)))

        {

            var units = GetUnitsInRole(root, role);

            string line = units.Count > 0

                ? string.Join(", ", units.ConvertAll(FormatRosterUnitLabel))

                : " -  empty  - ";

            sb.AppendLine($"{RoleLabel(role)} ({CountRole(root, role)}/{GetRoleCap(root, role)}): {line}");

        }



        return sb.ToString().TrimEnd();

    }



    static List<Unit> GetUnitsInRole(City root, ClergyRole role)

    {

        var list = new List<Unit>();

        if (TurnManager.Instance == null)

            return list;



        foreach (var unit in TurnManager.Instance.GetUnits(root.Faction))

        {

            if (!unit.IsAlive || RoleForUnitType(unit.Type) != role)

                continue;



            if (role == ClergyRole.Archbishop)

            {

                list.Add(unit);

                continue;

            }



            if (GetControllingRoot(GetAssignedCity(unit)) == root)

                list.Add(unit);

        }



        return list;

    }



    public static IEnumerable<Unit> GetRosterClergyForRoot(City root)

    {

        if (root == null || TurnManager.Instance == null)

            yield break;



        foreach (var unit in TurnManager.Instance.GetUnits(root.Faction))

        {

            if (!unit.IsAlive || !IsStationaryClergy(unit.Type))

                continue;

            if (GetControllingRoot(GetAssignedCity(unit)) == root)

                yield return unit;

        }

    }



    static string FormatRosterUnitLabel(Unit unit)

    {

        if (unit.Type == UnitType.Chaplain)

            return ChaplainSpecialty.FormatAssignment(unit);

        if (unit.Type is UnitType.Bishop or UnitType.Archbishop)

            return $"{Unit.TypeDisplayName(unit.Type)}  -  {EpiscopalOversight.FormatPassiveSummary(unit)}";

        return Unit.TypeDisplayName(unit.Type);

    }



    static string RoleLabel(ClergyRole role) => role switch

    {

        ClergyRole.Pastor => "Pastor",

        ClergyRole.Bishop => "Bishop",

        ClergyRole.Archbishop => "Archbishop",

        ClergyRole.Deaconess => "Deaconess",

        ClergyRole.Cantor => "Cantor",

        ClergyRole.Chaplain => "Chaplain",

        _ => role.ToString()

    };

}


