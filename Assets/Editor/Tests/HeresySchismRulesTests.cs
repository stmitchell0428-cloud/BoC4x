using NUnit.Framework;

public class HeresySchismRulesTests
{
    [Test]
    public void PickHeresyForCrisis_KeepsCrisisFlavor_EvenWhenAlreadyActive()
    {
        var active = new[] { HeresyType.Antinomian };

        var picked = HeresyDatabase.PickHeresyForCrisis(
            CrisisType.Antinomian,
            isRepeat: true,
            active,
            HeresyPackId.FullCanon);

        Assert.AreEqual(HeresyType.Antinomian, picked);
    }

    [Test]
    public void PickHeresyForCrisis_PrefersUnusedHeresyWhenNotRepeat()
    {
        var active = new[] { HeresyType.Antinomian };

        var picked = HeresyDatabase.PickHeresyForCrisis(
            CrisisType.Antinomian,
            isRepeat: false,
            active,
            HeresyPackId.FullCanon);

        Assert.AreNotEqual(HeresyType.Antinomian, picked);
    }

    [Test]
    public void PickHeresyForCrisis_FallsBackWhenCrisisFlavorNotInPack()
    {
        var active = new[] { HeresyType.Enthusiasm };

        var picked = HeresyDatabase.PickHeresyForCrisis(
            CrisisType.Legalism,
            isRepeat: false,
            active,
            HeresyPackId.RadicalFringe);

        Assert.AreEqual(HeresyType.Sacramentarian, picked);
    }
}
