using NUnit.Framework;

namespace BoC4x.Tests
{
    public class DualProductionTrackTests
    {
        [Test]
        public void UsesProduction_SplitsTimerFromProd()
        {
            Assert.IsFalse(CityBuildDatabase.Get(CityBuildId.TrainSoldier).UsesProduction);
            Assert.IsTrue(CityBuildDatabase.Get(CityBuildId.BuildPrintingPress).UsesProduction);
            Assert.IsTrue(CityBuildDatabase.Get(CityBuildId.BuildWharf).UsesProduction);
            Assert.IsTrue(CityBuildDatabase.Get(CityBuildId.BuildDock).UsesProduction);
        }

        [Test]
        public void EarlyNaval_UsesTier2WharvesTech()
        {
            Assert.AreEqual(
                ConfessionTechId.CoastalWharves,
                CityBuildDatabase.Get(CityBuildId.BuildWharf).RequiredTech);
            Assert.AreEqual(
                ConfessionTechId.CoastalWharves,
                CityBuildDatabase.Get(CityBuildId.TrainCoastalExplorer).RequiredTech);
            Assert.AreEqual(
                ConfessionTechId.NavalWarfare,
                CityBuildDatabase.Get(CityBuildId.BuildDock).RequiredTech);
            Assert.AreEqual(
                ConfessionTechId.OpenOceanNavigation,
                CityBuildDatabase.Get(CityBuildId.TrainDeepSeaShip).RequiredTech);
        }

        [Test]
        public void Dock_IsProductionTrack()
        {
            var dock = CityBuildDatabase.Get(CityBuildId.BuildDock);
            Assert.Greater(dock.ProductionCost, 0);
            Assert.AreEqual(0, dock.ManuscriptCost);
        }
    }
}
