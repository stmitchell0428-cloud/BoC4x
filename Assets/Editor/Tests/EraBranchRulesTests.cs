using System.Collections.Generic;
using NUnit.Framework;

namespace BoC4x.Tests
{
    public class EraBranchRulesTests
    {
        [Test]
        public void ParseBranchGroup_ConfessionalTrack()
        {
            Assert.IsTrue(EraBranchRules.TryParseBranchGroup(
                "confessional:Era2-Confession",
                out var track,
                out var branchId));
            Assert.AreEqual(EraForkIntegrationTrack.Confessional, track);
            Assert.AreEqual("Era2-Confession", branchId);
        }

        [Test]
        public void ChosenSibling_WhenAugsburgUnlocked_LocksGutenberg()
        {
            var unlocked = new HashSet<ConfessionTechId> { ConfessionTechId.AugsburgConfession };
            var chosen = EraBranchRules.ChosenSiblingInBranch(unlocked, ConfessionTechId.GutenbergPress);
            Assert.AreEqual(ConfessionTechId.AugsburgConfession, chosen);
        }

        [Test]
        public void ForkPotency_ScalesNumeric_KeepsGuards()
        {
            var raw = new ConfessionModifiers { LegalismGuard = true, SiegePressureBonus = 2 };
            var scaled = EraBranchRules.ApplyForkPotency(raw, 0.5f);
            Assert.IsTrue(scaled.LegalismGuard);
            Assert.AreEqual(1, scaled.SiegePressureBonus);
        }

        [Test]
        public void ColloquyCost_ScalesByTier()
        {
            Assert.AreEqual(3, EraBranchRules.ColloquyCostForTier(2));
            Assert.AreEqual(4, EraBranchRules.ColloquyCostForTier(4));
            Assert.AreEqual(5, EraBranchRules.ColloquyCostForTier(5));
        }

        [Test]
        public void StudyColloquyCost_ScalesByTier()
        {
            Assert.AreEqual(4, EraBranchRules.StudyColloquyCostForTier(2));
            Assert.AreEqual(5, EraBranchRules.StudyColloquyCostForTier(4));
        }

        [Test]
        public void ResolveForkPotency_IntegratedOnly_IsPartial()
        {
            var unlocked = new HashSet<ConfessionTechId> { ConfessionTechId.GutenbergPress };
            var integrated = new HashSet<ConfessionTechId> { ConfessionTechId.GutenbergPress };
            var studied = new HashSet<ConfessionTechId>();

            Assert.AreEqual(
                EraBranchRules.IntegratedSiblingPotency,
                EraBranchRules.ResolveForkPotency(
                    ConfessionTechId.GutenbergPress, unlocked, integrated, studied));
        }

        [Test]
        public void ResolveForkPotency_Studied_IsDeepened()
        {
            var unlocked = new HashSet<ConfessionTechId> { ConfessionTechId.GutenbergPress };
            var integrated = new HashSet<ConfessionTechId> { ConfessionTechId.GutenbergPress };
            var studied = new HashSet<ConfessionTechId> { ConfessionTechId.GutenbergPress };

            Assert.AreEqual(
                EraBranchRules.StudiedSiblingPotency,
                EraBranchRules.ResolveForkPotency(
                    ConfessionTechId.GutenbergPress, unlocked, integrated, studied));
        }

        [Test]
        public void ResolveForkPotency_BothSiblingsUnlocked_IsFull()
        {
            var unlocked = new HashSet<ConfessionTechId>
            {
                ConfessionTechId.AugsburgConfession,
                ConfessionTechId.GutenbergPress
            };
            var integrated = new HashSet<ConfessionTechId> { ConfessionTechId.GutenbergPress };
            var studied = new HashSet<ConfessionTechId> { ConfessionTechId.GutenbergPress };

            Assert.IsTrue(EraBranchRules.BothSiblingsUnlocked(unlocked, ConfessionTechId.GutenbergPress));
            Assert.AreEqual(
                EraBranchRules.FullDualPathPotency,
                EraBranchRules.ResolveForkPotency(
                    ConfessionTechId.GutenbergPress, unlocked, integrated, studied));
        }

        [Test]
        public void AdvanceForkHint_NamesSibling_OnBothSides()
        {
            string augsburg = EraBranchRules.FormatAdvanceForkHint(ConfessionTechId.AugsburgConfession);
            string gutenberg = EraBranchRules.FormatAdvanceForkHint(ConfessionTechId.GutenbergPress);
            Assert.IsTrue(augsburg.Contains("Gutenberg") || augsburg.Contains("Printed"), augsburg);
            Assert.IsTrue(gutenberg.Contains("Augsburg"), gutenberg);

            string mission = EraBranchRules.FormatAdvanceForkHint(ConfessionTechId.MissionarySending);
            string bach = EraBranchRules.FormatAdvanceForkHint(ConfessionTechId.JohannSebastianBach);
            Assert.IsFalse(string.IsNullOrEmpty(mission));
            Assert.IsFalse(string.IsNullOrEmpty(bach));
        }
    }
}
