using System;
using System.Collections.Generic;

/// <summary>Salvation-history intro beats with optional Law/Gospel choices at match start.</summary>
public static class SalvationHistoryDatabase
{
    public readonly struct Choice
    {
        public readonly string Label;
        public readonly string Description;
        public readonly Action Apply;

        public Choice(string label, string description, Action apply)
        {
            Label = label;
            Description = description;
            Apply = apply;
        }
    }

    public readonly struct Beat
    {
        public readonly int NarrativeDay;
        public readonly string Title;
        public readonly string Body;
        public readonly string ScriptureRef;
        public readonly IReadOnlyList<Choice> Choices;

        public bool HasChoices => Choices != null && Choices.Count > 0;

        public Beat(int narrativeDay, string title, string body, string scriptureRef, IReadOnlyList<Choice> choices)
        {
            NarrativeDay = narrativeDay;
            Title = title;
            Body = body;
            ScriptureRef = scriptureRef;
            Choices = choices ?? Array.Empty<Choice>();
        }
    }

    static readonly Beat[] IntroBeats =
    {
        new(
            18,
            "In the Beginning",
            "God creates by Word. How shall the synod receive ordered creation?",
            "Genesis 1:1",
            new List<Choice>
            {
                new(
                    "Confess the Creator",
                    "Law +4, adherence +5",
                    () =>
                    {
                        ApplyLaw(4f);
                        ApplyAdherence(5f);
                    }),
                new(
                    "Pastor the wonderers",
                    "Gospel +6, adherence +3",
                    () =>
                    {
                        ApplyGospel(6f);
                        ApplyAdherence(3f);
                    }),
                new(
                    "Study the hexameron (2 mss)",
                    "Adherence +8, Gospel +2, −2 mss  —  six days of creation taught from Scripture",
                    () =>
                    {
                        var faction = FirstSteps.Instance;
                        if (faction == null || faction.ScriptureManuscripts < 2)
                        {
                            UnityEngine.Debug.LogWarning("Need 2 manuscripts to study the hexameron.");
                            return;
                        }

                        faction.ScriptureManuscripts -= 2;
                        ApplyAdherence(8f);
                        ApplyGospel(2f);
                    })
            })
    };

    public static int IntroBeatCount => IntroBeats.Length;

    public static Beat GetIntroBeat(int index)
    {
        int i = index < 0 ? 0 : index >= IntroBeats.Length ? IntroBeats.Length - 1 : index;
        return IntroBeats[i];
    }

    public static int NarrativeDayForIntroBeat(int index) => GetIntroBeat(index).NarrativeDay;

    static void ApplyLaw(float delta)
    {
        var faction = FirstSteps.Instance;
        if (faction == null) return;
        faction.AdjustCivicRestraint(delta);
        faction.RefreshDashboard();
    }

    static void ApplyGospel(float delta)
    {
        FirstSteps.Instance?.AdjustSpiritualComfort(delta);
        FirstSteps.Instance?.RefreshDashboard();
    }

    static void ApplyAdherence(float delta)
    {
        FirstSteps.Instance?.AdjustConfessionalAdherence(delta);
        FirstSteps.Instance?.RefreshDashboard();
    }
}
