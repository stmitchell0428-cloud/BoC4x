using System.Collections.Generic;
using NUnit.Framework;

namespace BoC4x.Tests
{
    /// <summary>Automated gates for tier-2 confessional emphasis (scout vs combat).</summary>
    public class EmphasisGateTests
    {
        readonly HashSet<SchismaticBlocId> activeBlocs = new();
        readonly HashSet<SchismaticBlocId> scoutContacts = new();

        [SetUp]
        public void SetUp()
        {
            activeBlocs.Clear();
            scoutContacts.Clear();
        }

        [Test]
        public void NoSchism_NoExternalEmphasis()
        {
            Assert.IsFalse(CanOfferAugsburg());
            Assert.IsFalse(CanOfferSmalcald(0, hasSchism: false));
        }

        [Test]
        public void SchismWithScoutContact_OffersAugsburgOnly()
        {
            activeBlocs.Add(SchismaticBlocId.Bloc1);
            scoutContacts.Add(SchismaticBlocId.Bloc1);

            Assert.IsTrue(CanOfferAugsburg());
            Assert.IsFalse(CanOfferSmalcald(0, hasSchism: true));
        }

        [Test]
        public void SchismWithSchismaticCombat_OffersSmalcaldOnly()
        {
            activeBlocs.Add(SchismaticBlocId.Bloc1);

            Assert.IsFalse(CanOfferAugsburg());
            Assert.IsTrue(CanOfferSmalcald(1, hasSchism: true));
        }

        [Test]
        public void SchismWithScoutAndCombat_OffersBoth()
        {
            activeBlocs.Add(SchismaticBlocId.Bloc1);
            scoutContacts.Add(SchismaticBlocId.Bloc1);

            Assert.IsTrue(CanOfferAugsburg());
            Assert.IsTrue(CanOfferSmalcald(1, hasSchism: true));
        }

        [Test]
        public void SchismaticCombatRequiredForSmalcald_NotGenericCombat()
        {
            activeBlocs.Add(SchismaticBlocId.Bloc1);

            Assert.IsFalse(CanOfferSmalcald(0, hasSchism: true));
        }

        [Test]
        public void ScoutContactOnInactiveBloc_DoesNotOfferAugsburg()
        {
            scoutContacts.Add(SchismaticBlocId.Bloc1);

            Assert.IsFalse(CanOfferAugsburg());
        }

        bool CanOfferAugsburg() =>
            EmphasisGateRules.CanOfferAugsburgConfessionalEmphasis(
                activeBlocs.Count > 0,
                scoutContacts,
                blocId => activeBlocs.Contains(blocId));

        static bool CanOfferSmalcald(int schismaticCombats, bool hasSchism) =>
            EmphasisGateRules.CanOfferSmalcaldConfessionalEmphasis(hasSchism, schismaticCombats);
    }
}
