using NUnit.Framework;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.UnitTests.Common;
using System;


namespace SmiServices.UnitTests.Microservices.CohortPackager.Execution.ExtractJobStorage;

public class VerificationFailureInfoTest
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

    [TestCase("  ", "bar")]
    [TestCase("foo", "  ")]
    public void Constructor_ThrowsArgumentException_OnInvalidArgs(string anonFilePath, string failureData)
    {
        Assert.Throws<ArgumentException>(() => { var _ = new FileVerificationFailureInfo(anonFilePath, failureData); });
    }

    #endregion
}
