
using FellowOakDicom;
using DicomTypeTranslation;
using DicomTypeTranslation.TableCreation;
using Microservices.DicomRelationalMapper.Execution;
using Microservices.DicomRelationalMapper.Execution.Namers;
using Microservices.Tests.RDMPTests;
using Moq;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.Curation.Data.EntityNaming;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.EntityNaming;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.Logging;
using Rdmp.Dicom.Attachers.Routing;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;
using Smi.Common.Messages;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BadMedicine.Dicom;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common;
using DatabaseType = FAnsi.DatabaseType;

namespace Microservices.DicomRelationalMapper.Tests.DLEBenchmarkingTests
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    public class HowFastIsDLETest : DatabaseTests
    {
        private GlobalOptions _globals = null!;
        private DicomRelationalMapperTestHelper _helper = null!;
        private IDataLoadInfo _dli = null!;

        string _templateXml = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, @"DLEBenchmarkingTests/CT.it"));

        [OneTimeSetUp]
        public void SetupLogging()
        {
            var lm = CatalogueRepository.GetDefaultLogManager();
            lm.CreateNewLoggingTaskIfNotExists("aaa");
            _dli = lm.CreateDataLoadInfo("aaa", "HowFastIsDLETest", "Test", "", true);
        }

        [Test]
        public void Test_NullRoot()
        {
            var s = new DicomDatasetCollectionSource
            {
                ArchiveRoot = null
            };
        }

        [TestCase(DatabaseType.MySql, 600), RequiresRabbit]
        [TestCase(DatabaseType.MicrosoftSQLServer, 600), RequiresRabbit]
        public void TestLargeImageDatasets(DatabaseType databaseType, int numberOfImages)
        {
            foreach (Pipeline p in CatalogueRepository.GetAllObjects<Pipeline>())
                p.DeleteInDatabase();

            var db = GetCleanedServer(databaseType);

            if (CatalogueRepository.GetDefaultFor(PermissableDefaults.RAWDataLoadServer) is not null)
                CatalogueRepository.ClearDefault(PermissableDefaults.RAWDataLoadServer);

            var template = ImageTableTemplateCollection.LoadFrom(_templateXml);

            _globals = new GlobalOptionsFactory().Load(nameof(TestLargeImageDatasets));

            _globals.DicomRelationalMapperOptions!.DatabaseNamerType = typeof(MyFixedStagingDatabaseNamer).FullName;
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = ushort.MaxValue;
            _globals.DicomRelationalMapperOptions.MinimumBatchSize = numberOfImages;
            _globals.DicomRelationalMapperOptions.UseInsertIntoForRAWMigration = true;

            _helper = new DicomRelationalMapperTestHelper();
            _helper.SetupSuite(db, RepositoryLocator, _globals, typeof(DicomDatasetCollectionSource), root: null, template: template, persistentRaw: true);

            //do not use an explicit RAW data load server
            if (CatalogueRepository.GetDefaultFor(PermissableDefaults.RAWDataLoadServer) is not null)
                CatalogueRepository.ClearDefault(PermissableDefaults.RAWDataLoadServer);

            Random r = new(123);

            List<DicomDataset> allImages;

            using (var generator = new DicomDataGenerator(r, TestContext.CurrentContext.TestDirectory, "CT") { NoPixels = true })
                allImages = generator.GenerateImages(numberOfImages, r);

            Assert.That(allImages, Has.Count.EqualTo(numberOfImages));

            using (var tester = new MicroserviceTester(_globals.RabbitOptions!, _globals.DicomRelationalMapperOptions))
            {
                using var host = new DicomRelationalMapperHost(_globals);
                tester.SendMessages(_globals.DicomRelationalMapperOptions, allImages.Select(GetFileMessageForDataset), true);

                Console.WriteLine("Starting Host");
                host.Start();

                Stopwatch sw = Stopwatch.StartNew();
                TestTimelineAwaiter.Await(() => host.Consumer!.AckCount == numberOfImages, null, 20 * 60 * 100); //1 minute

                Console.Write($"Time For DLE:{sw.Elapsed.TotalSeconds}s");
                host.Stop("Test finished");
            }

            foreach (Pipeline allObject in CatalogueRepository.GetAllObjects<Pipeline>())
                allObject.DeleteInDatabase();
        }


        [TestCase(500)]
        public void TestGetChunkOnly(int numberOfImages)
        {
            Random r = new(123);

            List<DicomDataset> allImages;

            using (DicomDataGenerator g = new(r, null, "CT", "MR"))
                allImages = g.GenerateImages(numberOfImages, r);

            DicomDatasetCollectionSource source = new();
            source.PreInitialize(
                new ExplicitListDicomDatasetWorklist(allImages.ToArray(), "amagad.dcm",
                    new Dictionary<string, string> { { "MessageGuid", "0x123" } }),
                ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "gggg";

            Stopwatch sw = new();
            sw.Start();

            var dt = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
            source.Dispose(ThrowImmediatelyDataLoadEventListener.Noisy, null);

            sw.Stop();
            Console.WriteLine($"GetChunk took {sw.ElapsedMilliseconds}");

            Assert.That(dt.Rows, Has.Count.EqualTo(numberOfImages));
            Assert.That(dt.Rows.Cast<DataRow>().Select(static w => w["SOPInstanceUID"]).Distinct().Count(), Is.EqualTo(numberOfImages));
        }

        [TestCase(DatabaseType.MySql, 500)]
        [TestCase(DatabaseType.MicrosoftSQLServer, 500)]
        public void TestBulkInsertOnly(DatabaseType databaseType, int numberOfImages)
        {
            foreach (Pipeline p in CatalogueRepository.GetAllObjects<Pipeline>())
                p.DeleteInDatabase();

            var db = GetCleanedServer(databaseType);

            var template = ImageTableTemplateCollection.LoadFrom(_templateXml);

            _globals = new GlobalOptionsFactory().Load(nameof(TestBulkInsertOnly));

            _globals.DicomRelationalMapperOptions!.DatabaseNamerType = typeof(MyFixedStagingDatabaseNamer).FullName;
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = ushort.MaxValue;
            _globals.DicomRelationalMapperOptions.MinimumBatchSize = numberOfImages;
            _globals.DicomRelationalMapperOptions.UseInsertIntoForRAWMigration = true;

            _helper = new DicomRelationalMapperTestHelper();
            _helper.SetupSuite(db, RepositoryLocator, _globals, typeof(DicomDatasetCollectionSource), root: null, template: template, persistentRaw: true);

            //do not use an explicit RAW data load server
            if (CatalogueRepository.GetDefaultFor(PermissableDefaults.RAWDataLoadServer) is not null)
                CatalogueRepository.ClearDefault(PermissableDefaults.RAWDataLoadServer);

            Random r = new(123);

            List<DicomDataset> allImages;

            using (var generator = new DicomDataGenerator(r, null, "CT"))
                allImages = generator.GenerateImages(numberOfImages, r);

            DicomDatasetCollectionSource source = new();
            source.PreInitialize(
                new ExplicitListDicomDatasetWorklist(allImages.ToArray(), "amagad.dcm",
                    new Dictionary<string, string> { { "MessageGuid", "0x123" } }),
                ThrowImmediatelyDataLoadEventListener.Quiet);
            source.FilenameField = "gggg";

            var dt = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
            source.Dispose(ThrowImmediatelyDataLoadEventListener.Noisy, null);

            Assert.Multiple(() =>
            {
                Assert.That(allImages, Has.Count.EqualTo(numberOfImages));
                Assert.That(dt.Rows, Has.Count.EqualTo(numberOfImages));
            });

            var tables = _helper.LoadMetadata!.GetDistinctTableInfoList(false);

            var config = new HICDatabaseConfiguration(_helper.LoadMetadata, new SuffixBasedNamer());

            var job = Mock.Of<IDataLoadJob>(
                j => j.RegularTablesToLoad == tables.Cast<ITableInfo>().ToList() &&
                j.DataLoadInfo == _dli &&
                j.Configuration == config);

            var attacher = new AutoRoutingAttacher();
            attacher.Job = job;

            //Drop Primary Keys
            using (var con = db.Server.GetConnection())
            {
                con.Open();

                var cmd = db.Server.GetCommand(
                    databaseType == DatabaseType.MicrosoftSQLServer ?
                    @"ALTER TABLE ImageTable DROP CONSTRAINT PK_ImageTable
                    ALTER TABLE SeriesTable DROP CONSTRAINT PK_SeriesTable
                    ALTER TABLE StudyTable DROP CONSTRAINT PK_StudyTable" :

                    @"ALTER TABLE ImageTable DROP PRIMARY KEY;
                    ALTER TABLE SeriesTable  DROP PRIMARY KEY;
                    ALTER TABLE StudyTable  DROP PRIMARY KEY;"
                , con);

                cmd.ExecuteNonQuery();
            }

            attacher.Initialize(LoadDirectory.CreateDirectoryStructure(new DirectoryInfo(TestContext.CurrentContext.TestDirectory), "IgnoreMe", true), db);
            try
            {
                attacher.ProcessPipelineData(dt, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
                attacher.Dispose(ThrowImmediatelyDataLoadEventListener.Quiet, null);
            }
            catch (Exception e)
            {
                attacher.Dispose(ThrowImmediatelyDataLoadEventListener.Quiet, e);
                throw;
            }

            foreach (var tableInfo in tables)
                Assert.That(tableInfo.Discover(DataAccessContext.InternalDataProcessing).GetRowCount(), Is.EqualTo(numberOfImages),
                    "Row count was wrong for " + tableInfo);

            foreach (Pipeline allObject in CatalogueRepository.GetAllObjects<Pipeline>())
                allObject.DeleteInDatabase();
        }

        private DicomFileMessage GetFileMessageForDataset(DicomDataset dicomDataset)
        {

            var root = TestContext.CurrentContext.TestDirectory;

            var f = Path.GetRandomFileName();
            var msg = new DicomFileMessage(root, Path.Combine(root, $"{f}.dcm"));
            msg.SeriesInstanceUID = dicomDataset.GetString(DicomTag.SeriesInstanceUID);
            msg.StudyInstanceUID = dicomDataset.GetString(DicomTag.StudyInstanceUID);
            msg.SOPInstanceUID = dicomDataset.GetString(DicomTag.SOPInstanceUID);
            msg.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(dicomDataset);
            return msg;
        }
    }

}
