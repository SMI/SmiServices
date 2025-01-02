
using NUnit.Framework;
using SmiServices.Microservices.DicomReprocessor;
using System;

namespace SmiServices.UnitTests.Microservices.DicomReprocessor;

public class DicomReprocessorCliOptionsTests
{

    #region Fixture Methods 

    [OneTimeSetUp]
    public void OneTimeSetUp() { }

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
    public void TestInvalidCollectionArgument()
    {
        Assert.Throws<ArgumentException>(() =>
            new DicomReprocessorCliOptions { SourceCollection = "database.collection" }
        );
    }

    #endregion

}
