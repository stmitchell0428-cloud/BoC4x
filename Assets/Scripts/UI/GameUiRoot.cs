using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Resolves screen-space UI canvases, creating dedicated overlays when needed.</summary>
public static class GameUiRoot
{
    const int ModalSortOrder = 1000;

    static Canvas cachedCanvas;
    static Canvas modalCanvas;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cachedCanvas = null;
        modalCanvas = null;
    }
#endif

    public static void InvalidateCache()
    {
        cachedCanvas = null;
        modalCanvas = null;
    }

    public static Canvas GetModalCanvas()
    {
        if (IsAlive(modalCanvas))
            return modalCanvas;

        modalCanvas = FindNamedCanvas("ModalUiCanvas");
        if (IsAlive(modalCanvas))
            return modalCanvas;

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (!IsAlive(canvas))
                continue;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                modalCanvas = canvas;
                return modalCanvas;
            }
        }

        modalCanvas = CreateOverlayCanvas("ModalUiCanvas", ModalSortOrder);
        return modalCanvas;
    }

    static Canvas FindNamedCanvas(string name)
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (IsAlive(canvas) && canvas.gameObject.name == name)
                return canvas;
        }

        return null;
    }

    /// <summary>Creates a dedicated overlay canvas when scene UI is not ready yet.</summary>
    public static Canvas CreateOverlayCanvas(string name, int sortOrder, bool logCreation = false)
    {
        EnsureEventSystem();

        var go = new GameObject(name);
        Object.DontDestroyOnLoad(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        canvas.pixelPerfect = false;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        if (logCreation)
            Debug.Log($"GameUiRoot: created runtime overlay Canvas ({name}).");

        return canvas;
    }

    public static Canvas GetCanvas()
    {
        if (IsAlive(cachedCanvas))
            return cachedCanvas;

        cachedCanvas = FindNamedCanvas("GameUiCanvas");
        if (IsAlive(cachedCanvas))
            return cachedCanvas;

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (IsAlive(canvas))
            {
                cachedCanvas = canvas;
                return cachedCanvas;
            }
        }

        cachedCanvas = CreateOverlayCanvas("GameUiCanvas", 500);
        return cachedCanvas;
    }

    static bool IsAlive(Object obj) => obj != null;

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        var esGo = new GameObject("EventSystem");
        Object.DontDestroyOnLoad(esGo);
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }
}
