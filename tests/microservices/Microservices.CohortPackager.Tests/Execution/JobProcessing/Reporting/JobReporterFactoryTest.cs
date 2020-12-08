﻿using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting
{
    public class JobReporterFactoryTest
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
        public void GetReporter_ThrowsException_OnInvalidReportFormatStr()
        {
            var exc = Assert.Throws<ArgumentException>(() =>
                JobReporterFactory.GetReporter(
                    reporterTypeStr: "FileReporter",
                    new Mock<IExtractJobStore>().Object,
                    new MockFileSystem(),
                    extractRoot: "",
                    reportFormatStr: "FooFormat",
                    reportNewLine: null
                )
            );
            Assert.AreEqual(expected: "Could not parse reportFormatStr to a valid ReportFormat. Got 'FooFormat'", exc.Message);
        }

        [Test]
        public void GetReporter_ConstructsFileReporter()
        {
            var mockJobStore = new Mock<IExtractJobStore>();
            IJobReporter reporter = JobReporterFactory.GetReporter(
                reporterTypeStr: "FileReporter",
                mockJobStore.Object,
                new MockFileSystem(),
                extractRoot: "",
                reportFormatStr: "Combined",
                reportNewLine: null
            );

            var fileReporter = reporter as FileReporter;
            Assert.NotNull(fileReporter);
            Assert.AreEqual(ReportFormat.Combined, fileReporter.ReportFormat);
        }

        [Test]
        public void GetReporter_ConstructsLoggingReporter()
        {
            var mockJobStore = new Mock<IExtractJobStore>();
            IJobReporter reporter = JobReporterFactory.GetReporter(
                reporterTypeStr: "LoggingReporter",
                mockJobStore.Object,
                new MockFileSystem(),
                extractRoot: "",
                reportFormatStr: "Combined",
                reportNewLine: null
            );

            var loggingReporter = reporter as LoggingReporter;
            Assert.NotNull(loggingReporter);
            Assert.AreEqual(ReportFormat.Combined, loggingReporter.ReportFormat);
        }

        [Test]
        public void GetReporter_ThrowsException_OnInvalidReporterTypeStr()
        {
            var exc = Assert.Throws<ArgumentException>(() =>
                JobReporterFactory.GetReporter(
                    reporterTypeStr: "FooReporter",
                    new Mock<IExtractJobStore>().Object,
                    new MockFileSystem(),
                    extractRoot: "",
                    reportFormatStr: "Combined",
                    reportNewLine: null
                )
            );
            Assert.AreEqual(expected: "No case for type, or invalid type string 'FooReporter'", exc.Message);
        }

        [Test]
        public void GetReporter_UsesOutputNewLine()
        {
            // TODO(rkm 2020-11-26) Maybe improve this by building list using reflection to cover new types in future?
            var reporterImpls = new List<Type> { typeof(FileReporter), typeof(LoggingReporter) };

            // NOTE(rkm 2020-11-20) Ensure we aren't testing the Environment.NewLine, which will be the default if the format is not properly passed
            string testNewLine = (Environment.NewLine == "\r\n") ? "\n" : "\r\n";

            foreach (Type reporterImpl in reporterImpls)
            {
                IJobReporter reporter = JobReporterFactory.GetReporter(
                    reporterImpl.Name,
                    new Mock<IExtractJobStore>().Object,
                    new MockFileSystem(),
                    extractRoot: "",
                    reportFormatStr: "Combined",
                    testNewLine
                );

                var asBase = reporter as JobReporterBase;
                Assert.NotNull(asBase);
                Assert.AreEqual(testNewLine, asBase.ReportNewLine);
            }
        }

        #endregion
    }
}
