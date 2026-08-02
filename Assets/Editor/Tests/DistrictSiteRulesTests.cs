using NUnit.Framework;
using UnityEngine;

namespace BoC4x.Tests
{
    public class DistrictSiteRulesTests
    {
        [Test]
        public void DistrictRange_FitsInsideCitySeparation()
        {
            // Districts must be within 3 of parent; other cities need separation 6.
            // Parent must be ignored for separation or the valid ring is empty.
            Assert.Less(3, CityManager.MinCitySeparation);
        }
    }
}
