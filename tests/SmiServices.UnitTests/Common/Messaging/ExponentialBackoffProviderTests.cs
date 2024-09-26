using NUnit.Framework;
using SmiServices.Common.Messaging;
using System;

namespace SmiServices.UnitTests.Common.Messaging;

internal class ExponentialBackoffProviderTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestLogger.Setup();
    }

    [Test]
    public void GetNextBackoff_ReturnsIncreasingTimeSpan()
    {
        var provider = new ExponentialBackoffProvider(new TimeSpan(1));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(1)));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(2)));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(4)));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(8)));
    }

    [Test]
    public void Reset_ReturnsTimeoutToInitial()
    {
        var provider = new ExponentialBackoffProvider(new TimeSpan(1));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(1)));
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(2)));
        provider.Reset();
        Assert.That(provider.GetNextBackoff(), Is.EqualTo(new TimeSpan(1)));
    }
}
