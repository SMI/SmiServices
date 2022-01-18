using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;
using System.Threading;
using Moq;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Smi.Common.Tests;
using SmiPlugin;

namespace Applications.ExtractImages.Tests
{
    [RequiresRabbit]
    public class SmiImageExtractorTests
    {
        #region Fixture Methods

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
        public void SmiImageExtractor_CheckNoConnectionSettings()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(SmiImageExtractor_CheckNoConnectionSettings));
            globals.ExtractImagesOptions.MaxIdentifiersPerMessage = 1;

            var extractor = new SmiImageExtractor();
            extractor.Check(new ThrowImmediatelyCheckNotifier());
        }

        // TODO: remove this
        [Test]
        public void Fail()
        {
            Assert.Fail("fail");
        }
        #endregion
    }
}
