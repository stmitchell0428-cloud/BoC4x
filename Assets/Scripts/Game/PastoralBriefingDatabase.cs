using System.Collections.Generic;
using UnityEngine;

public readonly struct PastoralBriefingChoice
{
    public readonly string Label;
    public readonly string Description;
    public readonly float LawDelta;
    public readonly float GospelDelta;
    public readonly float AdherenceDelta;
    public readonly int FameDelta;
    public readonly int ManuscriptCost;
    public readonly bool ReinforceRivalBloc;

    public PastoralBriefingChoice(
        string label,
        string description,
        float lawDelta,
        float gospelDelta,
        float adherenceDelta,
        int fameDelta = 0,
        int manuscriptCost = 0,
        bool reinforceRivalBloc = false)
    {
        Label = label;
        Description = description;
        LawDelta = lawDelta;
        GospelDelta = gospelDelta;
        AdherenceDelta = adherenceDelta;
        FameDelta = fameDelta;
        ManuscriptCost = manuscriptCost;
        ReinforceRivalBloc = reinforceRivalBloc;
    }
}

public readonly struct PastoralBriefingEntry
{
    public readonly PastoralBriefingSituation Situation;
    public readonly string Author;
    public readonly string Lifespan;
    public readonly string Quote;
    public readonly string Prompt;
    public readonly PastoralBriefingChoice ChoiceA;
    public readonly PastoralBriefingChoice ChoiceB;

    public PastoralBriefingEntry(
        PastoralBriefingSituation situation,
        string author,
        string lifespan,
        string quote,
        string prompt,
        PastoralBriefingChoice choiceA,
        PastoralBriefingChoice choiceB)
    {
        Situation = situation;
        Author = author;
        Lifespan = lifespan;
        Quote = quote;
        Prompt = prompt;
        ChoiceA = choiceA;
        ChoiceB = choiceB;
    }
}

/// <summary>Historical Law/Gospel quotes with paired pastoral responses.</summary>
public static class PastoralBriefingDatabase
{
    static readonly PastoralBriefingEntry[] Entries =
    {
        new(
            PastoralBriefingSituation.LawHeavy,
            "Martin Luther",
            "1483-1546",
            "The law says, 'Do this,' and it is never done. The gospel says, 'Believe this,' and behold, it is already done.",
            "Delegates press you: has discipline outrun comfort in the synod?",
            new PastoralBriefingChoice(
                "Preach the Gospel",
                "Gospel +10, Law -8, adherence +3",
                -8f, 10f, 3f),
            new PastoralBriefingChoice(
                "Hold the Law",
                "Law +6, Gospel -4, adherence +5",
                6f, -4f, 5f)),

        new(
            PastoralBriefingSituation.GospelHeavy,
            "C. F. W. Walther",
            "1811-1887",
            "Every person needs both the thunder of Sinai and the comfort of Calvary.",
            "Some warn that comfort is outpacing confession in your preaching.",
            new PastoralBriefingChoice(
                "Thunder of Sinai",
                "Law +12, Gospel -6, adherence +6",
                12f, -6f, 6f),
            new PastoralBriefingChoice(
                "Comfort the weary",
                "Gospel +8, Law -5, adherence +2",
                -5f, 8f, 2f)),

        new(
            PastoralBriefingSituation.AdherenceLow,
            "Martin Chemnitz",
            "1522-1586",
            "The distinction between Law and Gospel is the supreme art of a theologian.",
            "Adherence is slipping. The synod asks which word must be recovered first.",
            new PastoralBriefingChoice(
                "Confess the Creed",
                "Adherence +10, Law +4, Gospel +4",
                4f, 4f, 10f, fameDelta: 2),
            new PastoralBriefingChoice(
                "Pastoral counsel",
                "Gospel +10, adherence +6, Law -3",
                -3f, 10f, 6f)),

        new(
            PastoralBriefingSituation.Balanced,
            "Philipp Melanchthon",
            "1497-1560",
            "The law was given that sin might be known; the gospel was given that sin might be forgiven.",
            "A calm week in synod — how will you steer the dialectic?",
            new PastoralBriefingChoice(
                "Strengthen discipline",
                "Law +7, Gospel -2, adherence +4",
                7f, -2f, 4f),
            new PastoralBriefingChoice(
                "Widen mercy",
                "Gospel +7, Law -2, adherence +3",
                -2f, 7f, 3f)),

        new(
            PastoralBriefingSituation.LawHeavy,
            "Johann Gerhard",
            "1582-1637",
            "The law is a hammer that shatters self-righteousness and drives the sinner to Christ.",
            "Gerhard's voice is invoked against creeping legalism in parish life.",
            new PastoralBriefingChoice(
                "Shatter pride",
                "Law +5, Gospel +6, adherence +4",
                5f, 6f, 4f),
            new PastoralBriefingChoice(
                "Soften the hammer",
                "Law -10, Gospel +5, adherence -2",
                -10f, 5f, -2f)),

        new(
            PastoralBriefingSituation.GospelHeavy,
            "St. Augustine",
            "354-430",
            "God gives where he finds empty hands — not where he finds full ones.",
            "A delegate warns against preaching that flatters without convicting.",
            new PastoralBriefingChoice(
                "Empty the hands",
                "Law +8, Gospel +2, adherence +5",
                8f, 2f, 5f),
            new PastoralBriefingChoice(
                "Lift the fallen",
                "Gospel +10, Law -6, adherence +1",
                -6f, 10f, 1f)),

        new(
            PastoralBriefingSituation.GospelHeavy,
            "Martin Luther",
            "1483-1546",
            "Sin boldly, but believe in Christ more boldly still.",
            "The quote is misused in taverns. How do you answer from the pulpit?",
            new PastoralBriefingChoice(
                "Rebuke misuse",
                "Law +10, Gospel -3, adherence +8",
                10f, -3f, 8f, fameDelta: 3),
            new PastoralBriefingChoice(
                "Pastor the weak",
                "Gospel +6, Law -4, adherence +4",
                -4f, 6f, 4f)),

        new(
            PastoralBriefingSituation.AdherenceLow,
            "St. John Chrysostom",
            "349-407",
            "The devil is never nearer than when we are overconfident.",
            "Doctrinal drift has made the synod careless.",
            new PastoralBriefingChoice(
                "Restore confession",
                "Law +6, adherence +9, Gospel -2",
                6f, -2f, 9f),
            new PastoralBriefingChoice(
                "Gentle recall",
                "Gospel +6, adherence +5, Law -3",
                -3f, 6f, 5f)),

        new(
            PastoralBriefingSituation.SchismSaturation,
            "Martin Luther",
            "1483-1546",
            "Peace if possible, truth at all costs.",
            "Three sisters in error already stand abroad. Living with them is the trial now — not founding a fourth capital.",
            new PastoralBriefingChoice(
                "Colloquy (pay 3 mss)",
                "Law +4, Gospel +4, adherence +6, -3 mss",
                4f, 4f, 6f, manuscriptCost: 3),
            new PastoralBriefingChoice(
                "Hard rebuke",
                "Law +8, Gospel -5, adherence +3, pop strain",
                8f, -5f, 3f)),

        new(
            PastoralBriefingSituation.SchismSaturation,
            "C. F. W. Walther",
            "1811-1887",
            "The true knowledge of the distinction between the Law and the Gospel is the key to Holy Scripture.",
            "Union strife rises when three dissenting synods press the land. Preach with sharper distinction — or they will.",
            new PastoralBriefingChoice(
                "Sharpen distinction",
                "Law +5, Gospel +5, adherence +8",
                5f, 5f, 8f, fameDelta: 4),
            new PastoralBriefingChoice(
                "Feed an existing bloc",
                "Law -4, Gospel +6, adherence -3 (rival grows)",
                -4f, 6f, -3f, reinforceRivalBloc: true)),

        new(
            PastoralBriefingSituation.Wilderness,
            "Martin Luther",
            "1483-1546",
            "Affliction is the very best book in my library.",
            "Hardship on the frontier tests whether Law or Gospel will anchor the encampment.",
            new PastoralBriefingChoice(
                "Discipline the camp",
                "Law +9, Gospel -3, adherence +5",
                9f, -3f, 5f),
            new PastoralBriefingChoice(
                "Comfort exiles",
                "Gospel +9, Law -4, adherence +3",
                -4f, 9f, 3f)),

        new(
            PastoralBriefingSituation.Nomadic,
            "St. Basil the Great",
            "329-379",
            "The bread you store up belongs to the hungry.",
            "The wandering synod debates whether rigor or mercy should mark the next settlement.",
            new PastoralBriefingChoice(
                "Order before founding",
                "Law +8, adherence +6, Gospel -2",
                8f, -2f, 6f),
            new PastoralBriefingChoice(
                "Mercy on the road",
                "Gospel +8, adherence +3, Law -4",
                -4f, 8f, 3f)),

        new(
            PastoralBriefingSituation.Balanced,
            "C. F. W. Walther",
            "1811-1887",
            "The Law must be preached to the secure; the Gospel to the terrified.",
            "Pastors report mixed congregations — sinners and self-righteous in the same pews.",
            new PastoralBriefingChoice(
                "Convict the secure",
                "Law +10, Gospel -4, adherence +5",
                10f, -4f, 5f),
            new PastoralBriefingChoice(
                "Comfort the terrified",
                "Gospel +10, Law -4, adherence +5",
                -4f, 10f, 5f)),

        new(
            PastoralBriefingSituation.LawHeavy,
            "Philipp Melanchthon",
            "1497-1560",
            "We teach that the law is not abolished, but that its terror is ended for believers.",
            "Some demand you ease discipline; others fear antinomian drift.",
            new PastoralBriefingChoice(
                "End the terror",
                "Gospel +8, Law -7, adherence +4",
                -7f, 8f, 4f),
            new PastoralBriefingChoice(
                "Keep good order",
                "Law +6, Gospel -2, adherence +6",
                6f, -2f, 6f)),
    };

    public static PastoralBriefingEntry PickForSituation(
        PastoralBriefingSituation situation,
        HashSet<int> recentlyUsedIndices,
        out int entryIndex)
    {
        var pool = new List<int>();
        for (int i = 0; i < Entries.Length; i++)
        {
            if (recentlyUsedIndices != null && recentlyUsedIndices.Contains(i))
                continue;
            if (Entries[i].Situation == situation || Entries[i].Situation == PastoralBriefingSituation.Any)
                pool.Add(i);
        }

        if (pool.Count == 0)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (recentlyUsedIndices == null || !recentlyUsedIndices.Contains(i))
                    pool.Add(i);
            }
        }

        if (pool.Count == 0)
        {
            entryIndex = Random.Range(0, Entries.Length);
            return Entries[entryIndex];
        }

        entryIndex = pool[Random.Range(0, pool.Count)];
        return Entries[entryIndex];
    }

    public static string FormatBody(PastoralBriefingEntry entry)
    {
        string body =
            $"{entry.Prompt}\n\n" +
            $"<i>\"{entry.Quote}\"</i>\n\n" +
            $"<size=13><color=#AABBCC>— {entry.Author} ({entry.Lifespan})</color></size>\n\n" +
            "<size=12><color=#8899AA>Your response shapes Law, Gospel, and adherence this season.</color></size>";
        return ChurchYearFlavor.EnrichPastoralBody(body);
    }
}
