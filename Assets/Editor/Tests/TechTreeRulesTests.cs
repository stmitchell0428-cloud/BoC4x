using NUnit.Framework;

namespace BoC4x.Tests
{
    public class TechTreeRulesTests
    {
        [Test]
        public void CategoryForTrack_MapsThreeQueues()
        {
            Assert.AreEqual(TechTreeCategory.Doctrine, TechTreeRules.CategoryFor(TechTrack.Doctrine));
            Assert.AreEqual(TechTreeCategory.Culture, TechTreeRules.CategoryFor(TechTrack.Culture));
            Assert.AreEqual(TechTreeCategory.Secular, TechTreeRules.CategoryFor(TechTrack.Secular));
        }

        [Test]
        public void RequiresAdherence_OnlySecularIsExempt()
        {
            Assert.IsTrue(TechTreeRules.RequiresAdherence(TechTreeCategory.Doctrine));
            Assert.IsTrue(TechTreeRules.RequiresAdherence(TechTreeCategory.Culture));
            Assert.IsFalse(TechTreeRules.RequiresAdherence(TechTreeCategory.Secular));
        }
    }
}
