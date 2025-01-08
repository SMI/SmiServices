using NUnit.Framework;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser.Anonymisers;
using System;

namespace SmiServices.UnitTests.Microservices.DicomAnonymiser.Anonymisers;

public class AnonymiserFactoryTests
{
    [Test]
    public void CreateAnonymiser_ValidAnonymiser_IsOk()
    {
        // Arrange

        var globals = new GlobalOptions
        {
            DicomAnonymiserOptions = new DicomAnonymiserOptions
            {
                AnonymiserType = "SmiCtpAnonymiser",
                CtpAnonCliJar = "missing.jar",
            }
        };

        // Act

        void call() => AnonymiserFactory.CreateAnonymiser(globals);

        // Assert

        var exc = Assert.Throws<ArgumentException>(call);
        Assert.That(exc.Message, Is.EqualTo("CtpAnonCliJar 'missing.jar' does not exist (Parameter 'globalOptions')"));
    }

    [Test]
    public void CreateAnonymiser_InvalidAnonymiserName_ThrowsException()
    {
        // Arrange

        var globals = new GlobalOptions
        {
            DicomAnonymiserOptions = new DicomAnonymiserOptions
            {
                AnonymiserType = "whee",
            }
        };

        // Act

        void call() => AnonymiserFactory.CreateAnonymiser(globals);

        // Assert

        var e = Assert.Throws<ArgumentException>(call);
        Assert.That(e.Message, Is.EqualTo("Could not parse 'whee' to a valid AnonymiserType"));
    }

    [Test]
    public void CreateAnonymiser_NoCaseForAnonymiser_ThrowsException()
    {
        // Arrange

        var globals = new GlobalOptions
        {
            DicomAnonymiserOptions = new DicomAnonymiserOptions
            {
                AnonymiserType = "None",
            }
        };

        // Act

        void call() => AnonymiserFactory.CreateAnonymiser(globals);

        // Assert

        var e = Assert.Throws<NotImplementedException>(call);
        Assert.That(e.Message, Is.EqualTo("No case for AnonymiserType 'None'"));
    }
}
