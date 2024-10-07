using NUnit.Framework;
using SmiServices.Common.Messages;

namespace SmiServices.UnitTests;

[SetUpFixture]
internal class FixtureSetup
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        MessageHeader.CurrentProgramName = nameof(UnitTests);
    }
}
