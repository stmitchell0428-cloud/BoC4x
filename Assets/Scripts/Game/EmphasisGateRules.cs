using System.Collections.Generic;

/// <summary>Pure match gates for tier-2 confessional emphasis options.</summary>
public static class EmphasisGateRules
{
    public static bool CanOfferAugsburgConfessionalEmphasis(
        bool hasActiveSchism,
        IEnumerable<SchismaticBlocId> scoutContactBlocs,
        System.Func<SchismaticBlocId, bool> isActiveBloc)
    {
        if (!hasActiveSchism)
            return false;

        return HasActiveBlocIn(scoutContactBlocs, isActiveBloc);
    }

    public static bool CanOfferSmalcaldConfessionalEmphasis(
        bool hasActiveSchism,
        int playerSchismaticCombatEngagements)
    {
        return hasActiveSchism && playerSchismaticCombatEngagements > 0;
    }

    static bool HasActiveBlocIn(
        IEnumerable<SchismaticBlocId> blocs,
        System.Func<SchismaticBlocId, bool> isActiveBloc)
    {
        if (blocs == null || isActiveBloc == null)
            return false;

        foreach (var blocId in blocs)
        {
            if (blocId != SchismaticBlocId.None && isActiveBloc(blocId))
                return true;
        }

        return false;
    }
}
