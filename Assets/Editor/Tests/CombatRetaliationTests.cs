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
        // 10/30 HP → weight capped by hp share (exact kill, absorbed share = 1).
        float weight = CombatSystem.RetaliationWeightForKill(10, 30, 10);
        Assert.AreEqual(10f / 30f, weight, 0.01f);
        Assert.Less(weight, CombatSystem.RetaliationWeightForKill(30, 30, 10));
    }
}
