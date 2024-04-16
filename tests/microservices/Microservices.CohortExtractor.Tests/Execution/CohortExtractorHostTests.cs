using NUnit.Framework;
using Smi.Common.Tests;
using Smi.Common.Options;
using Microservices.CohortExtractor.Execution;
using FAnsi;

namespace Microservices.CohortExtractor.Tests.Execution;

[RequiresRabbit, RequiresRelationalDb(DatabaseType.MySql)]
internal class CohortExtractorHostTests
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
    public void Constructor_HappyPath()
    {
        // Arrange

        var globals = new GlobalOptionsFactory().Load(nameof(Constructor_HappyPath));

        using var tester = new MicroserviceTester(
            globals.RabbitOptions!,
            globals.CohortPackagerOptions!.FileCollectionInfoOptions!
        );
        tester.CreateExchange("TEST.ExtractFileExchange");

        var host = new CohortExtractorHost(globals, null, null);

        // Act

        host.Start();

        // TODO
    }

    #endregion
}
