using System.Collections.Generic;
using UnityEngine;

/// <summary>Spawns pre-schism AI rival blocs from lobby player count (2-4 players).</summary>
public static class AiRivalBootstrap
{
    public static void SpawnLobbyRivals(HexCoordinates synodAnchor)
    {
        var settings = MatchLobbyController.Instance?.Current;
        if (settings == null || settings.PlayerCount <= 1)
            return;

        int aiCount = Mathf.Min(settings.PlayerCount - 1, SchismaticBlocRegistry.MaxBlocs);
        if (aiCount <= 0 || SchismManager.Instance == null || HexGridMap.Instance == null)
            return;

        var heresyPool = HeresyDatabase.GetHeresyPool(settings.HeresyPack);
        var placedCapitals = new List<HexCoordinates>();

        for (int i = 0; i < aiCount; i++)
        {
            var blocId = (SchismaticBlocId)(i + 1);
            if (SchismaticBlocRegistry.Instance.TryGetBloc(blocId, out _))
                continue;

            var heresy = heresyPool[i % heresyPool.Length];
            if (!SchismManager.Instance.TrySpawnLobbyRival(blocId, heresy, synodAnchor, placedCapitals))
                continue;

            if (SchismaticBlocRegistry.Instance.TryGetBloc(blocId, out var record))
                placedCapitals.Add(record.CapitalHex);
        }

        if (placedCapitals.Count > 0)
            Debug.Log($"Lobby rivals: spawned {placedCapitals.Count} dissent bloc(s) active from turn 1.");
    }
}
