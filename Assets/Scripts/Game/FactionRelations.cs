/// <summary>Hostility between factions, synod players, and schismatic blocs.</summary>
public static class FactionRelations
{
    public static bool AreHostile(Unit a, Unit b)
    {
        if (a == null || b == null || !a.IsAlive || !b.IsAlive || a == b)
            return false;

        if (a.Faction != b.Faction)
            return a.Faction != FactionId.None && b.Faction != FactionId.None;

        if (a.Faction == FactionId.LutheranSynod)
            return a.SynodPlayer != b.SynodPlayer;

        if (a.Faction == FactionId.Schismatic)
            return a.SchismaticBloc != b.SchismaticBloc;

        return false;
    }

    public static bool IsHostileToCity(Unit unit, City city)
    {
        if (unit == null || city == null || !unit.IsAlive)
            return false;

        if (city.Faction != unit.Faction)
            return city.Faction != FactionId.None && unit.Faction != FactionId.None;

        if (city.Faction == FactionId.LutheranSynod)
            return city.SynodPlayer != unit.SynodPlayer;

        if (city.Faction == FactionId.Schismatic)
            return city.SchismaticBloc != unit.SchismaticBloc;

        return false;
    }
}
