using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target Tracking")]
    public Transform playerTarget;

    [Header("Tracking Settings")]
    public float trackingSmoothness = 6f;
    public float cameraHeightDepth = -10f;

    [Header("Free Pan")]
    public float keyboardPanSpeed = 22f;
    public float dragPanSpeed = 1f;

    Vector3? temporaryPanPosition;
    float temporaryPanUntil;
    bool userPanning;
    bool dragPanActive;
    Vector3 dragPanCamStart;
    Vector2 dragPanMouseStart;
    Camera cam;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void FollowUnit(Unit unit)
    {
        if (unit == null) return;
        playerTarget = unit.transform;
        userPanning = false;
        dragPanActive = false;
        ClearTemporaryPan();

        var focus = ResolveFollowWorld(unit.transform.position);
        transform.position = new Vector3(focus.x, focus.y, cameraHeightDepth);
    }

    public void PanToHex(HexCoordinates hex, float holdSeconds = 4f)
    {
        if (HexGridMap.Instance == null) return;
        var world = ResolveFollowWorld(HexGridMap.Instance.HexToWorld(hex));
        temporaryPanPosition = new Vector3(world.x, world.y, cameraHeightDepth);
        temporaryPanUntil = Time.time + holdSeconds;
        userPanning = true;
        dragPanActive = false;
    }

    public void ClearTemporaryPan()
    {
        temporaryPanPosition = null;
        temporaryPanUntil = 0f;
    }

    public void RecenterOnActiveUnit()
    {
        var selected = TurnManager.Instance?.SelectedUnit;
        if (selected != null)
            FollowUnit(selected);
        else if (playerTarget != null)
            userPanning = false;
    }

    void Update()
    {
        if (!TurnManager.Instance?.IsPlayerTurn ?? true)
            return;
        if (MatchController.Instance != null && MatchController.Instance.IsMatchOver)
            return;
        if (CityScreenPanel.Instance != null && CityScreenPanel.Instance.IsOpen)
            return;
        if (ConfessionTechPanel.Instance != null && ConfessionTechPanel.Instance.IsOpen)
            return;

        HandleKeyboardPan();
        HandleDragPan();

        if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame)
            RecenterOnActiveUnit();
    }

    void HandleKeyboardPan()
    {
        if (Keyboard.current == null) return;

        Vector2 move = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;

        if (move.sqrMagnitude <= 0f) return;

        userPanning = true;
        dragPanActive = false;
        ClearTemporaryPan();

        move.Normalize();
        var delta = new Vector3(move.x, move.y, 0f) * keyboardPanSpeed * Time.deltaTime;
        transform.position += delta;
        WrapCameraIntoHomeBand();
    }

    void HandleDragPan()
    {
        if (Mouse.current == null || cam == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            dragPanActive = true;
            userPanning = true;
            dragPanMouseStart = Mouse.current.position.ReadValue();
            dragPanCamStart = transform.position;
            ClearTemporaryPan();
        }

        if (dragPanActive && Mouse.current.middleButton.isPressed)
        {
            Vector2 current = Mouse.current.position.ReadValue();
            Vector2 deltaPixels = dragPanMouseStart - current;
            float worldPerPixel = cam.orthographicSize * 2f / Screen.height;
            var worldDelta = new Vector3(deltaPixels.x, deltaPixels.y, 0f) * worldPerPixel * dragPanSpeed;
            transform.position = dragPanCamStart + worldDelta;
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            dragPanActive = false;
            WrapCameraIntoHomeBand();
        }
    }

    void LateUpdate()
    {
        if (dragPanActive)
            return;

        if (userPanning)
        {
            WrapCameraIntoHomeBand();
            return;
        }

        if (temporaryPanPosition.HasValue && Time.time < temporaryPanUntil)
        {
            var panTarget = temporaryPanPosition.Value;
            if (HexGridMap.Instance != null)
            {
                var home = HexGridMap.Instance.WrapWorldIntoHomeBand(panTarget);
                panTarget = new Vector3(home.x, home.y, cameraHeightDepth);
            }

            LerpToward(panTarget);
            return;
        }

        if (temporaryPanPosition.HasValue)
            temporaryPanPosition = null;

        if (playerTarget == null) return;

        var focus = ResolveFollowWorld(playerTarget.position);
        LerpToward(new Vector3(focus.x, focus.y, cameraHeightDepth));
    }

    Vector3 ResolveFollowWorld(Vector3 targetWorld)
    {
        if (HexGridMap.Instance == null)
            return targetWorld;

        var home = HexGridMap.Instance.WrapWorldIntoHomeBand(targetWorld);
        // Prefer the wrap image nearest the current camera so selecting a unit while
        // viewing an edge clone does not teleport across the map into black void.
        var nearest = HexGridMap.Instance.NearestWorldImage(targetWorld, transform.position);
        float nearDist = (nearest - transform.position).sqrMagnitude;
        float homeDist = (home - transform.position).sqrMagnitude;
        if (nearDist + 0.01f < homeDist)
            return nearest;

        return home;
    }

    void WrapCameraIntoHomeBand()
    {
        if (HexGridMap.Instance == null)
            return;

        var wrapped = HexGridMap.Instance.WrapWorldIntoHomeBand(transform.position);
        transform.position = new Vector3(wrapped.x, wrapped.y, cameraHeightDepth);
    }

    void LerpToward(Vector3 targetCoordinates)
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetCoordinates,
            trackingSmoothness * Time.deltaTime);
        // Hard-clamp after lerp so wrap-band drift can't accumulate into a jump.
        WrapCameraIntoHomeBand();
    }
}
