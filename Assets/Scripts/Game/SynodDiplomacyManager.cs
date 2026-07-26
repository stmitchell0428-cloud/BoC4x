using System.Collections.Generic;
using UnityEngine;

/// <summary>War/truce state between the human synod and lobby rival synods (schismatics stay always hostile).</summary>
public class SynodDiplomacyManager : MonoBehaviour
{
    public static SynodDiplomacyManager Instance { get; private set; }

    public const int TruceDurationTurns = 10;
    public const int TruceManuscriptCost = 2;

    readonly HashSet<SynodPlayerId> activeRivals = new();
    readonly Dictionary<SynodPlayerId, int> truceTurnsRemaining = new();

    public event System.Action DiplomacyChanged;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterRival(SynodPlayerId playerId)
    {
        if (playerId is SynodPlayerId.None or SynodPlayerId.Player1)
            return;

        activeRivals.Add(playerId);
        truceTurnsRemaining[playerId] = 0;
    }

    public IReadOnlyCollection<SynodPlayerId> ActiveRivals => activeRivals;

    public bool HasRivals => activeRivals.Count > 0;

    public int GetTruceTurns(SynodPlayerId rivalId) =>
        truceTurnsRemaining.TryGetValue(rivalId, out int turns) ? turns : 0;

    public bool IsTruceActive(SynodPlayerId rivalId) => GetTruceTurns(rivalId) > 0;

    public bool AreHostile(SynodPlayerId a, SynodPlayerId b)
    {
        if (a == b || a == SynodPlayerId.None || b == SynodPlayerId.None)
            return false;

        if (a != SynodPlayerId.Player1 && b != SynodPlayerId.Player1)
            return a != b;

        SynodPlayerId rival = a == SynodPlayerId.Player1 ? b : a;
        if (!activeRivals.Contains(rival))
            return a != b;

        return GetTruceTurns(rival) <= 0;
    }

    public bool TryProposeTruce(SynodPlayerId target)
    {
        if (!activeRivals.Contains(target) || IsTruceActive(target))
            return false;

        var faction = FirstSteps.Instance;
        if (faction == null || faction.ScriptureManuscripts < TruceManuscriptCost)
        {
            Debug.Log($"Truce refused — need {TruceManuscriptCost} manuscripts to send colloquy envoys.");
            return false;
        }

        faction.ScriptureManuscripts -= TruceManuscriptCost;
        truceTurnsRemaining[target] = TruceDurationTurns;
        Debug.Log(
            $"{SynodPlayerDatabase.DisplayName(target)} accepts a colloquy truce for {TruceDurationTurns} turns " +
            $"(−{TruceManuscriptCost} manuscripts).");
        NotifyChanged();
        faction.RefreshDashboard();
        return true;
    }

    public void DeclareWar(SynodPlayerId target)
    {
        if (!activeRivals.Contains(target))
            return;

        bool wasTruce = IsTruceActive(target);
        truceTurnsRemaining[target] = 0;
        Debug.Log(wasTruce
            ? $"Truce broken — {SynodPlayerDatabase.DisplayName(target)} is at war with your synod."
            : $"You declare open dispute against {SynodPlayerDatabase.DisplayName(target)}.");
        NotifyChanged();
        FirstSteps.Instance?.RefreshDashboard();
    }

    public void ProcessTurnEnd()
    {
        if (activeRivals.Count == 0)
            return;

        bool changed = false;
        var expired = new List<SynodPlayerId>();
        foreach (var rival in activeRivals)
        {
            int turns = GetTruceTurns(rival);
            if (turns <= 0)
                continue;

            turns--;
            truceTurnsRemaining[rival] = turns;
            changed = true;
            if (turns <= 0)
                expired.Add(rival);
        }

        foreach (var rival in expired)
            Debug.Log($"Truce with {SynodPlayerDatabase.DisplayName(rival)} has expired — at war again.");

        if (changed)
            NotifyChanged();
    }

    public string FormatSummaryLine()
    {
        if (activeRivals.Count == 0)
            return "";

        int truces = 0;
        foreach (var rival in activeRivals)
        {
            if (IsTruceActive(rival))
                truces++;
        }

        if (truces == 0)
            return $"<color=#FFAA88>Diplomacy</color>  at war with {activeRivals.Count} rival synod(s)  |  <color=#AABBCC>D</color> panel";

        return $"<color=#88CCAA>Diplomacy</color>  {truces} truce(s), {activeRivals.Count - truces} at war  |  <color=#AABBCC>D</color> panel";
    }

    public string FormatStatusLabel(SynodPlayerId rivalId)
    {
        int turns = GetTruceTurns(rivalId);
        return turns > 0
            ? $"<color=#88CCAA>Truce</color> ({turns} turns left)"
            : "<color=#FFAA88>At war</color>";
    }

    void NotifyChanged()
    {
        DiplomacyChanged?.Invoke();
        TerrainInfoPanel.Instance?.RefreshUnitDisplay();
        HexSelectionController.Instance?.RefreshHighlightsForSelection();
    }
}
