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

    public string FormatEmphasisGateSummary()
    {
        bool schism = HasActiveSchism();
        bool augsburg = CanOfferAugsburgConfessionalEmphasis();
        bool smalcald = CanOfferSmalcaldConfessionalEmphasis();

        if (!schism && !HasPlayerCombat)
            return "<size=12><color=#AABBCC>Military witness: no combat yet — Smalcald emphasis locked until schismatic battle.</color></size>";

        var lines = new List<string>();
        lines.Add("<size=13><color=#DDCC88><b>Emphasis gates</b></color></size>");

        string scoutGate = augsburg
            ? "<color=#88FFAA>Scout contact met</color> — Augsburg confessional emphasis available"
            : schism
                ? "<color=#FFCC88>Scout a schismatic bloc</color> — Augsburg confessional emphasis locked"
                : "<color=#CCCCCC>Augsburg emphasis needs schism + scout contact</color>";
        lines.Add($"<size=12>{scoutGate}</size>");

        string combatGate = smalcald
            ? "<color=#88FFAA>Military witness met</color> — Smalcald confessional emphasis available"
            : schism
                ? "<color=#FFCC88>Fight schismatic forces</color> — Smalcald confessional emphasis locked"
                : "<color=#CCCCCC>Smalcald emphasis needs schism + combat</color>";
        lines.Add($"<size=12>{combatGate}</size>");

        if (playerSchismaticCombatEngagements > 0)
            lines.Add($"<size=12><color=#AABBCC>Military witness: {playerSchismaticCombatEngagements} schismatic engagement(s) logged.</color></size>");

        return string.Join("\n", lines);
    }

    public string FormatBriefMilitaryWitnessLine()
    {
        if (playerSchismaticCombatEngagements > 0)
            return $"<size=12><color=#88CCAA><b>Military witness:</b> {playerSchismaticCombatEngagements} schismatic combat engagement(s) — Smalcald emphasis gate met.</color></size>";

        if (HasPlayerCombat)
            return "<size=12><color=#FFCC88><b>Military witness:</b> synod combat logged, but no schismatic battle yet.</color></size>";

        return "<size=12><color=#AABBCC><b>Military witness:</b> none yet — Smalcald emphasis unlocks after schismatic combat.</color></size>";
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
