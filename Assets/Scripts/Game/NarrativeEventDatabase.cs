using System.Collections.Generic;

public readonly struct NarrativeEventChoice
{
    public readonly string Label;
    public readonly string Description;
    public readonly float LawDelta;
    public readonly float GospelDelta;
    public readonly float AdherenceDelta;
    public readonly int FameDelta;
    public readonly int ManuscriptCost;

    public NarrativeEventChoice(
        string label,
        string description,
        float lawDelta,
        float gospelDelta,
        float adherenceDelta,
        int fameDelta = 0,
        int manuscriptCost = 0)
    {
        Label = label;
        Description = description;
        LawDelta = lawDelta;
        GospelDelta = gospelDelta;
        AdherenceDelta = adherenceDelta;
        FameDelta = fameDelta;
        ManuscriptCost = manuscriptCost;
    }
}

public readonly struct NarrativeEventEntry
{
    public readonly string Id;
    public readonly int TriggerNarrativeDay;
    public readonly int DaysAdvanceOnResolve;
    public readonly string EraLabel;
    public readonly string Title;
    public readonly string Prompt;
    public readonly string Quote;
    public readonly string SourceCitation;
    public readonly NarrativeEventChoice ChoiceA;
    public readonly NarrativeEventChoice ChoiceB;
    public readonly NarrativeEventChoice? ChoiceC;
    public readonly bool ActivatesChurchYear;
    public readonly string[] UnlockNameFragments;

    public NarrativeEventEntry(
        string id,
        int triggerNarrativeDay,
        string eraLabel,
        string title,
        string prompt,
        string quote,
        NarrativeEventChoice choiceA,
        NarrativeEventChoice choiceB,
        int daysAdvanceOnResolve = 0,
        string sourceCitation = null,
        NarrativeEventChoice? choiceC = null,
        bool activatesChurchYear = false,
        params string[] unlockNameFragments)
    {
        Id = id;
        TriggerNarrativeDay = triggerNarrativeDay;
        DaysAdvanceOnResolve = daysAdvanceOnResolve;
        EraLabel = eraLabel;
        Title = title;
        Prompt = prompt;
        Quote = quote;
        SourceCitation = sourceCitation;
        ChoiceA = choiceA;
        ChoiceB = choiceB;
        ChoiceC = choiceC;
        ActivatesChurchYear = activatesChurchYear;
        UnlockNameFragments = unlockNameFragments ?? System.Array.Empty<string>();
    }
}

/// <summary>Scripted salvation-history and Reformation narrative beats (Tier A).</summary>
public static class NarrativeEventDatabase
{
    public static readonly NarrativeEventEntry[] Events =
    {
        new(
            "creation",
            0,
            "Before all ages",
            "In the Beginning",
            "God creates by Word. How shall the synod receive ordered creation?",
            "In the beginning God created the heavens and the earth.",
            new NarrativeEventChoice("Confess the Creator", "Law +4, adherence +5", 4f, 0f, 5f),
            new NarrativeEventChoice("Pastor the wonderers", "Gospel +6, adherence +3", -2f, 6f, 3f),
            daysAdvanceOnResolve: 80,
            sourceCitation: "Genesis 1:1",
            choiceC: new NarrativeEventChoice("Study the hexameron (2 mss)", "Adherence +8, Gospel +2, -2 mss", 2f, 2f, 8f, manuscriptCost: 2)),

        new(
            "fall",
            120,
            "Paradise lost",
            "The Promise After the Fall",
            "Sin enters the world, yet a promise is spoken. What will the synod emphasize?",
            "I will put enmity between you and the woman, and between your offspring and hers.",
            new NarrativeEventChoice("Preach the curse honestly", "Law +8, adherence +4", 8f, -2f, 4f),
            new NarrativeEventChoice("Foreground the Seed", "Gospel +8, adherence +6", -3f, 8f, 6f, fameDelta: 1),
            daysAdvanceOnResolve: 120,
            sourceCitation: "Genesis 3:15"),

        new(
            "sinai",
            400,
            "Covenant at the mountain",
            "The Law at Sinai",
            "Israel receives the Law. How does the synod hold Law and Gospel together?",
            "I am the Lord your God, who brought you out of the land of Egypt.",
            new NarrativeEventChoice("Magnify the Commandments", "Law +10, adherence +5", 10f, -4f, 5f),
            new NarrativeEventChoice("Preach redemption first", "Gospel +7, adherence +7", -4f, 7f, 7f),
            daysAdvanceOnResolve: 200,
            sourceCitation: "Exodus 20:2",
            choiceC: new NarrativeEventChoice("Catechize the people (3 mss)", "Law +5, Gospel +4, adherence +9, -3 mss", 5f, 4f, 9f, manuscriptCost: 3)),

        new(
            "nativity",
            850,
            "Fulfillment of promise",
            "Word Made Flesh",
            "Christ is born. What witness will the synod bear this season?",
            "The Word became flesh and dwelt among us.",
            new NarrativeEventChoice("Keep the feast publicly", "Gospel +10, adherence +6, fame +2", -2f, 10f, 6f, fameDelta: 2),
            new NarrativeEventChoice("Quiet Bethlehem alms", "Gospel +8, adherence +4", -1f, 8f, 4f),
            daysAdvanceOnResolve: 60,
            sourceCitation: "John 1:14"),

        new(
            "passion",
            940,
            "Holy Week",
            "Passion of Our Lord",
            "Christ goes to the cross. How will the synod answer costly love?",
            "Father, forgive them, for they know not what they do.",
            new NarrativeEventChoice("Confess without flinching", "Adherence +9, Law +4, fame +2", 4f, 2f, 9f, fameDelta: 2),
            new NarrativeEventChoice("Pastor the fearful", "Gospel +9, adherence +5", -3f, 9f, 5f),
            daysAdvanceOnResolve: 7,
            sourceCitation: "Luke 23:34",
            unlockNameFragments: new[] { "St. Stephen" }),

        new(
            "easter",
            947,
            "Third day",
            "Christ Is Risen",
            "The tomb is empty. What courage will the synod carry forward?",
            "He is not here, for he has risen, as he said.",
            new NarrativeEventChoice("Proclaim resurrection boldly", "Gospel +10, adherence +8, fame +3", -2f, 10f, 8f, fameDelta: 3),
            new NarrativeEventChoice("Discipline false comfort", "Law +6, adherence +6", 6f, 0f, 6f),
            daysAdvanceOnResolve: 40,
            sourceCitation: "Matthew 28:6",
            unlockNameFragments: new[] { "Polycarp", "Ignatius" }),

        new(
            "ascension",
            995,
            "Ascension of Our Lord",
            "Receive the Great Commission",
            "The Lord ascends; the Church waits in prayer. The church-year calendar opens for this match.",
            "Go therefore and make disciples of all nations, baptizing them in the name of the Father and of the Son and of the Holy Spirit.",
            new NarrativeEventChoice("Send missionaries boldly", "Gospel +8, adherence +8, fame +2", -2f, 8f, 8f, fameDelta: 2),
            new NarrativeEventChoice("Strengthen home parishes first", "Law +6, adherence +7", 6f, 2f, 7f),
            daysAdvanceOnResolve: 50,
            sourceCitation: "Matthew 28:19",
            choiceC: new NarrativeEventChoice("Colloquy on church order (4 mss)", "Adherence +10, Gospel +5, -4 mss", 2f, 5f, 10f, manuscriptCost: 4),
            activatesChurchYear: true),

        new(
            "theses",
            1100,
            "Reformation · 1517",
            "Ninety-Five Theses",
            "Indulgences are debated at Wittenberg. How does the synod respond to fresh confession?",
            "When our Lord and Master Jesus Christ said, \"Repent,\" he willed the entire life of believers to be one of repentance.",
            new NarrativeEventChoice("Post public theses", "Law +5, adherence +9, fame +3", 5f, 2f, 9f, fameDelta: 3),
            new NarrativeEventChoice("Debate in colloquy", "Gospel +6, adherence +7", -1f, 6f, 7f),
            daysAdvanceOnResolve: 100,
            sourceCitation: "Luther, Thesis 1 (1517)",
            unlockNameFragments: new[] { "Martin Luther" }),

        new(
            "augsburg",
            1300,
            "Reformation · 1530",
            "Augsburg Confession",
            "The evangelical estates confess before the emperor. What will this synod imitate?",
            "We teach that men cannot be justified before God by their own strength, merits, or works, but freely for Christ's sake through faith.",
            new NarrativeEventChoice("Confess before rulers", "Adherence +10, Law +4, fame +4", 4f, 3f, 10f, fameDelta: 4),
            new NarrativeEventChoice("Pastor the anxious (3 mss)", "Gospel +8, adherence +8, -3 mss", -2f, 8f, 8f, manuscriptCost: 3),
            daysAdvanceOnResolve: 120,
            sourceCitation: "Augsburg Confession, Art. IV",
            unlockNameFragments: new[] { "Presentation of the Augsburg Confession", "Philip Melanchthon" }),

        new(
            "formula",
            1500,
            "Reformation · 1580",
            "Formula of Concord",
            "After controversy, concord is sought in doctrine. How will the synod hold the center?",
            "We believe, teach, and confess that the sole rule and standard of all doctrine is the prophetic and apostolic Scriptures alone.",
            new NarrativeEventChoice("Bind the Formula", "Adherence +12, Law +5", 5f, 2f, 12f, fameDelta: 2),
            new NarrativeEventChoice("Study the Epitome (4 mss)", "Gospel +6, adherence +10, -4 mss", -2f, 6f, 10f, manuscriptCost: 4),
            daysAdvanceOnResolve: 0,
            sourceCitation: "Formula of Concord, Rule and Norm",
            unlockNameFragments: new[] { "Johann Gerhard", "Johannes Bugenhagen", "Athanasius", "Ambrose", "Jerome" })
    };

    public static bool TryGetById(string id, out NarrativeEventEntry entry)
    {
        for (int i = 0; i < Events.Length; i++)
        {
            if (Events[i].Id == id)
            {
                entry = Events[i];
                return true;
            }
        }

        entry = default;
        return false;
    }

    public static string FormatBody(NarrativeEventEntry entry)
    {
        string body = $"{entry.Prompt}\n\n<i>\"{entry.Quote}\"</i>";
        if (!string.IsNullOrEmpty(entry.SourceCitation))
            body += $"\n\n<size=12><color=#8899AA><b>Source:</b> <i>{entry.SourceCitation}</i></color></size>";
        body += $"\n\n<size=12><color=#AABBCC>— {entry.EraLabel}</color></size>";
        return body;
    }
}
