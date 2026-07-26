using System.Linq;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Optional legacy scene marker")]
    public Transform legacyPlayerMarker;

    void Start()
    {
        StartCoroutine(BootstrapAfterLobby());
    }

    System.Collections.IEnumerator BootstrapAfterLobby()
    {
        while (MatchLobbyController.Instance == null)
            yield return null;

        if (!MatchLobbyController.Instance.MatchStarted)
        {
            bool started = false;
            void OnStarted() => started = true;
            MatchLobbyController.Instance.MatchStartedEvent += OnStarted;
            while (!started)
                yield return null;
            MatchLobbyController.Instance.MatchStartedEvent -= OnStarted;
        }

        while (HexGridMap.Instance == null || !HexGridMap.Instance.TryGetTile(new HexCoordinates(0, 0), out _))
            yield return null;

        yield return null;
        SetupMatch();
    }

    void SetupMatch()
    {
        LoadingScreenPanel.Instance?.SetLoadProgress(0.98f);
        if (legacyPlayerMarker == null)
        {
            var tribe = GameObject.Find("PlayerTribe");
            if (tribe != null)
                legacyPlayerMarker = tribe.transform;
        }

        if (legacyPlayerMarker != null)
        {
            // Keep FirstSteps alive  -  only hide the legacy map sprite.
            var legacySprite = legacyPlayerMarker.GetComponent<SpriteRenderer>();
            if (legacySprite != null)
                legacySprite.enabled = false;
        }

        var map = HexGridMap.Instance;
        if (map == null)
        {
            Debug.LogError("Book of Concord: HexGridMap missing  -  cannot bootstrap match.");
            LoadingScreenPanel.Instance?.Hide();
            return;
        }

        var spawn = map.SpawnLayout;

        NomadicFoundingGate.ResetForNewMatch();

        SpawnUnit(FactionId.LutheranSynod, UnitType.Settler, spawn.SynodSettler, isNomadicFounder: true);
        SpawnUnit(FactionId.LutheranSynod, UnitType.Scout, spawn.SynodScout);

        SynodAiBootstrap.SpawnLobbySynodPlayers(spawn.SynodSettler, transform);

        var factionState = FirstSteps.Instance ?? FindAnyObjectByType<FirstSteps>();
        factionState?.BindPlayerUnit(FindPlayerLeader());

        var tm = TurnManager.Instance;
        if (tm != null)
        {
            tm.TurnEnded += OnTurnEnded;
            tm.BeginGame();
        }

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            var playerUnit = FindPlayerLeader();
            if (playerUnit != null)
                cam.FollowUnit(playerUnit);
        }

        Debug.Log("Book of Concord: nomadic start  -  settler + scout. F = found Wittenberg. AI synod rivals may already be on the map from lobby settings.");

        var settings = MatchLobbyController.Instance?.Current;
        if (settings != null && settings.PlayerCount > 1)
            Debug.Log($"Lobby: {settings.PlayerCount - 1} AI synod rival(s) active alongside your synod.");

        ConfessionResearchManager.Instance?.ApplyBonusesToAllPlayerUnits();
        TerrainInfoPanel.Instance?.RefreshMissionaryTile();
        TerrainInfoPanel.Instance?.RefreshCityYield();
        FirstSteps.Instance?.RefreshDashboard();
        GameHUD.Instance?.EnsureDashboardVisible();
        FogOfWarManager.Instance?.Refresh();
        ArtEraVisualController.Instance?.ApplyGlobalVisuals();
        LoadingScreenPanel.Instance?.NotifyLoadComplete();
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.TurnEnded -= OnTurnEnded;
    }

    void OnTurnEnded()
    {
        // Player turn-end logic runs via EndTurnPhaseController before EndTurn() is called.
    }

    static Unit FindPlayerLeader()
    {
        if (TurnManager.Instance == null) return null;
        var units = TurnManager.Instance.GetSynodUnits(SynodPlayerId.Player1);
        return units.FirstOrDefault(u => u.IsAlive && u.Type == UnitType.Settler && u.IsNomadicFounder)
            ?? units.FirstOrDefault(u => u.IsAlive && u.Type == UnitType.Missionary)
            ?? units.FirstOrDefault(u => u.IsAlive);
    }

    void SpawnCity(FactionId faction, HexCoordinates hex, string name, bool isCapital = false)
    {
        var go = new GameObject($"City_{name}");
        go.transform.SetParent(transform);
        go.AddComponent<City>().Initialize(faction, hex, name, isCapital);
    }

    void SpawnUnit(FactionId faction, UnitType type, HexCoordinates hex, bool isNomadicFounder = false)
    {
        var go = new GameObject($"{faction}_{type}");
        go.transform.SetParent(transform);
        var unit = go.AddComponent<Unit>();
        unit.Initialize(faction, type, hex, isNomadicFounder);
        TurnManager.Instance.RegisterUnit(unit);
    }
}
