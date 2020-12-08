﻿using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
using Smi.Common.Tests;
using System;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    public class VerificationFailureInfoTest
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

        [TestCase(null, "bar")]
        [TestCase("  ", "bar")]
        [TestCase("foo", null)]
        [TestCase("foo", "  ")]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(string anonFilePath, string failureData)
        {
            Assert.Throws<ArgumentException>(() => { var _ = new FileVerificationFailureInfo(anonFilePath, failureData); });
        }

        #endregion
    }
}
