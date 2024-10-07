using NUnit.Framework;
using SmiServices.Common.Messages;

namespace SmiServices.IntegrationTests;

[SetUpFixture]
internal class FixtureSetup
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        MessageHeader.CurrentProgramName = nameof(IntegrationTests);
    }
}
