using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
using Smi.Common.Tests;
using System;

namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    public class CompletedExtractJobInfoTest
    {
        private readonly TestDateTimeProvider _dateTimeProvider = new();

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
        public void Equality()
        {
            var guid = Guid.NewGuid();
            var info1 = new CompletedExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );
            var info2 = new CompletedExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );

            Assert.That(info2, Is.EqualTo(info1));
        }

        [Test]
        public void Test_GetHashCode()
        {
            var guid = Guid.NewGuid();
            var info1 = new CompletedExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );
            var info2 = new CompletedExtractJobInfo(
                guid,
                _dateTimeProvider.UtcNow(),
                _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
                "1234",
                "test/directory",
                "KeyTag",
                123,
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
                );

            Assert.That(info2.GetHashCode(), Is.EqualTo(info1.GetHashCode()));
        }

        #endregion
    }
}
