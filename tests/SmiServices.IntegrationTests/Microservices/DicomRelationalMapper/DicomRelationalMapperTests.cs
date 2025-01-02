using BadMedicine.Dicom;
using DicomTypeTranslation;
using FAnsi.Implementations.MicrosoftSQL;
using FellowOakDicom;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using Rdmp.Dicom.TagPromotionSchema;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomRelationalMapper;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.Microservices.DicomRelationalMapper;
using SmiServices.UnitTests.TestCommon;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Tests.Common;
using DatabaseType = FAnsi.DatabaseType;

namespace SmiServices.IntegrationTests.Microservices.DicomRelationalMapper;

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

    }

    [Test]
    public void Test_DodgyTagNames()
    {
        _helper.TruncateTablesIfExists();

        DirectoryInfo d = new(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(Test_DodgyTagNames)));
        d.Create();

        var td = new TestData();
        var fi = td.Create(new FileInfo(Path.Combine(d.FullName, "MyTestFile.dcm")));
        var fi2 = td.Create(new FileInfo(Path.Combine(d.FullName, "MyTestFile2.dcm")));

        DicomFile dcm;

        using (var stream = File.OpenRead(fi.FullName))
        {
            dcm = DicomFile.Open(stream);
            // JS 2022-04-27 fo-dicom 4 version of this test used .Print, which is a group 0 tag disallowed in metadata in fo-dicom 5
            dcm.Dataset.AddOrUpdate(DicomTag.PrintPriority, "FISH");
            dcm.Dataset.AddOrUpdate(DicomTag.Date, new DateTime(2001, 01, 01));
            dcm.Save(fi2.FullName);
        }

        var adder = new TagColumnAdder(DicomTypeTranslaterReader.GetColumnNameForTag(DicomTag.Date, false), "datetime2", _helper.ImageTableInfo, new AcceptAllCheckNotifier());
        adder.Execute();

        adder = new TagColumnAdder(DicomTypeTranslaterReader.GetColumnNameForTag(DicomTag.PrintPriority, false), "varchar(max)", _helper.ImageTableInfo, new AcceptAllCheckNotifier());
        adder.Execute();

        fi.Delete();
        File.Move(fi2.FullName, fi.FullName);

        //creates the queues, exchanges and bindings
        var tester = new MicroserviceTester(_globals.RabbitOptions!, _globals.DicomRelationalMapperOptions!);
        tester.CreateExchange(_globals.RabbitOptions!.FatalLoggingExchange!, null);

        using (var host = new DicomRelationalMapperHost(_globals))
        {
            host.Start();

            using var timeline = new TestTimeline(tester);
            timeline.SendMessage(_globals.DicomRelationalMapperOptions!, DicomRelationalMapperTestHelper.GetDicomFileMessage(_globals.FileSystemOptions!.FileSystemRoot!, fi));

            //start the timeline
            timeline.StartTimeline();

            Thread.Sleep(TimeSpan.FromSeconds(10));
            TestTimelineAwaiter.Await(() => host.Consumer!.AckCount >= 1, null, 30000, () => host.Consumer!.DleErrors);

            Assert.Multiple(() =>
            {
                Assert.That(_helper.SeriesTable!.GetRowCount(), Is.EqualTo(1), "SeriesTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.StudyTable!.GetRowCount(), Is.EqualTo(1), "StudyTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.ImageTable!.GetRowCount(), Is.EqualTo(1), "ImageTable did not have the expected number of rows in LIVE");
            });

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

        var fi = new TestData().Create(new FileInfo(Path.Combine(d.FullName, "MyTestFile.dcm")));

        if (mixInATextFile)
        {
            var randomText = new FileInfo(Path.Combine(d.FullName, "RandomTextFile.dcm"));
            File.WriteAllLines(randomText.FullName, ["I love dancing", "all around the world", "boy the world is a big place eh?"]);
        }

        //creates the queues, exchanges and bindings
        var tester = new MicroserviceTester(_globals.RabbitOptions!, _globals.DicomRelationalMapperOptions!);
        tester.CreateExchange(_globals.RabbitOptions!.FatalLoggingExchange!, null);

        using (var host = new DicomRelationalMapperHost(_globals))
        {
            host.Start();

            using var timeline = new TestTimeline(tester);
            //send the message 10 times over a 10 second period
            for (int i = 0; i < numberOfMessagesToSend; i++)
            {
                timeline
                    .SendMessage(_globals.DicomRelationalMapperOptions!, DicomRelationalMapperTestHelper.GetDicomFileMessage(_globals.FileSystemOptions!.FileSystemRoot!, fi))
                    .Wait(1000);
            }

            //start the timeline
            timeline.StartTimeline();

            Thread.Sleep(TimeSpan.FromSeconds(10));
            TestTimelineAwaiter.Await(() => host.Consumer!.AckCount >= numberOfMessagesToSend, null, 30000, () => host.Consumer!.DleErrors);

            Assert.Multiple(() =>
            {
                Assert.That(_helper.SeriesTable!.GetRowCount(), Is.EqualTo(1), "SeriesTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.StudyTable!.GetRowCount(), Is.EqualTo(1), "StudyTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.ImageTable!.GetRowCount(), Is.EqualTo(1), "ImageTable did not have the expected number of rows in LIVE");
            });

            host.Stop("Test end");

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

        Assert.That(files, Has.Length.EqualTo(1));

        var existingColumns = _helper.ImageTable!.DiscoverColumns();

        //Add 200 random tags
        foreach (string tag in TagColumnAdder.GetAvailableTags().OrderBy(a => r.Next()).Take(200))
        {
            string dataType;

            try
            {
                dataType = TagColumnAdder.GetDataTypeForTag(tag, MicrosoftSQLTypeTranslater.Instance);

            }
            catch (Exception)
            {
                continue;
            }

            if (existingColumns.Any(c => c.GetRuntimeName().Equals(tag)))
                continue;

            var adder = new TagColumnAdder(tag, dataType, _helper.ImageTableInfo, new AcceptAllCheckNotifier())
            {
                SkipChecksAndSynchronization = true
            };
            adder.Execute();
        }

        new TableInfoSynchronizer(_helper.ImageTableInfo).Synchronize(new AcceptAllCheckNotifier());

        //creates the queues, exchanges and bindings
        var tester = new MicroserviceTester(_globals.RabbitOptions!, _globals.DicomRelationalMapperOptions!);
        tester.CreateExchange(_globals.RabbitOptions!.FatalLoggingExchange!, null);

        using (var host = new DicomRelationalMapperHost(_globals))
        {
            host.Start();

            using var timeline = new TestTimeline(tester);
            foreach (var f in files)
                timeline.SendMessage(_globals.DicomRelationalMapperOptions!,
                    DicomRelationalMapperTestHelper.GetDicomFileMessage(_globals.FileSystemOptions!.FileSystemRoot!, f));

            //start the timeline
            timeline.StartTimeline();

            TestTimelineAwaiter.Await(() => host.Consumer!.MessagesProcessed == 1, null, 30000, () => host.Consumer!.DleErrors);

            Assert.Multiple(() =>
            {
                Assert.That(_helper.SeriesTable!.GetRowCount(), Is.GreaterThanOrEqualTo(1), "SeriesTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.StudyTable!.GetRowCount(), Is.GreaterThanOrEqualTo(1), "StudyTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.ImageTable.GetRowCount(), Is.EqualTo(1), "ImageTable did not have the expected number of rows in LIVE");
            });

            host.Stop("Test end");
        }

        tester.Shutdown();

    }

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

        var msg1 = DicomRelationalMapperTestHelper.GetDicomFileMessage(ds, _globals.FileSystemOptions!.FileSystemRoot!, Path.Combine(_globals.FileSystemOptions.FileSystemRoot!, "mydicom.dcm"));
        var msg2 = DicomRelationalMapperTestHelper.GetDicomFileMessage(ds, _globals.FileSystemOptions.FileSystemRoot!, Path.Combine(_globals.FileSystemOptions.FileSystemRoot!, "mydicom.dcm"));


        //creates the queues, exchanges and bindings
        using var tester = new MicroserviceTester(_globals.RabbitOptions!, _globals.DicomRelationalMapperOptions!);
        tester.CreateExchange(_globals.RabbitOptions!.FatalLoggingExchange!, null);
        tester.SendMessage(_globals.DicomRelationalMapperOptions!, msg1);
        tester.SendMessage(_globals.DicomRelationalMapperOptions!, msg2);

        _globals.DicomRelationalMapperOptions!.RunChecks = true;

        using (var host = new DicomRelationalMapperHost(_globals))
        {
            host.Start();

            TestTimelineAwaiter.Await(() => host.Consumer!.MessagesProcessed == 2, null, 30000, () => host.Consumer!.DleErrors);

            Assert.Multiple(() =>
            {
                Assert.That(_helper.SeriesTable!.GetRowCount(), Is.GreaterThanOrEqualTo(1), "SeriesTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.StudyTable!.GetRowCount(), Is.GreaterThanOrEqualTo(1), "StudyTable did not have the expected number of rows in LIVE");
                Assert.That(_helper.ImageTable!.GetRowCount(), Is.EqualTo(1), "ImageTable did not have the expected number of rows in LIVE");
            });

            host.Stop("Test end");
        }
        tester.Shutdown();
    }
}
