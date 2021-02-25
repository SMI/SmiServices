using Microservices.CohortPackager.Execution;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using MongoDB.Driver;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;


namespace Microservices.CohortPackager.Tests.Execution
{
    [TestFixture, RequiresMongoDb, RequiresRabbit]
    public class CohortPackagerHostTest
    {
        private readonly TestDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        #region Fixtures

        // TODO(rkm 2020-12-17) Test if the old form of this is fixed in NUnit 3.13 (see https://github.com/nunit/nunit/issues/2574)
        private class PathFixtures : IDisposable
        {
            public readonly string ExtractName;
            public readonly string TestDirAbsolute;
            public readonly string ExtractRootAbsolute;
            public readonly string ProjExtractionsDirRelative = Path.Combine("proj", "extractions");
            public readonly string ProjExtractDirRelative;
            public readonly string ProjExtractDirAbsolute;
            public readonly string ProjReportsDirAbsolute;

            public PathFixtures(string extractName)
            {
                ExtractName = extractName;
                string testName = TestContext.CurrentContext.Test.FullName.Replace('(', '_').Replace(")", "");
                TestDirAbsolute = Path.Combine(Path.GetTempPath(), "nunit-smiservices", $"{testName}-{Guid.NewGuid().ToString().Split('-')[0]}");

                ExtractRootAbsolute = Path.Combine(TestDirAbsolute, "extractRoot");

                ProjExtractDirRelative = Path.Combine(ProjExtractionsDirRelative, extractName);
                ProjExtractDirAbsolute = Path.Combine(ExtractRootAbsolute, ProjExtractDirRelative);

                ProjReportsDirAbsolute = Path.Combine(ExtractRootAbsolute, ProjExtractionsDirRelative, "reports");

                // NOTE(rkm 2020-11-19) This would normally be created by one of the other services
                Directory.CreateDirectory(ProjExtractDirAbsolute);
            }

            public void Dispose()
            {
                ResultState outcome = TestContext.CurrentContext.Result.Outcome;
                if (outcome == ResultState.Failure || outcome == ResultState.Error)
                    return;

                Directory.Delete(TestDirAbsolute, recursive: true);
            }
        }

        #endregion

        #region Test Methods

        private bool HaveFiles(PathFixtures pf) => Directory.Exists(pf.ProjReportsDirAbsolute) && Directory.EnumerateFiles(pf.ProjExtractDirAbsolute).Any();

        private void VerifyReports(GlobalOptions globals, PathFixtures pf, ReportFormat reportFormat, IEnumerable<Tuple<ConsumerOptions, IMessage>> toSend)
        {
            globals.FileSystemOptions.ExtractRoot = pf.ExtractRootAbsolute;
            globals.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 5;
            globals.CohortPackagerOptions.ReporterType = "FileReporter";
            globals.CohortPackagerOptions.ReportFormat = reportFormat.ToString();
            
            MongoClient client = MongoClientHelpers.GetMongoClient(globals.MongoDatabases.ExtractionStoreOptions, "test", true);
            globals.MongoDatabases.ExtractionStoreOptions.DatabaseName += "-" + Guid.NewGuid().ToString().Split('-')[0];
            client.DropDatabase(globals.MongoDatabases.ExtractionStoreOptions.DatabaseName);

            using (var tester = new MicroserviceTester(
                globals.RabbitOptions,
                globals.CohortPackagerOptions.ExtractRequestInfoOptions,
                globals.CohortPackagerOptions.FileCollectionInfoOptions,
                globals.CohortPackagerOptions.NoVerifyStatusOptions,
                globals.CohortPackagerOptions.VerificationStatusOptions))
            {
                foreach ((ConsumerOptions consumerOptions, IMessage message) in toSend)
                    tester.SendMessage(consumerOptions, new MessageHeader(), message);

                var host = new CohortPackagerHost(globals);

                host.Start();

                var timeoutSecs = 10;

                while (!HaveFiles(pf) && timeoutSecs > 0)
                {
                    --timeoutSecs;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                host.Stop("Test end");
            }

            var firstLine = $"# SMI extraction validation report for testProj1/{pf.ExtractName}";
            switch (reportFormat)
            {
                case ReportFormat.Combined:
                    {
                        string reportContent = File.ReadAllText(Path.Combine(pf.ProjReportsDirAbsolute, $"{pf.ExtractName}_report.txt"));
                        Assert.True(reportContent.StartsWith(firstLine));
                        break;
                    }
                case ReportFormat.Split:
                    {
                        string extractReportsDirAbsolute = Path.Combine(pf.ProjReportsDirAbsolute, pf.ExtractName);
                        Assert.AreEqual(6, Directory.GetFiles(extractReportsDirAbsolute).Length);
                        string reportContent = File.ReadAllText(Path.Combine(extractReportsDirAbsolute, "README.md"));
                        Assert.True(reportContent.StartsWith(firstLine));
                        break;
                    }
                default:
                    Assert.Fail($"No case for ReportFormat {reportFormat}");
                    break;
            }
        }

        #endregion

        #region Tests

        [TestCase(ReportFormat.Combined)]
        [TestCase(ReportFormat.Split)]
        public void Integration_HappyPath(ReportFormat reportFormat)
        {
            // Test messages:
            //  - series-1
            //      - series-1-anon-1.dcm -> valid

            using var pf = new PathFixtures($"Integration_HappyPath_{reportFormat}");

            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 1,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-1-anon-1.dcm" },
                },
                RejectionReasons = new Dictionary<string, int>
                {
                    {"rejected - blah", 1 },
                },
                KeyValue = "series-1",
            };
            var testIsIdentifiableMessage = new ExtractedFileVerificationMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "series-1-anon-1.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                IsIdentifiable = false,
                Report = "[]",
                DicomFilePath = "series-1-orig-1.dcm",
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();

            VerifyReports(
                globals,
                pf,
                reportFormat,
                new[]
                {
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.ExtractRequestInfoOptions, testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions, testExtractFileCollectionInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions, testIsIdentifiableMessage),
                }
            );
        }

        [TestCase(ReportFormat.Combined)]
        [TestCase(ReportFormat.Split)]
        public void Integration_BumpyRoad(ReportFormat reportFormat)
        {
            // Test messages:
            //  - series-1
            //      - series-1-anon-1.dcm -> valid
            //      - series-1-anon-2.dcm -> rejected
            //  - series-2
            //      - series-2-anon-1.dcm -> fails anonymisation
            //      - series-2-anon-2.dcm -> fails validation

            using var pf = new PathFixtures($"Integration_BumpyRoad_{reportFormat}");

            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 2,
            };
            var testExtractFileCollectionInfoMessage1 = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-1-anon-1.dcm" },
                },
                RejectionReasons = new Dictionary<string, int>
                {
                    {"rejected - blah", 1 },
                },
                KeyValue = "series-1",
            };
            var testExtractFileCollectionInfoMessage2 = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-2-anon-1.dcm" },
                    { new MessageHeader(), "series-2-anon-2.dcm" },
                },
                RejectionReasons = new Dictionary<string, int>(),
                KeyValue = "series-2",
            };
            var testExtractFileStatusMessage = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "series-2-anon-1.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                Status = ExtractedFileStatus.ErrorWontRetry,
                StatusMessage = "Couldn't anonymise",
                DicomFilePath = "series-2-orig-1.dcm",
            };
            var testIsIdentifiableMessage1 = new ExtractedFileVerificationMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "series-1-anon-1.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                IsIdentifiable = false,
                Report = "[]",
                DicomFilePath = "series-1-orig-1.dcm",
            };
            const string failureReport = @"
[
    {
        'Parts': [],
        'Resource': 'series-2-anon-2.dcm',
        'ResourcePrimaryKey': '1.2.3.4',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'FOO'
    }
]";
            var testIsIdentifiableMessage2 = new ExtractedFileVerificationMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "series-2-anon-2.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                IsIdentifiable = true,
                Report = failureReport,
                DicomFilePath = "series-2-orig-2.dcm",
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();

            VerifyReports(
                globals,
                pf,
                reportFormat,
                new[]
                {
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.ExtractRequestInfoOptions, testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions,testExtractFileCollectionInfoMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions,  testExtractFileCollectionInfoMessage2),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions,  testExtractFileStatusMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions, testIsIdentifiableMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions, testIsIdentifiableMessage2),
                }
            );
        }

        [Test]
        public void Integration_IdentifiableExtraction_HappyPath()
        {
            using var pf = new PathFixtures("Integration_IdentifiableExtraction_HappyPath");

            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 1,
                IsIdentifiableExtraction = true,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "out1.dcm" },
                    { new MessageHeader(), "out2.dcm" },
                },
                RejectionReasons = new Dictionary<string, int>
                {
                    {"rejected - blah", 1 },
                },
                KeyValue = "study-1",
                IsIdentifiableExtraction = true,
            };
            var testExtractFileStatusMessage1 = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "src.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                Status = ExtractedFileStatus.Copied,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-1.dcm",
                IsIdentifiableExtraction = true,
            };
            var testExtractFileStatusMessage2 = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "src_missing.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                Status = ExtractedFileStatus.FileMissing,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-2.dcm",
                IsIdentifiableExtraction = true,
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();

            VerifyReports(
                globals,
                pf,
                ReportFormat.Combined,
                new[]
                {
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.ExtractRequestInfoOptions,  testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions,testExtractFileCollectionInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions,testExtractFileStatusMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions,  testExtractFileStatusMessage2),
                }
            );
        }

        #endregion
    }
}
