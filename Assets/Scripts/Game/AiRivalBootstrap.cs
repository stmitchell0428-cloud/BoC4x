using System.Collections.Generic;
using UnityEngine;

/// <summary>Deprecated — lobby rivals are now Lutheran AI synods via <see cref="SynodAiBootstrap"/>.</summary>
public static class AiRivalBootstrap
{
    public static void SpawnLobbyRivals(HexCoordinates synodAnchor)
    {
        Debug.LogWarning("AiRivalBootstrap is deprecated; SynodAiBootstrap handles lobby AI synod rivals.");
    }
}
