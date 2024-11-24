using NUnit.Framework;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;


namespace SmiServices.UnitTests.Microservices.CohortPackager.Execution.ExtractJobStorage
{
    public class ExtractionIdentifierRejectionInfoTest
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs()
        {
            // Check keyValue arg
            Assert.Throws<ArgumentException>(() => { var _ = new ExtractionIdentifierRejectionInfo("  ", new Dictionary<string, int> { { "bar", 1 } }); });

            // Check rejectionItems arg
            Assert.Throws<ArgumentException>(() => { var _ = new ExtractionIdentifierRejectionInfo("foo", []); });

            // Check empty dict key
            Assert.Throws<ArgumentException>(() => { var _ = new ExtractionIdentifierRejectionInfo("foo", new Dictionary<string, int> { { "  ", 1 } }); });

            // Check entries with 0 count
            var exc = Assert.Throws<ArgumentException>(() =>
            {
                var _ = new ExtractionIdentifierRejectionInfo(
                    "foo",
                    new Dictionary<string, int>
                    {
                        { "bar", 0 },
                        { "baz", 0 },
                    }
                );
            });
            Assert.That(exc!.Message, Is.EqualTo("Dict contains key(s) with a zero count: bar,baz"));
        }

        #endregion
    }
}
