using UnityEngine;

/// <summary>Resets static match state when Enter Play Mode runs without domain reload.</summary>
public static class PlayModeStateReset
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void FixSceneCanvas()
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas == null)
                continue;

            var scale = canvas.transform.localScale;
            if (scale.x == 0f || scale.y == 0f)
            {
                canvas.transform.localScale = Vector3.one;
                Debug.LogWarning($"Fixed Canvas '{canvas.name}' scale was zero — UI should be visible now.");
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        NomadicFoundingGate.ResetForNewMatch();
        GameUiRoot.InvalidateCache();
        CityPlacementAdvisor.InvalidateCache();
    }
}
