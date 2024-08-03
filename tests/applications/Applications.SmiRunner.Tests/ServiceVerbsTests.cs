using CommandLine;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace Applications.SmiRunner.Tests
{
    public class ServiceVerbsTests
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
        public void VerbName_MatchesClassName()
        {
            foreach (Type t in _allVerbs)
            {
                string nameWithoutVerb = t.Name.Substring(0, t.Name.LastIndexOf("Verb"));
                string[] splitWords = Regex.Split(nameWithoutVerb, @"(?<!^)(?=[A-Z])");
                string expectedVerbName = string.Join('-', splitWords).ToLower();

                // Special-case 'DB'
                expectedVerbName = expectedVerbName.Replace("-db-", "db-");

                var verbAttribute = (VerbAttribute?)Attribute.GetCustomAttribute(t, typeof(VerbAttribute));
                Assert.That(verbAttribute, Is.Not.Null);

                Assert.That(verbAttribute!.Name, Is.EqualTo(expectedVerbName));
            }
        }

        #endregion
    }
}
