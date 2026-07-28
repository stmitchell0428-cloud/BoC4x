using System.Collections.Generic;
using UnityEngine;

/// <summary>Match facts that gate era emphasis options (scout contact, schismatic combat).</summary>
public class MatchHistory : MonoBehaviour
{
    public static MatchHistory Instance { get; private set; }

    int playerCombatEngagements;
    int playerSchismaticCombatEngagements;
    readonly HashSet<SchismaticBlocId> scoutContactBlocs = new();
    readonly HashSet<SchismaticBlocId> combatContactBlocs = new();

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HasPlayerCombat => playerCombatEngagements > 0;

    public bool HasPlayerSchismaticCombat => playerSchismaticCombatEngagements > 0;

    public bool HasMetAnySchismaticBloc =>
        scoutContactBlocs.Count > 0 || combatContactBlocs.Count > 0;

    public bool CanOfferAugsburgConfessionalEmphasis() =>
        EmphasisGateRules.CanOfferAugsburgConfessionalEmphasis(
            HasActiveSchism(),
            scoutContactBlocs,
            IsActiveBloc);

    public bool CanOfferSmalcaldConfessionalEmphasis() =>
        EmphasisGateRules.CanOfferSmalcaldConfessionalEmphasis(
            HasActiveSchism(),
            playerSchismaticCombatEngagements);

    public void RegisterPlayerCombat(Unit attacker, Unit defender)
    {
        if (!InvolvesPlayerSynod(attacker, defender))
            return;

        playerCombatEngagements++;

        if (!InvolvesSchismatic(attacker, defender))
            return;

        playerSchismaticCombatEngagements++;
        if (attacker.Faction == FactionId.Schismatic)
            RegisterSchismaticBlocCombatContact(attacker.SchismaticBloc);
        if (defender.Faction == FactionId.Schismatic)
            RegisterSchismaticBlocCombatContact(defender.SchismaticBloc);
    }

    public void RegisterSchismaticBlocScoutContact(SchismaticBlocId blocId)
    {
        if (blocId == SchismaticBlocId.None)
            return;

        scoutContactBlocs.Add(blocId);
    }

    public void RegisterSchismaticBlocCombatContact(SchismaticBlocId blocId)
    {
        if (blocId == SchismaticBlocId.None)
            return;

        combatContactBlocs.Add(blocId);
    }

    static bool HasActiveSchism() =>
        SchismaticBlocRegistry.Instance != null && SchismaticBlocRegistry.Instance.HasAnySchism;

    bool IsActiveBloc(SchismaticBlocId blocId) =>
        SchismaticBlocRegistry.Instance != null &&
        SchismaticBlocRegistry.Instance.TryGetBloc(blocId, out _);

    static bool InvolvesPlayerSynod(Unit a, Unit b) =>
        IsPlayerSynodUnit(a) || IsPlayerSynodUnit(b);

    static bool InvolvesSchismatic(Unit a, Unit b) =>
        a.Faction == FactionId.Schismatic || b.Faction == FactionId.Schismatic;

    static bool IsPlayerSynodUnit(Unit unit) =>
        unit != null &&
        unit.Faction == FactionId.LutheranSynod &&
        unit.SynodPlayer == SynodPlayerId.Player1;
}
