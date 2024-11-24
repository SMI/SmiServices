using NUnit.Framework;
using SmiServices.Common.Messages;

namespace SmiServices.IntegrationTests;

[SetUpFixture]
internal class FixtureSetup
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        UnitTests.LoggerFixture.Setup();

        MessageHeader.CurrentProgramName = nameof(IntegrationTests);
    }
}
