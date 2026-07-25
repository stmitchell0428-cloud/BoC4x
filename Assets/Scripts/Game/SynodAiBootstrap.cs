using System.Collections.Generic;
using UnityEngine;

/// <summary>Spawns rival Lutheran synod factions from lobby player count (2–4 players).</summary>
public static class SynodAiBootstrap
{
    public static void SpawnLobbySynodPlayers(HexCoordinates humanAnchor, Transform parent)
    {
        var settings = MatchLobbyController.Instance?.Current;
        if (settings == null || settings.PlayerCount <= 1 || HexGridMap.Instance == null)
            return;

        int aiCount = Mathf.Min(settings.PlayerCount - 1, SynodPlayerDatabase.MaxPlayers - 1);
        if (aiCount <= 0)
            return;

        var placedCapitals = new List<HexCoordinates>();
        int spawned = 0;

        for (int i = 0; i < aiCount; i++)
        {
            var playerId = (SynodPlayerId)(i + 2);
            if (!HexGridMap.Instance.TryPickRivalSpawnSite(
                    humanAnchor,
                    placedCapitals,
                    out var capitalHex,
                    out var soldierHex,
                    out var scoutHex))
            {
                Debug.LogWarning($"Synod AI {playerId}: no valid spawn site.");
                continue;
            }

            string capitalName = SynodPlayerDatabase.DefaultCapitalName(playerId);
            SpawnSynodCity(parent, playerId, capitalHex, capitalName);
            SpawnSynodUnit(parent, playerId, UnitType.Soldier, soldierHex);
            SpawnSynodUnit(parent, playerId, UnitType.Scout, scoutHex);

            placedCapitals.Add(capitalHex);
            TurnManager.Instance?.ActivateSynodPlayer(playerId);
            spawned++;
        }

        if (spawned > 0)
            Debug.Log($"Lobby synod rivals: spawned {spawned} AI synod faction(s) active from turn 1.");
    }

    static void SpawnSynodCity(Transform parent, SynodPlayerId playerId, HexCoordinates hex, string name)
    {
        var go = new GameObject($"City_{name}");
        go.transform.SetParent(parent);
        var city = go.AddComponent<City>();
        city.Initialize(FactionId.LutheranSynod, hex, name, isCapital: true, synodPlayer: playerId);
    }

    static void SpawnSynodUnit(Transform parent, SynodPlayerId playerId, UnitType type, HexCoordinates hex)
    {
        var go = new GameObject($"Synod{playerId}_{type}");
        go.transform.SetParent(parent);
        var unit = go.AddComponent<Unit>();
        unit.Initialize(FactionId.LutheranSynod, type, hex, synodPlayer: playerId);
        TurnManager.Instance?.RegisterUnit(unit);
    }
}
