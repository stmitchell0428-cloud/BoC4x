using NUnit.Framework;

namespace BoC4x.Tests
{
    public class EmphasisDocumentRulesTests
    {
        [Test]
        public void UnlinkedDocument_AlwaysFullPotency()
        {
            Assert.AreEqual(1f, EmphasisDocumentRules.DocumentPotencyFor(ConfessionTechId.VerbalInspiration));
        }

        [Test]
        public void LinkedDocument_WithoutManager_HalfPotency()
        {
            Assert.AreEqual(
                EmphasisDocumentRules.UnmatchedDocumentPotency,
                EmphasisDocumentRules.DocumentPotencyFor(ConfessionTechId.FormulaOfConcord));
        }

        [Test]
        public void ApplyDocumentPotency_KeepsGuardsAtFull()
        {
            var raw = new ConfessionModifiers { LegalismGuard = true, AdherenceDecayMultiplier = 0.9f };
            var scaled = EmphasisDocumentRules.ApplyDocumentPotency(raw, 0.5f);

            Assert.IsTrue(scaled.LegalismGuard);
            // Lerp(1 → 0.9, 0.5) ≈ 0.95 — use delta (exact GreaterOrEqual vs 0.95f flakes on float).
            Assert.AreEqual(0.95f, scaled.AdherenceDecayMultiplier, 0.001f);
        }

        [Test]
        public void WildernessCap_TruncatesExcess()
        {
            var mods = new ConfessionModifiers { WildernessManuscriptBonus = 5 };
            EmphasisDocumentRules.CapWildernessManuscriptBonus(mods);
            Assert.AreEqual(EmphasisDocumentRules.WildernessManuscriptCap, mods.WildernessManuscriptBonus);
        }

        [Test]
        public void FormulaDocument_ModifiersMatchSplit()
        {
            var mod = ConfessionModifiers.ForTech(ConfessionTechId.FormulaOfConcord);
            Assert.AreEqual(0.9f, mod.AdherenceDecayMultiplier, 0.001f);
            Assert.IsFalse(mod.AntinomianGuard);
        }

        [Test]
        public void AugsburgDocument_ModifiersMatchSplit()
        {
            var mod = ConfessionModifiers.ForTech(ConfessionTechId.AugsburgConfession);
            Assert.AreEqual(1, mod.SiegePressureBonus);
            Assert.AreEqual(0, mod.SoldierAttackBonus);
        }
    }
}
