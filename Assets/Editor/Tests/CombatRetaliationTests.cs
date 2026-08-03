using NUnit.Framework;

public class CombatRetaliationTests
{
    [Test]
    public void KillBlow_OneHpDefender_HasNegligibleRetaliationWeight()
    {
        float weight = CombatSystem.RetaliationWeightForKill(1, 14, 12);
        Assert.Less(weight, 0.1f);
    }

    [Test]
    public void KillBlow_FullStrengthDefender_KeepsMeaningfulRetaliationWeight()
    {
        float weight = CombatSystem.RetaliationWeightForKill(30, 30, 15);
        Assert.GreaterOrEqual(weight, 0.9f);
    }

    [Test]
    public void KillBlow_WoundedDefender_ScalesRetaliationDown()
    {
        float weight = CombatSystem.RetaliationWeightForKill(10, 30, 10);
        Assert.AreEqual(1f, weight, 0.01f);
    }
}
