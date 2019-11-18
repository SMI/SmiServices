
using Dicom;
using DicomTypeTranslation;
using DicomTypeTranslation.TableCreation;
using Microservices.DicomRelationalMapper.Execution;
using Microservices.DicomRelationalMapper.Execution.Namers;
using Microservices.DicomRelationalMapper.Tests.TestTagGeneration;
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
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.Progress;
using Smi.Common.Messages;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Tests.Common;
using DatabaseType = FAnsi.DatabaseType;

namespace Microservices.DicomRelationalMapper.Tests.DLEBenchmarkingTests
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    public class HowFastIsDLETest : DatabaseTests
    {
        private GlobalOptions _globals;
        private DicomRelationalMapperTestHelper _helper;
        private IDataLoadInfo _dli;

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
            var s = new DicomDatasetCollectionSource();
            s.ArchiveRoot = null;
        }

        [TestCase(DatabaseType.MySql, 600), RequiresRabbit]
        [TestCase(DatabaseType.MicrosoftSQLServer, 600), RequiresRabbit]
        public void TestLargeImageDatasets(DatabaseType databaseType, int numberOfImages)
        {
            foreach (Pipeline p in CatalogueRepository.GetAllObjects<Pipeline>())
                p.DeleteInDatabase();

            var db = GetCleanedServer(databaseType);

            var d = CatalogueRepository.GetServerDefaults();
            d.ClearDefault(PermissableDefaults.RAWDataLoadServer);

            var template = ImageTableTemplateCollection.LoadFrom(_templateXml);

            _globals = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

            _globals.DicomRelationalMapperOptions.DatabaseNamerType = typeof(MyFixedStagingDatabaseNamer).FullName;
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = ushort.MaxValue;
            _globals.DicomRelationalMapperOptions.MinimumBatchSize = numberOfImages;
            _globals.DicomRelationalMapperOptions.UseInsertIntoForRAWMigration = true;

            _helper = new DicomRelationalMapperTestHelper();
            _helper.SetupSuite(db, RepositoryLocator, _globals, typeof(DicomDatasetCollectionSource), root: null, template: template, persistentRaw: true);

            //do not use an explicit RAW data load server
            d.ClearDefault(PermissableDefaults.RAWDataLoadServer);

            Random r = new Random(123);

            MeaningfulTestTagDataGenerator generator = new MeaningfulTestTagDataGenerator(template, r);

            List<DicomDataset> allImages = new List<DicomDataset>();

            for (int i = 0; i < numberOfImages;)
            {
                int numberInSeries = r.Next(500);

                //don't generate more than we were told to
                numberInSeries = Math.Min(numberInSeries, numberOfImages - i);
                allImages.AddRange(generator.GenerateDatasets(numberInSeries));

                i += numberInSeries;
                Console.WriteLine("Created " + i + " Datasets So Far");
            }

            Assert.AreEqual(numberOfImages, allImages.Count);

            using (var tester = new MicroserviceTester(_globals.RabbitOptions, _globals.DicomRelationalMapperOptions))
            {
                using (var host = new DicomRelationalMapperHost(_globals, loadSmiLogConfig: false))
                {
                    tester.SendMessages(_globals.DicomRelationalMapperOptions, allImages.Select(GetFileMessageForDataset), true);

                    Console.WriteLine("Starting Host");
                    host.Start();

                    Stopwatch sw = Stopwatch.StartNew();
                    new TestTimelineAwaiter().Await(() => host.Consumer.AckCount == numberOfImages, null, 20 * 60 * 100); //1 minute

                    Console.Write("Time For DLE:" + sw.Elapsed.TotalSeconds + "s");
                    host.Stop("Test finished");
                }
            }

            foreach (Pipeline allObject in CatalogueRepository.GetAllObjects<Pipeline>())
                allObject.DeleteInDatabase();
        }


        [TestCase(500)]
        public void TestGetChunktOnly(int numberOfImages)
        {
            Random r = new Random(123);
            var template = ImageTableTemplateCollection.LoadFrom(_templateXml);

            MeaningfulTestTagDataGenerator generator = new MeaningfulTestTagDataGenerator(template, r);

            List<DicomDataset> allImages = new List<DicomDataset>();

            for (int i = 0; i < numberOfImages;)
            {
                int numberInSeries = r.Next(500);

                //don't generate more than we were told to
                numberInSeries = Math.Min(numberInSeries, numberOfImages - i);
                allImages.AddRange(generator.GenerateDatasets(numberInSeries));

                i += numberInSeries;
                Console.WriteLine("Created " + i + " Datasets So Far");
            }

            DicomDatasetCollectionSource source = new DicomDatasetCollectionSource();
            source.PreInitialize(new ExplicitListDicomDatasetWorklist(allImages.ToArray(), "amagad.dcm", new Dictionary<string, string>() { { "MessageGuid", "0x123" } }), new ThrowImmediatelyDataLoadEventListener()); ;
            source.FilenameField = "gggg";

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var dt = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
            source.Dispose(new ThrowImmediatelyDataLoadEventListener() { WriteToConsole = true }, null);

            sw.Stop();
            Console.WriteLine("GetChunk took " + sw.ElapsedMilliseconds);

            Assert.AreEqual(numberOfImages, dt.Rows.Count);
            Assert.AreEqual(numberOfImages, dt.Rows.Cast<DataRow>().Select(w => w["SOPInstanceUID"]).Distinct().Count());
        }

        [TestCase(DatabaseType.MySql, 500)]
        [TestCase(DatabaseType.MicrosoftSQLServer, 500)]
        public void TestBulkInsertOnly(DatabaseType databaseType, int numberOfImages)
        {
            foreach (Pipeline p in CatalogueRepository.GetAllObjects<Pipeline>())
                p.DeleteInDatabase();

            var db = GetCleanedServer(databaseType);

            var template = ImageTableTemplateCollection.LoadFrom(_templateXml);

            _globals = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

            _globals.DicomRelationalMapperOptions.DatabaseNamerType = typeof(MyFixedStagingDatabaseNamer).FullName;
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = ushort.MaxValue;
            _globals.DicomRelationalMapperOptions.MinimumBatchSize = numberOfImages;
            _globals.DicomRelationalMapperOptions.UseInsertIntoForRAWMigration = true;

            _helper = new DicomRelationalMapperTestHelper();
            _helper.SetupSuite(db, RepositoryLocator, _globals, typeof(DicomDatasetCollectionSource), root: null, template: template, persistentRaw: true);

            //do not use an explicit RAW data load server
            CatalogueRepository.GetServerDefaults().ClearDefault(PermissableDefaults.RAWDataLoadServer);

            Random r = new Random(123);

            MeaningfulTestTagDataGenerator generator = new MeaningfulTestTagDataGenerator(template, r);

            List<DicomDataset> allImages = new List<DicomDataset>();

            for (int i = 0; i < numberOfImages;)
            {
                int numberInSeries = r.Next(500);

                //don't generate more than we were told to
                numberInSeries = Math.Min(numberInSeries, numberOfImages - i);
                allImages.AddRange(generator.GenerateDatasets(numberInSeries));

                i += numberInSeries;
                Console.WriteLine("Created " + i + " Datasets So Far");
            }

            DicomDatasetCollectionSource source = new DicomDatasetCollectionSource();
            source.PreInitialize(new ExplicitListDicomDatasetWorklist(allImages.ToArray(), "amagad.dcm", new Dictionary<string, string>() { { "MessageGuid", "0x123" } }), new ThrowImmediatelyDataLoadEventListener()); ;
            source.FilenameField = "gggg";

            var dt = source.GetChunk(new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
            source.Dispose(new ThrowImmediatelyDataLoadEventListener() { WriteToConsole = true }, null);

            Assert.AreEqual(numberOfImages, allImages.Count);
            Assert.AreEqual(numberOfImages, dt.Rows.Count);

            var tables = _helper.LoadMetadata.GetDistinctTableInfoList(false);

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
                attacher.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
                attacher.Dispose(new ThrowImmediatelyDataLoadEventListener(), null);
            }
            catch (Exception e)
            {
                attacher.Dispose(new ThrowImmediatelyDataLoadEventListener(), e);
                throw;
            }

            foreach (var tableInfo in tables)
                Assert.AreEqual(numberOfImages,
                    tableInfo.Discover(DataAccessContext.InternalDataProcessing).GetRowCount(),
                    "Row count was wrong for " + tableInfo);

            foreach (Pipeline allObject in CatalogueRepository.GetAllObjects<Pipeline>())
                allObject.DeleteInDatabase();
        }

        private DicomFileMessage GetFileMessageForDataset(DicomDataset dicomDataset)
        {

            var root = TestContext.CurrentContext.TestDirectory;

            var f = Path.GetRandomFileName();
            var msg = new DicomFileMessage(root, Path.Combine(root, f + ".dcm"));
            msg.NationalPACSAccessionNumber = "ABC";
            msg.SeriesInstanceUID = dicomDataset.GetString(DicomTag.SeriesInstanceUID);
            msg.StudyInstanceUID = dicomDataset.GetString(DicomTag.StudyInstanceUID);
            msg.SOPInstanceUID = dicomDataset.GetString(DicomTag.SOPInstanceUID);
            msg.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(dicomDataset);
            return msg;
        }
    }

}
