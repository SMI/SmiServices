﻿using CommandLine;
using JetBrains.Annotations;
using NUnit.Framework;
using Smi.Common.Options;


namespace Smi.Common.Tests.Options
{
    public class SmiCliInitTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SmiCliInit.InitSmiLogging = false;
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

        [UsedImplicitly]
        private class FakeCliOpts : CliOptions
        {
            [Option(
                'f', "foo",
                Required = false
            )]
            [UsedImplicitly]
            public string Foo { get; set; }
        }

        [Verb("fake")]
        private class FakeCliVerbOpts : CliOptions
        {
            [Option(
                'f', "foo",
                Required = false
            )]
            [UsedImplicitly]
            public string Foo { get; set; }
        }

        #endregion

        #region Tests

        [Test]
        public void SmiCliInit_SingleParser_HappyPath()
        {
            static int OnParse(GlobalOptions globals, FakeCliOpts opts)
            {
                if (opts.Foo == "bar")
                    return 123;
                return -1;
            }

            var args = new[]
            {
                "-y", "default.yaml",
                "-f", "bar"
            };

            int ret = SmiCliInit.ParseAndRun<FakeCliOpts>(args, OnParse);
            Assert.AreEqual(123, ret);
        }

        [Test]
        public void SmiCliInit_SingleParser_Help_ReturnsZero()
        {
            var args = new[] { "--help" };

            int ret = SmiCliInit.ParseAndRun<FakeCliOpts>(args, (_, __) => -1);
            Assert.AreEqual(0, ret);
        }

        [Test]
        public void SmiCliInit_VerbParser_HappyPath()
        {
            static int OnParse(GlobalOptions globals, object parsed)
            {
                var opts = SmiCliInit.Verify<FakeCliVerbOpts>(parsed);

                if (opts.Foo == "bar")
                    return 123;
                return -1;
            }

            var args = new[]
            {
                "fake",
                "-y", "default.yaml",
                "-f", "bar"
            };

            int ret = SmiCliInit.ParseAndRun(args, new[] { typeof(FakeCliVerbOpts) }, OnParse);
            Assert.AreEqual(123, ret);
        }

        [Test]
        public void SmiCliInit_VerbParser_Help_ReturnsZero()
        {
            var args = new[]
            {
                "fake",
                "--help"
            };

            int ret = SmiCliInit.ParseAndRun(args, new[] { typeof(FakeCliVerbOpts) }, (_, __) => -1);
            Assert.AreEqual(0, ret);
        }

        #endregion
    }
}
