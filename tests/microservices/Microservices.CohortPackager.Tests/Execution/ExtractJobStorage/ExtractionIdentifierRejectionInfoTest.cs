﻿using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    public class ExtractionIdentifierRejectionInfoTest
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
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
            Assert.Throws<ArgumentException>(() => { var _ = new ExtractionIdentifierRejectionInfo(null, new Dictionary<string, int> { { "bar", 1 } }); });
            Assert.Throws<ArgumentException>(() => { var _ = new ExtractionIdentifierRejectionInfo("  ", new Dictionary<string, int> { { "bar", 1 } }); });

            // Check rejectionItems arg
            Assert.Throws<ArgumentException>(() => { var _ = new ExtractionIdentifierRejectionInfo("foo", null); });
            Assert.Throws<ArgumentException>(() => { var _ = new ExtractionIdentifierRejectionInfo("foo", new Dictionary<string, int>()); });

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
            Assert.AreEqual("Dict contains key(s) with a zero count: bar,baz", exc.Message);
        }

        #endregion
    }
}
