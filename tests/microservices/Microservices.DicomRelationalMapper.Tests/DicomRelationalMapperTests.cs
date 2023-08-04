using System;
using System.IO;
using System.Linq;
using System.Threading;
using BadMedicine.Dicom;
using FellowOakDicom;
using DicomTypeTranslation;
using FAnsi.Implementations.MicrosoftSQL;
using Microservices.DicomRelationalMapper.Execution;
using Microservices.Tests.RDMPTests;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using Rdmp.Dicom.TagPromotionSchema;
using ReusableLibraryCode.Checks;
using Smi.Common.Options;
using Smi.Common.Tests;
using Tests.Common;
using DatabaseType = FAnsi.DatabaseType;

namespace Microservices.DicomRelationalMapper.Tests
{
    [RequiresRabbit, RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    public class DicomRelationalMapperTests : DatabaseTests
    {
        private DicomRelationalMapperTestHelper _helper = null!;
        private GlobalOptions _globals = null!;

        [SetUp]
        public void Setup()
        {
            BlitzMainDataTables();

            _globals = new GlobalOptionsFactory().Load(nameof(DicomRelationalMapperTests));
            var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
            _helper = new DicomRelationalMapperTestHelper();
            _helper.SetupSuite(db, RepositoryLocator, _globals, typeof(DicomDatasetCollectionSource));

            TestLogger.Setup();
        }

        [Test]
        public void Test_DodgyTagNames()
        {
            _helper.TruncateTablesIfExists();

            DirectoryInfo d = new(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(Test_DodgyTagNames)));
            d.Create();

            var fi = TestData.Create(new FileInfo(Path.Combine(d.FullName, "MyTestFile.dcm")));
            var fi2 = TestData.Create(new FileInfo(Path.Combine(d.FullName, "MyTestFile2.dcm")));
            
            DicomFile dcm;

            using (var stream = File.OpenRead(fi.FullName))
            {    
                dcm = DicomFile.Open(stream);
                // JS 2022-04-27 fo-dicom 4 version of this test used .Print, which is a group 0 tag disallowed in metadata in fo-dicom 5
                dcm.Dataset.AddOrUpdate(DicomTag.PrintPriority, "FISH");
                dcm.Dataset.AddOrUpdate(DicomTag.Date, new DateTime(2001,01,01));
                dcm.Save(fi2.FullName);
            }
            
            var adder = new TagColumnAdder(DicomTypeTranslaterReader.GetColumnNameForTag(DicomTag.Date,false), "datetime2", _helper.ImageTableInfo, new AcceptAllCheckNotifier());
            adder.Execute();

            adder = new TagColumnAdder(DicomTypeTranslaterReader.GetColumnNameForTag(DicomTag.PrintPriority,false), "varchar(max)", _helper.ImageTableInfo, new AcceptAllCheckNotifier());
            adder.Execute();
            
            fi.Delete();
            File.Move(fi2.FullName,fi.FullName);
            
            //creates the queues, exchanges and bindings
            var tester = new MicroserviceTester(_globals.RabbitOptions, _globals.DicomRelationalMapperOptions);
            tester.CreateExchange(_globals.RabbitOptions.FatalLoggingExchange, null);

            using (var host = new DicomRelationalMapperHost(_globals))
            {
                host.Start();

                using var timeline = new TestTimeline(tester);
                timeline.SendMessage(_globals.DicomRelationalMapperOptions, _helper.GetDicomFileMessage(_globals.FileSystemOptions.FileSystemRoot, fi));

                //start the timeline
                timeline.StartTimeline();

                Thread.Sleep(TimeSpan.FromSeconds(10));
                TestTimelineAwaiter.Await(() => host.Consumer.AckCount >= 1, null, 30000, () => host.Consumer.DleErrors);

                Assert.AreEqual(1, _helper.SeriesTable.GetRowCount(), "SeriesTable did not have the expected number of rows in LIVE");
                Assert.AreEqual(1, _helper.StudyTable.GetRowCount(), "StudyTable did not have the expected number of rows in LIVE");
                Assert.AreEqual(1, _helper.ImageTable.GetRowCount(), "ImageTable did not have the expected number of rows in LIVE");

                host.Stop("Test end");

            }

            tester.Shutdown();
        }


        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(10, false)]
        public void TestLoadingOneImage_SingleFileMessage(int numberOfMessagesToSend, bool mixInATextFile)
        {
            _helper.TruncateTablesIfExists();

            DirectoryInfo d = new(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(TestLoadingOneImage_SingleFileMessage)));
            d.Create();

            var fi = TestData.Create(new FileInfo(Path.Combine(d.FullName, "MyTestFile.dcm")));

            if (mixInATextFile)
            {
                var randomText = new FileInfo(Path.Combine(d.FullName, "RandomTextFile.dcm"));
                File.WriteAllLines(randomText.FullName, new[] { "I love dancing", "all around the world", "boy the world is a big place eh?" });
            }

            //creates the queues, exchanges and bindings
            var tester = new MicroserviceTester(_globals.RabbitOptions, _globals.DicomRelationalMapperOptions);
            tester.CreateExchange(_globals.RabbitOptions.FatalLoggingExchange, null);

            using (var host = new DicomRelationalMapperHost(_globals))
            {
                host.Start();

                using (var timeline = new TestTimeline(tester))
                {
                    //send the message 10 times over a 10 second period
                    for (int i = 0; i < numberOfMessagesToSend; i++)
                    {
                        timeline
                            .SendMessage(_globals.DicomRelationalMapperOptions, _helper.GetDicomFileMessage(_globals.FileSystemOptions.FileSystemRoot, fi))
                            .Wait(1000);
                    }

                    //start the timeline
                    timeline.StartTimeline();

                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    TestTimelineAwaiter.Await(() => host.Consumer.AckCount >= numberOfMessagesToSend, null, 30000, () => host.Consumer.DleErrors);

                    Assert.AreEqual(1, _helper.SeriesTable.GetRowCount(), "SeriesTable did not have the expected number of rows in LIVE");
                    Assert.AreEqual(1, _helper.StudyTable.GetRowCount(), "StudyTable did not have the expected number of rows in LIVE");
                    Assert.AreEqual(1, _helper.ImageTable.GetRowCount(), "ImageTable did not have the expected number of rows in LIVE");

                    host.Stop("Test end");
                }
                
            }

            tester.Shutdown();
        }

        [Test]
        public void TestLoadingOneImage_MileWideTest()
        {
            _helper.TruncateTablesIfExists();

            DirectoryInfo d = new(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(TestLoadingOneImage_MileWideTest)));
            d.Create();

            var r = new Random(5000);
            FileInfo[] files;

            using (var g = new DicomDataGenerator(r, d.FullName, "CT")) 
                files = g.GenerateImageFiles(1, r).ToArray();

            Assert.AreEqual(1,files.Length);

            var existingColumns = _helper.ImageTable.DiscoverColumns();

            //Add 200 random tags
            foreach (string tag in TagColumnAdder.GetAvailableTags().OrderBy(a => r.Next()).Take(200))
            {
                string dataType;

                try
                {
                    dataType = TagColumnAdder.GetDataTypeForTag(tag, new MicrosoftSQLTypeTranslater());
                    
                }
                catch (Exception)
                {
                    continue;
                }

                if (existingColumns.Any(c => c.GetRuntimeName().Equals(tag)))
                    continue;

                var adder = new TagColumnAdder(tag, dataType, _helper.ImageTableInfo, new AcceptAllCheckNotifier());
                adder.SkipChecksAndSynchronization = true;
                adder.Execute();
            }

            new TableInfoSynchronizer(_helper.ImageTableInfo).Synchronize(new AcceptAllCheckNotifier());

            //creates the queues, exchanges and bindings
            var tester = new MicroserviceTester(_globals.RabbitOptions, _globals.DicomRelationalMapperOptions);
            tester.CreateExchange(_globals.RabbitOptions.FatalLoggingExchange, null);

            using (var host = new DicomRelationalMapperHost(_globals))
            {
                host.Start();

                using (var timeline = new TestTimeline(tester))
                {
                    foreach (var f in files)
                        timeline.SendMessage(_globals.DicomRelationalMapperOptions,
                            _helper.GetDicomFileMessage(_globals.FileSystemOptions.FileSystemRoot, f));

                    //start the timeline
                    timeline.StartTimeline();

                    TestTimelineAwaiter.Await(() => host.Consumer.MessagesProcessed == 1, null, 30000, () => host.Consumer.DleErrors);

                    Assert.GreaterOrEqual(1, _helper.SeriesTable.GetRowCount(), "SeriesTable did not have the expected number of rows in LIVE");
                    Assert.GreaterOrEqual(1, _helper.StudyTable.GetRowCount(), "StudyTable did not have the expected number of rows in LIVE");
                    Assert.AreEqual(1, _helper.ImageTable.GetRowCount(), "ImageTable did not have the expected number of rows in LIVE");

                    host.Stop("Test end");
                }
            }

            tester.Shutdown();
            
        }

        /*
        [TestCase(10, 1000)]
        public void DicomFileGeneratorTest(int numberOfImges, int intervalInMilliseconds)
        {
            _helper.TruncateTablesIfExists();

            DirectoryInfo d = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(DicomFileGeneratorTest)));
            d.Create();

            foreach (var oldFile in d.EnumerateFiles())
                oldFile.Delete();

            var seedDir = d.CreateSubdirectory("Seed");

            TestData.Create(new FileInfo(Path.Combine(seedDir.FullName, "MyTestFile.dcm")));

            using (DicomGenerator g = new DicomGenerator(d.FullName, "Seed", 1000))
            {
                g.GenerateTestSet(numberOfImges, 100, new TestTagDataGenerator(), 100, false);

                //creates the queues, exchanges and bindings
                var tester = new MicroserviceTester(_globals.RabbitOptions, _globals.DicomRelationalMapperOptions);
                tester.CreateExchange(_globals.RabbitOptions.FatalLoggingExchange, null);

                using (var host = new DicomRelationalMapperHost(_globals, loadSmiLogConfig: false))
                {
                    host.Start();

                    using(var timeline = new TestTimeline(tester))
                    {
                        foreach (var f in g.FilesCreated)
                        {

                            timeline
                                .SendMessage(_globals.DicomRelationalMapperOptions, _helper.GetDicomFileMessage(_globals.FileSystemOptions.FileSystemRoot, f))
                                .Wait(intervalInMilliseconds);
                        }

                        //start the timeline
                        timeline.StartTimeline();


                        new TestTimelineAwaiter().Await(() => host.Consumer.MessagesProcessed == numberOfImges, null, 30000, () => host.Consumer.DleErrors);
                        Assert.GreaterOrEqual(1, _helper.SeriesTable.GetRowCount(), "SeriesTable did not have the expected number of rows in LIVE");
                        Assert.GreaterOrEqual(1, _helper.StudyTable.GetRowCount(), "StudyTable did not have the expected number of rows in LIVE");
                        Assert.AreEqual(numberOfImges, _helper.ImageTable.GetRowCount(), "ImageTable did not have the expected number of rows in LIVE");

                        host.Stop("Test end");
                    }
                    
                }

                tester.Shutdown();
            }
        }
        */

        /// <summary>
        /// Tests the abilities of the DLE to not load identical FileMessage
        /// </summary>
        [Test]
        public void IdenticalDatasetsTest()
        {
            _helper.TruncateTablesIfExists();

            var ds = new DicomDataset();
            ds.AddOrUpdate(DicomTag.SeriesInstanceUID, "123");
            ds.AddOrUpdate(DicomTag.SOPInstanceUID, "123");
            ds.AddOrUpdate(DicomTag.StudyInstanceUID, "123");
            ds.AddOrUpdate(DicomTag.PatientID, "123");

            var msg1 = _helper.GetDicomFileMessage(ds, _globals.FileSystemOptions.FileSystemRoot, Path.Combine(_globals.FileSystemOptions.FileSystemRoot, "mydicom.dcm"));
            var msg2 = _helper.GetDicomFileMessage(ds, _globals.FileSystemOptions.FileSystemRoot, Path.Combine(_globals.FileSystemOptions.FileSystemRoot, "mydicom.dcm"));


            //creates the queues, exchanges and bindings
            using var tester = new MicroserviceTester(_globals.RabbitOptions, _globals.DicomRelationalMapperOptions);
            tester.CreateExchange(_globals.RabbitOptions.FatalLoggingExchange, null);
            tester.SendMessage(_globals.DicomRelationalMapperOptions, msg1);
            tester.SendMessage(_globals.DicomRelationalMapperOptions, msg2);

            _globals.DicomRelationalMapperOptions.RunChecks = true;

            using (var host = new DicomRelationalMapperHost(_globals))
            {
                host.Start();

                TestTimelineAwaiter.Await(() => host.Consumer.MessagesProcessed == 2, null, 30000, () => host.Consumer.DleErrors);

                Assert.GreaterOrEqual(1, _helper.SeriesTable.GetRowCount(), "SeriesTable did not have the expected number of rows in LIVE");
                Assert.GreaterOrEqual(1, _helper.StudyTable.GetRowCount(), "StudyTable did not have the expected number of rows in LIVE");
                Assert.AreEqual(1, _helper.ImageTable.GetRowCount(), "ImageTable did not have the expected number of rows in LIVE");

                host.Stop("Test end");
            }
            tester.Shutdown();
        }
    }
}
