using NUnit.Framework;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.Linq;


namespace SmiServices.UnitTests;

public class ProgramTests
{
    #region Fixture Methods

    private readonly IEnumerable<Type> _allVerbs =
        typeof(VerbBase)
            .Assembly
            .GetTypes()
            .Where(t => typeof(VerbBase).IsAssignableFrom(t) && !t.IsAbstract);

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

    /// <summary>
    /// Checks all defined verb types are actually used
    /// </summary>
    [Test]
    public void AllVerbTypes_AreUsed()
    {
        foreach (Type t in _allVerbs)
        {
            if (t.BaseType == typeof(ApplicationVerbBase))
            {
                Assert.That(Program.AllApplications, Does.Contain(t), $"{t} not in the list of applications");
            }
            else if (t.BaseType == typeof(MicroservicesVerbBase))
            {
                Assert.That(Program.AllServices, Does.Contain(t), $"{t} not in the list of services");
            }
            else
            {
                Assert.Fail($"No case for {t.BaseType}");
            }
        }

    }

    #endregion
}
