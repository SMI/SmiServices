﻿using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Applications.SmiRunner.Tests
{
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
                    Assert.Contains(t, Program.AllApplications, $"{t} not in the list of applications");
                }
                else if (t.BaseType == typeof(MicroservicesVerbBase))
                {
                    Assert.Contains(t, Program.AllServices, $"{t} not in the list of services");
                }
                else
                {
                    Assert.Fail($"No case for {t.BaseType}");
                }
            }

        }

        #endregion
    }
}
