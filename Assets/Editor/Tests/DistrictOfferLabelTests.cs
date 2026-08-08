using NUnit.Framework;

namespace BoC4x.Tests
{
    public class DistrictOfferLabelTests
    {
        [Test]
        public void MigrationAppealThreshold_IsEighteen()
        {
            Assert.AreEqual(18f, CityGrowthSystem.MigrationAppealThreshold);
        }

        [Test]
        public void DistrictPacing_RequiresStreakAndAge()
        {
            Assert.GreaterOrEqual(CityGrowthSystem.MinSurplusStreakForDistrict, 2);
            Assert.GreaterOrEqual(CityGrowthSystem.MinTurnsBeforeDistrictOffer, 8);
            Assert.GreaterOrEqual(CityGrowthSystem.MaxDistrictsPerCity, 6);
        }

        [Test]
        public void MeetsDistrictFoodGate_PositiveSurplus()
        {
            var snap = new CityGrowthSystem.GrowthSnapshot { FoodSurplus = 2, HousingRoom = 5 };
            Assert.IsTrue(CityGrowthSystem.MeetsDistrictFoodGate(snap));
        }

        [Test]
        public void MeetsDistrictFoodGate_BreakEvenWithFullHousing()
        {
            var snap = new CityGrowthSystem.GrowthSnapshot { FoodSurplus = 0, HousingRoom = 0 };
            Assert.IsTrue(CityGrowthSystem.MeetsDistrictFoodGate(snap));
        }

        [Test]
        public void MeetsDistrictFoodGate_BreakEvenWithHousingRoom_Fails()
        {
            var snap = new CityGrowthSystem.GrowthSnapshot { FoodSurplus = 0, HousingRoom = 3 };
            Assert.IsFalse(CityGrowthSystem.MeetsDistrictFoodGate(snap));
        }

        [Test]
        public void MeetsDistrictFoodGate_Deficit_Fails()
        {
            var snap = new CityGrowthSystem.GrowthSnapshot { FoodSurplus = -1, HousingRoom = 0 };
            Assert.IsFalse(CityGrowthSystem.MeetsDistrictFoodGate(snap));
        }

        [Test]
        public void CapitalUrbanFood_ExceedsUrbanBaseline()
        {
            Assert.Greater(CityGrowthSystem.CapitalUrbanFoodBaseline, CityGrowthSystem.UrbanFoodBaseline);
            Assert.GreaterOrEqual(CityGrowthSystem.CapitalUrbanFoodBaseline, 10);
        }
    }
}
