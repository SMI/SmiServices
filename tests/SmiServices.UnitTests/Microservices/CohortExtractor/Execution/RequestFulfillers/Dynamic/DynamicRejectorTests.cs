using NUnit.Framework;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers.Dynamic;
using SmiServices.UnitTests.Common;
using System.IO;
using System.IO.Abstractions.TestingHelpers;

namespace SmiServices.UnitTests.Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic;

public class DynamicRejectorTests

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
    public void Constructor_MissingRulesFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => new DynamicRejector("foo.txt", new MockFileSystem()));
    }

    [Test]
    public void Constructor_ExistsWithNoArgs()
    {
        _ = new DynamicRejector();
    }

    [Test]
    public void Constructor_UsesStaticRulesPath()
    {
        DynamicRejector.DefaultDynamicRulesPath = "someFile.txt";

        var exc = Assert.Throws<FileNotFoundException>(() => new DynamicRejector());
        Assert.That(exc?.Message, Is.EqualTo("Could not find rules file 'someFile.txt'"));
    }

    #endregion
}
