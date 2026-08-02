using NUnit.Framework;

namespace BoC4x.Tests
{
    public class CityLoyaltyRecoveryTests
    {
        [Test]
        public void ClergyRecovery_RanksAboveBase()
        {
            Assert.Greater(CityLoyaltySystem.GetClergyLoyaltyRecovery(UnitType.Pastor), CityLoyaltySystem.BaseLoyaltyRecovery);
            Assert.Greater(
                CityLoyaltySystem.GetClergyLoyaltyRecovery(UnitType.Archbishop),
                CityLoyaltySystem.GetClergyLoyaltyRecovery(UnitType.Pastor));
            Assert.Greater(
                CityLoyaltySystem.GetClergyLoyaltyRecovery(UnitType.Bishop),
                CityLoyaltySystem.GetClergyLoyaltyRecovery(UnitType.Chaplain));
        }

        [Test]
        public void MartialUnits_DoNotGrantClergyRecovery()
        {
            Assert.AreEqual(0f, CityLoyaltySystem.GetClergyLoyaltyRecovery(UnitType.Soldier));
        }
    }
}
