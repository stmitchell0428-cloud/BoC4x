using NUnit.Framework;

namespace BoC4x.Tests
{
    public class CityFoundingFoodTests
    {
        [Test]
        public void FoundingCapitalPopulation_SupportsFiveWorkers()
        {
            int workers = CityGrowthSystem.FoundingCapitalPopulation / CityGrowthSystem.WorkerPopulationDivisor;
            Assert.GreaterOrEqual(workers, 5);
        }

        [Test]
        public void CapitalUrbanBaseline_ExceedsFrontierBaseline()
        {
            Assert.Greater(CityGrowthSystem.CapitalUrbanFoodBaseline, CityGrowthSystem.UrbanFoodBaseline);
        }

        [Test]
        public void CapitalDeficitGrace_AllowsEarlyBuildWindow()
        {
            Assert.GreaterOrEqual(CityGrowthSystem.CapitalDeficitGraceTurns, 8);
        }
    }
}
