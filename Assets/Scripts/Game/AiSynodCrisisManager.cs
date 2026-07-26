using System.Collections.Generic;
using UnityEngine;

/// <summary>Walther-style crisis pressure for AI rival synods (Decision 1).</summary>
public static class AiSynodCrisisManager
{
    struct AiSynodState
    {
        public float Adherence;
        public float SpiritualComfort;
        public float CivicRestraint;
        public int SchismPressure;
        public CrisisType ActiveCrisis;
        public int TensionTurns;
        public bool HasSplintered;
    }

    const int SchismPressureThreshold = 52;
    const int PressurePerTensionTurn = 9;

    static readonly Dictionary<SynodPlayerId, AiSynodState> states = new();

    public static CityGrowthSystem.FactionGrowthMetrics GetMetrics(SynodPlayerId playerId)
    {
        EnsureState(playerId);
        var state = states[playerId];
        return new CityGrowthSystem.FactionGrowthMetrics
        {
            Adherence = state.Adherence,
            SpiritualComfort = state.SpiritualComfort,
            CivicRestraint = state.CivicRestraint,
            UseWaltherTension = true
        };
    }

    public static void ProcessEndTurn(SynodPlayerId playerId)
    {
        if (playerId is SynodPlayerId.None or SynodPlayerId.Player1)
            return;

        var capital = CityManager.Instance?.GetSynodPlayerCapital(playerId);
        if (capital == null)
            return;

        EnsureState(playerId);
        var state = states[playerId];
        if (state.HasSplintered)
        {
            states[playerId] = state;
            return;
        }

        DriftMeters(ref state);
        var snap = CityGrowthSystem.Evaluate(capital);
        var crisis = ClassifyCrisis(snap);

        if (crisis == CrisisType.None)
        {
            state.ActiveCrisis = CrisisType.None;
            state.TensionTurns = 0;
            state.SchismPressure = Mathf.Max(0, state.SchismPressure - 4);
        }
        else
        {
            state.ActiveCrisis = crisis;
            state.TensionTurns++;
            state.SchismPressure += PressurePerTensionTurn + state.TensionTurns;
        }

        states[playerId] = state;

        if (state.SchismPressure < SchismPressureThreshold || SchismManager.Instance == null)
            return;

        if (SchismManager.Instance.TryTriggerAiSchism(capital, playerId, crisis, snap.TensionLabel))
        {
            state.HasSplintered = true;
            state.SchismPressure = 0;
            state.ActiveCrisis = CrisisType.None;
            state.TensionTurns = 0;
            states[playerId] = state;
        }
    }

    static void EnsureState(SynodPlayerId playerId)
    {
        if (states.ContainsKey(playerId))
            return;

        states[playerId] = new AiSynodState
        {
            Adherence = 42f + (int)playerId * 3f,
            SpiritualComfort = 44f,
            CivicRestraint = 58f
        };
    }

    static void DriftMeters(ref AiSynodState state)
    {
        state.CivicRestraint = Mathf.Clamp(state.CivicRestraint + Random.Range(-3f, 3f), 0f, 100f);
        state.SpiritualComfort = Mathf.Clamp(state.SpiritualComfort + Random.Range(-2f, 4f), 0f, 100f);
        state.Adherence = Mathf.Clamp(state.Adherence + Random.Range(-2f, 2f), 0f, 100f);
    }

    static CrisisType ClassifyCrisis(CityGrowthSystem.GrowthSnapshot snap)
    {
        if (snap.TensionLabel is "Legalism" or "Rigid legalism" or "Double predestination rigor" or "Rigid dissent")
            return CrisisType.Legalism;

        if (snap.TensionLabel is "Antinomian drift" or "Gospel without Law" or "Enthusiast fervor")
            return CrisisType.Antinomian;

        if (snap.TensionLabel is "Ghetto church" or "Secular prosperity" or "Memorialist drift")
            return CrisisType.DoctrinalDrift;

        if (snap.BlendedAppeal < 18f)
            return CrisisType.DoctrinalDrift;

        return CrisisType.None;
    }
}
