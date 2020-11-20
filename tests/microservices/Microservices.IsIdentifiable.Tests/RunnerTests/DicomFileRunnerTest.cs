using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Runners;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;


namespace Microservices.IsIdentifiable.Tests.RunnerTests
{
    public class DicomFileRunnerTest
    {
        #region Fixture Methods

        private const string DataDirectory = @"../../../../../../../data/";
        private DirectoryInfo _tessDir;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            GlobalOptions globals = new GlobalOptionsFactory().Load();

            var testRulesDir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "data", "IsIdentifiableRules"));
            testRulesDir.Create();
            globals.IsIdentifiableOptions.DataDirectory = testRulesDir.Parent.FullName;
            _tessDir = new DirectoryInfo(Path.Combine(testRulesDir.Parent.FullName, "tessdata"));
            _tessDir.Create();
            string dest = Path.Combine(_tessDir.FullName, "eng.traineddata");
            if (!File.Exists(dest))
                File.Copy(Path.Combine(DataDirectory, "tessdata", "eng.traineddata"), dest);

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

        [TestCase(true)]
        [TestCase(false)]
        public void IgnorePixelDataLessThan(bool ignoreShortText)
        {
            var opts = new IsIdentifiableDicomFileOptions
            {
                ColumnReport = true,
                TessDirectory = _tessDir.FullName,
            };

            // NOTE(rkm 2020-11-16) The test image should report 3 bits of text with lengths 123, 127, and 170.
            // If we ignore less than 170 then only 1 bit of text should be reported.
            opts.IgnoreTextLessThan = ignoreShortText ? 170 : 0U;

            string fileName = Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(DicomFileRunnerTest), "f1.dcm");
            TestData.Create(new FileInfo(fileName), TestData.BURNED_IN_TEXT_IMG);

            var runner = new DicomFileRunner(opts);

            var fileSystem = new FileSystem();
            IFileInfo fileInfo = fileSystem.FileInfo.FromFileName(fileName);
            Assert.True(fileInfo.Exists);

            var toMemory = new ToMemoryFailureReport();
            runner.Reports.Add(toMemory);

            runner.ValidateDicomFile(fileInfo);

            List<Failure> failures = toMemory.Failures.ToList();
            Assert.AreEqual(ignoreShortText ? 1 : 3, failures.Count);
        }

        #endregion
    }
}
