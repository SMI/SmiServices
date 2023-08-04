using Microservices.DicomAnonymiser.Anonymisers;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;

namespace Microservices.DicomAnonymiser.Tests.Anonymisers
{
    public class AnonymiserFactoryTests
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
        public void CreateAnonymiser_InvalidAnonymiserName_ThrowsException()
        {
            var e = Assert.Throws<ArgumentException>(() =>
            {
                AnonymiserFactory.CreateAnonymiser(new DicomAnonymiserOptions { AnonymiserType = "whee" });
            });
            Assert.AreEqual(e!.Message, "Could not parse 'whee' to a valid AnonymiserType");
        }

        [Test]
        public void CreateAnonymiser_NoCaseForAnonymiser_ThrowsException()
        {
            var e = Assert.Throws<NotImplementedException>(() =>
            {
                AnonymiserFactory.CreateAnonymiser(new DicomAnonymiserOptions { AnonymiserType = "None" });
            });
            Assert.AreEqual(e!.Message, "No case for AnonymiserType 'None'");
        }

        #endregion
    }
}
