using NUnit.Framework;
using UnityEngine;

public class NarrativeChronologyTests
{
    GameObject root;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("NarrativeChronologyTests");
        root.AddComponent<MatchNarrativeChronology>();
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [Test]
    public void SalvationHistory_StartsBeforeChurchYear()
    {
        Assert.AreEqual(NarrativeChronologyPhase.SalvationHistory, MatchNarrativeChronology.Instance.Phase);
        Assert.IsFalse(ChurchYearCalendar.IsChurchYearActive);
    }

    [Test]
    public void Ascension_ActivatesChurchYearAndUnlocksPrincipalFeasts()
    {
        Assert.IsTrue(NarrativeEventDatabase.TryGetById("ascension", out var ascension));
        MatchNarrativeChronology.Instance.ResolveEvent(ascension, turn: 12);

        Assert.AreEqual(NarrativeChronologyPhase.ChurchYear, MatchNarrativeChronology.Instance.Phase);
        Assert.AreEqual(12, MatchNarrativeChronology.Instance.ChurchYearStartTurn);
        Assert.IsTrue(ChurchYearCalendar.IsChurchYearActive);
        Assert.IsTrue(ChurchYearCalendar.TryGetDecisionFeastForTurn(1, out _));
    }

    [Test]
    public void ProgressiveUnlock_ReformationEventAddsLutherCommemoration()
    {
        MatchNarrativeChronology.Instance.ActivateChurchYear(10);
        Assert.IsTrue(NarrativeEventDatabase.TryGetById("theses", out var theses));
        MatchNarrativeChronology.Instance.ResolveEvent(theses, 15);

        ChurchYearCalendar.GetCivilDateForTurn(15, out _, out _);
        bool found = false;
        foreach (var pair in ChurchYearCalendar.AllByDate)
        {
            foreach (var entry in pair.Value)
            {
                if (entry.Name.Contains("Martin Luther") &&
                    MatchNarrativeChronology.Instance.IsCommemorationUnlocked(entry))
                    found = true;
            }
        }

        Assert.IsTrue(found);
    }

    [Test]
    public void NarrativeAdvance_QueuesCreationAfterFirstTurn()
    {
        MatchNarrativeChronology.Instance.AdvanceForTurn(1);
        Assert.IsTrue(MatchNarrativeChronology.Instance.TryGetNextDueEvent(out var entry));
        Assert.AreEqual("creation", entry.Id);
    }
}
