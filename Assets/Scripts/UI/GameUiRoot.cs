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

    public static void InvalidateCache()
    {
        cachedCanvas = null;
        modalCanvas = null;
    }

    public static Canvas GetCanvas() => GetOrCreateCanvas(ref cachedCanvas, "GameUiCanvas", 500);

    /// <summary>High-priority overlay for modal panels (crisis cards, pickers).</summary>
    public static Canvas GetModalCanvas() =>
        GetOrCreateCanvas(ref modalCanvas, "ModalUiCanvas", ModalSortOrder);

    static Canvas GetOrCreateCanvas(ref Canvas slot, string name, int sortOrder)
    {
        if (IsAlive(slot))
            return slot;

        slot = null;

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (!IsAlive(canvas))
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                canvas.gameObject.activeInHierarchy &&
                canvas.sortingOrder <= sortOrder)
            {
                slot = canvas;
                return slot;
            }
        }

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (IsAlive(canvas))
            {
                slot = canvas;
                return slot;
            }
        }

        slot = CreateOverlayCanvas(name, sortOrder);
        return slot;
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

    static Canvas CreateOverlayCanvas(string name, int sortOrder)
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

        Debug.LogWarning($"GameUiRoot: created runtime overlay Canvas ({name}).");
        return canvas;
    }
}
