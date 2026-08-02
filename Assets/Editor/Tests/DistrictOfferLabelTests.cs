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
    }
}
