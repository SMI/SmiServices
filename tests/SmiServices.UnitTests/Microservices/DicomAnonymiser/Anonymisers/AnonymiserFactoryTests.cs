using NUnit.Framework;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser.Anonymisers;
using SmiServices.UnitTests.Common;
using System;

namespace SmiServices.UnitTests.Microservices.DicomAnonymiser.Anonymisers;

public class AnonymiserFactoryTests
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

    [Test]
    public void CreateAnonymiser_InvalidAnonymiserName_ThrowsException()
    {
        var e = Assert.Throws<ArgumentException>(() =>
        {
            // TODO (da 2024-02-28) Review if this is the correct way to test this
            // AnonymiserFactory.CreateAnonymiser(new DefaultAnonymiser { AnonymiserType = "whee" });
            AnonymiserFactory.CreateAnonymiser(new GlobalOptions { DicomAnonymiserOptions = new DicomAnonymiserOptions { AnonymiserType = "whee" } });
        });
        Assert.That(e!.Message, Is.EqualTo("Could not parse 'whee' to a valid AnonymiserType"));
    }

    [Test]
    public void CreateAnonymiser_NoCaseForAnonymiser_ThrowsException()
    {
        var e = Assert.Throws<NotImplementedException>(() =>
        {
            // TODO (da 2024-02-28) Review if this is the correct way to test this
            // AnonymiserFactory.CreateAnonymiser(new DicomAnonymiserOptions { AnonymiserType = "None" });
            AnonymiserFactory.CreateAnonymiser(new GlobalOptions { DicomAnonymiserOptions = new DicomAnonymiserOptions { AnonymiserType = "None" } });
        });
        Assert.That(e!.Message, Is.EqualTo("No case for AnonymiserType 'None'"));
    }

    #endregion
}
