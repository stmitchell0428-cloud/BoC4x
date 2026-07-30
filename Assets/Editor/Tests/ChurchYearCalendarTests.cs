using NUnit.Framework;

namespace BoC4x.Tests
{
    public class ChurchYearCalendarTests
    {
        [Test]
        public void Turn1_LandsOnStAndrew()
        {
            ChurchYearCalendar.GetCivilDateForTurn(1, out int month, out int day);
            Assert.AreEqual(11, month);
            Assert.AreEqual(30, day);

            var entry = ChurchYearCalendar.PrimaryEntryForTurn(1);
            Assert.IsTrue(entry.HasValue);
            Assert.IsTrue(entry.Value.Name.Contains("Andrew"));
        }

        [Test]
        public void DaysPerTurn_IsSynodicalMonth()
        {
            Assert.AreEqual(28, ChurchYearCalendar.DaysPerTurn);
            // ~13 turns wrap a non-leap year; turn 14 is well into the next cycle.
            ChurchYearCalendar.GetCivilDateForTurn(1, out int m1, out int d1);
            ChurchYearCalendar.GetCivilDateForTurn(2, out int m2, out int d2);
            Assert.AreEqual(11, m1);
            Assert.AreEqual(30, d1);
            // Nov 30 + 28 days → Dec 28 (Holy Innocents window).
            Assert.AreEqual(12, m2);
            Assert.AreEqual(28, d2);
        }

        [Test]
        public void ReformationDay_IsFeast()
        {
            var list = ChurchYearCalendar.EntriesFor(10, 31);
            Assert.IsNotEmpty(list);
            Assert.AreEqual(ChurchYearEntryKind.FeastOrFestival, list[0].Kind);
            Assert.IsTrue(list[0].Name.Contains("Reformation"));
        }

        [Test]
        public void WaltherCommemoration_Exists()
        {
            var list = ChurchYearCalendar.EntriesFor(5, 7);
            Assert.IsNotEmpty(list);
            Assert.IsTrue(list[0].Name.Contains("Walther"));
        }

        [Test]
        public void AugsburgConfessionPresentation_Exists()
        {
            var list = ChurchYearCalendar.EntriesFor(6, 25);
            Assert.IsNotEmpty(list);
            Assert.IsTrue(list[0].Name.Contains("Augsburg"));
        }

        [Test]
        public void EnrichEventBody_IncludesChurchYear_AndSaturationWitness()
        {
            string body = ChurchYearFlavor.EnrichEventBody("Test crisis.", saturatedEmphasis: true);
            Assert.IsTrue(body.Contains("Church Year"), body);
            Assert.IsTrue(body.Contains("Witness for this strife") || body.Contains("Witness"), body);
        }

        [Test]
        public void PrincipalFeasts_AreLsbBoldface_OneYearDates()
        {
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(12, 31)[0].IsPrincipalFeast);
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(1, 1)[0].IsPrincipalFeast);
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(2, 2)[0].IsPrincipalFeast);
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(3, 25)[0].IsPrincipalFeast);
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(6, 24)[0].IsPrincipalFeast);
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(7, 2)[0].IsPrincipalFeast);
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(9, 29)[0].IsPrincipalFeast);
            Assert.IsTrue(ChurchYearCalendar.EntriesFor(11, 1)[0].IsPrincipalFeast);

            // May 31 Visitation is 3-year; not a principal watch date.
            Assert.IsFalse(ChurchYearCalendar.EntriesFor(5, 31)[0].IsPrincipalFeast);
            Assert.IsFalse(ChurchYearCalendar.EntriesFor(10, 31)[0].IsPrincipalFeast);
        }

        [Test]
        public void PrincipalWatch_FindsFeastInsideSynodicalMonthWindow()
        {
            // Turn 2 = Dec 28; window covers Circumcision/Name (Jan 1). Prefer feast over Eve.
            Assert.IsTrue(ChurchYearCalendar.TryGetPrincipalWatchForTurn(2, out var feast));
            Assert.IsTrue(feast.Name.Contains("Circumcision") || feast.Name.Contains("Name of Jesus"), feast.Name);
            Assert.IsFalse(feast.Name.StartsWith("Eve of"), feast.Name);
        }

        [Test]
        public void PrincipalWatch_JohnAndVisitation_ShareSummerWindow()
        {
            // Find a turn whose window contains both Jun 24 and Jul 2.
            bool found = false;
            for (int turn = 1; turn <= 14; turn++)
            {
                if (!ChurchYearCalendar.TryGetPrincipalWatchesForTurn(turn, out var primary, out var secondary))
                    continue;
                bool hasJohn = primary.Name.Contains("John the Baptist") ||
                               (secondary.HasValue && secondary.Value.Name.Contains("John the Baptist"));
                bool hasVisit = primary.Name.Contains("Visitation") ||
                                (secondary.HasValue && secondary.Value.Name.Contains("Visitation"));
                if (hasJohn && hasVisit)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "Expected a synodical-month watch covering both John and Visitation.");
        }
    }
}
