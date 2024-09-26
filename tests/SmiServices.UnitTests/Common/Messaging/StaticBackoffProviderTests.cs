using NUnit.Framework;
using SmiServices.Common.Messaging;
using System;

namespace SmiServices.UnitTests.Common.Messaging;
internal class StaticBackoffProviderTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestLogger.Setup();
    }

    [Test]
    public void Constructor_WithTimeSpan_IsSet()
    {
        var provider = new StaticBackoffProvider(new TimeSpan(1, 2, 3));

        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(1, 2, 3)));
    }

    [Test]
    public void Constructor_WithNoTimeSpan_UsesDefault()
    {
        var provider = new StaticBackoffProvider();

        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(0, 1, 0)));
    }

    [Test]
    public void GetNextBackoff_ReturnsStaticTimeSpan()
    {
        var provider = new StaticBackoffProvider(new TimeSpan(1));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(1)));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(1)));
    }
}
