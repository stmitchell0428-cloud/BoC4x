using System;
using System.Collections;
using UnityEngine;

/// <summary>Gates map generation and match bootstrap until the lobby confirms settings.</summary>
public class MatchLobbyController : MonoBehaviour
{
    public static MatchLobbyController Instance { get; private set; }

    [Tooltip("When true, skip the lobby and start immediately with scene defaults.")]
    public bool skipLobby;

    public MatchSettings Current { get; private set; } = MatchSettings.CreateDefault();
    public bool MatchStarted { get; private set; }

    public event Action MatchStartedEvent;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (skipLobby)
        {
            BeginMatch(Current);
            return;
        }

        if (MatchLobbyPanel.Instance == null || !MatchLobbyPanel.Instance.EnsureUiBuilt())
        {
            Debug.LogWarning("MatchLobbyPanel unavailable  -  starting match with default settings.");
            BeginMatch(Current);
            return;
        }

        MatchLobbyPanel.Instance.Show();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void UpdateSettings(MatchSettings settings)
    {
        Current = settings ?? MatchSettings.CreateDefault();
    }

    public void BeginMatch(MatchSettings settings)
    {
        if (MatchStarted)
            return;

        Current = settings ?? MatchSettings.CreateDefault();
        MatchStarted = true;
        StartCoroutine(BeginMatchRoutine());
    }

    IEnumerator BeginMatchRoutine()
    {
        MatchLobbyPanel.Instance?.Hide();
        LoadingScreenPanel.Instance?.Show();
        yield return null;

        var map = HexGridMap.Instance;
        if (map != null)
        {
            map.ApplyMatchSettings(Current);
            yield return map.GenerateMapAsync(p => LoadingScreenPanel.Instance?.SetLoadProgress(p));
        }

        MatchStartedEvent?.Invoke();

        Debug.Log($"Book of Concord: match started  -  {Current.SummaryLine()}");
    }
}
