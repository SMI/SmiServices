﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Applications.DicomDirectoryProcessor.Execution;
using Applications.DicomDirectoryProcessor.Options;
using BadMedicine;
using BadMedicine.Dicom;
using Dicom;
using FAnsi.Discovery;
using MapsDirectlyToDatabaseTable;
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.DicomRelationalMapper.Execution;
using Microservices.DicomRelationalMapper.Execution.Namers;
using Microservices.DicomTagReader.Execution;
using Microservices.IdentifierMapper.Execution;
using Microservices.IdentifierMapper.Execution.Swappers;
using Microservices.MongoDBPopulator.Execution;
using Microservices.Tests.RDMPTests;
using MongoDB.Driver;
using NLog;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataLoad.Engine.Checks.Checkers;
using Rdmp.Core.Logging.PastEvents;
using Rdmp.Dicom.PipelineComponents;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using ReusableLibraryCode.Checks;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Smi.Common.Tests;
using Tests.Common;
using DatabaseType = FAnsi.DatabaseType;

namespace Microservices.DicomRelationalMapper.Tests
{
    [RequiresRabbit, RequiresMongoDb, RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    public class MicroservicesIntegrationTest : DatabaseTests
    {
        public const string ScratchDatabaseName = "RDMPTests_ScratchArea";

        private DicomRelationalMapperTestHelper _helper;
        private GlobalOptions _globals;
        private const string MongoTestDbName = "nUnitTestDb";

        public void SetupSuite(DiscoveredDatabase server, bool persistentRaw = false, string modalityPrefix = null)
        {
            TestLogger.Setup();

            _globals = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

            _globals.UseTestValues(
                RequiresRabbit.GetConnectionFactory(),
                RequiresMongoDb.GetMongoClientSettings(),
                RequiresRelationalDb.GetRelationalDatabaseConnectionStrings(),
                ((TableRepository)RepositoryLocator.CatalogueRepository).ConnectionStringBuilder,
                ((TableRepository)RepositoryLocator.DataExportRepository).ConnectionStringBuilder);

            _helper = new DicomRelationalMapperTestHelper();
            _helper.SetupSuite(server, RepositoryLocator, _globals, typeof(DicomDatasetCollectionSource), null, null, persistentRaw, modalityPrefix);

            _globals.DicomRelationalMapperOptions.RetryOnFailureCount = 0;
            _globals.DicomRelationalMapperOptions.RetryDelayInSeconds = 0;

            //do not use an explicit RAW data load server
            CatalogueRepository.GetServerDefaults().ClearDefault(PermissableDefaults.RAWDataLoadServer);
        }

        [TearDown]
        public void TearDown()
        {
            //delete all joins
            foreach (JoinInfo j in CatalogueRepository.GetAllObjects<JoinInfo>())
                j.DeleteInDatabase();

            //delete everything from data export
            foreach (var t in new Type[] { typeof(ExtractionConfiguration), typeof(ExternalCohortTable), typeof(ExtractableDataSet) })
                foreach (IDeleteable o in DataExportRepository.GetAllObjects(t))
                    o.DeleteInDatabase();

            //delete everything from catalogue
            foreach (var t in new Type[] { typeof(Catalogue), typeof(TableInfo), typeof(LoadMetadata), typeof(Pipeline) })
                foreach (IDeleteable o in CatalogueRepository.GetAllObjects(t))
                    o.DeleteInDatabase();
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, typeof(GuidDatabaseNamer))]
        [TestCase(DatabaseType.MicrosoftSQLServer, typeof(GuidTableNamer))]
        [TestCase(DatabaseType.MySql, typeof(GuidDatabaseNamer))]
        [TestCase(DatabaseType.MySql, typeof(GuidTableNamer))]
        public void IntegrationTest_HappyPath(DatabaseType databaseType, Type namerType)
        {
            var server = GetCleanedServer(databaseType, ScratchDatabaseName);
            SetupSuite(server, false, "MR_");

            //this ensures that the ExtractionConfiguration.ID and Project.ID properties are out of sync (they are automnums).  Its just a precaution since we are using both IDs in places if we
            //had any bugs where we used the wrong one but they were the same then it would be obscured until production
            var p = new Project(DataExportRepository, "delme");
            p.DeleteInDatabase();

            _globals.DicomRelationalMapperOptions.Guid = new Guid("fc229fc3-f700-4515-86e8-e3d38b3d1823");
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = 5000;
            _globals.DicomRelationalMapperOptions.DatabaseNamerType = namerType.FullName;

            _helper.TruncateTablesIfExists();

            //Create test directory with a single image
            var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(IntegrationTest_HappyPath)));
            dir.Create();

            var arg = _helper.LoadMetadata.ProcessTasks.SelectMany(a => a.ProcessTaskArguments).Single(a => a.Name.Equals("ModalityMatchingRegex"));
            arg.SetValue(new Regex("([A-z]*)_.*$"));
            arg.SaveToDatabase();

            //clean up the directory
            foreach (FileInfo f in dir.GetFiles())
                f.Delete();

            TestData.Create(new FileInfo(Path.Combine(dir.FullName, "MyTestFile.dcm")));

            RunTest(dir, 1);
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, typeof(GuidDatabaseNamer))]
        [TestCase(DatabaseType.MySql, typeof(GuidDatabaseNamer))]
        public void IntegrationTest_NoFileExtensions(DatabaseType databaseType, Type namerType)
        {
            var server = GetCleanedServer(databaseType, ScratchDatabaseName);
            SetupSuite(server, false, "MR_");

            //this ensures that the ExtractionConfiguration.ID and Project.ID properties are out of sync (they are automnums).  Its just a precaution since we are using both IDs in places if we
            //had any bugs where we used the wrong one but they were the same then it would be obscured until production
            var p = new Project(DataExportRepository, "delme");
            p.DeleteInDatabase();

            _globals.DicomRelationalMapperOptions.Guid = new Guid("fc229fc3-f700-4515-86e8-e3d38b3d1823");
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = 5000;
            _globals.DicomRelationalMapperOptions.DatabaseNamerType = namerType.FullName;

            _globals.FileSystemOptions.DicomSearchPattern = "*";

            _helper.TruncateTablesIfExists();

            //Create test directory with a single image
            var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(IntegrationTest_NoFileExtensions)));
            dir.Create();

            var arg = _helper.LoadMetadata.ProcessTasks.SelectMany(a => a.ProcessTaskArguments).Single(a => a.Name.Equals("ModalityMatchingRegex"));
            arg.SetValue(new Regex("([A-z]*)_.*$"));
            arg.SaveToDatabase();

            //clean up the directory
            foreach (FileInfo f in dir.GetFiles())
                f.Delete();

            TestData.Create(new FileInfo(Path.Combine(dir.FullName, "Mr.010101"))); //this is legit a dicom file

            try
            {
                RunTest(dir, 1, (o) => o.DicomSearchPattern = "*");
            }
            finally
            {
                // Reset this in case it breaks other tests
                _globals.FileSystemOptions.DicomSearchPattern = "*.dcm";
            }
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, null)]
        [TestCase(DatabaseType.MySql, typeof(RejectAll))]
        public void IntegrationTest_Rejector(DatabaseType databaseType, Type rejector)
        {
            var server = GetCleanedServer(databaseType, ScratchDatabaseName);
            SetupSuite(server, false, "MR_");

            //this ensures that the ExtractionConfiguration.ID and Project.ID properties are out of sync (they are automnums).  Its just a precaution since we are using both IDs in places if we
            //had any bugs where we used the wrong one but they were the same then it would be obscured until production
            var p = new Project(DataExportRepository, "delme");
            p.DeleteInDatabase();

            _globals.DicomRelationalMapperOptions.Guid = new Guid("fc229fc3-f700-4515-86e8-e3d38b3d1823");
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = 5000;
            _globals.DicomRelationalMapperOptions.DatabaseNamerType = typeof(GuidDatabaseNamer).FullName;

            _globals.CohortExtractorOptions.RejectorType = rejector?.FullName;

            _globals.FileSystemOptions.DicomSearchPattern = "*";

            _helper.TruncateTablesIfExists();

            //Create test directory with a single image
            var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(IntegrationTest_NoFileExtensions)));
            dir.Create();

            var arg = _helper.LoadMetadata.ProcessTasks.SelectMany(a => a.ProcessTaskArguments).Single(a => a.Name.Equals("ModalityMatchingRegex"));
            arg.SetValue(new Regex("([A-z]*)_.*$"));
            arg.SaveToDatabase();

            //clean up the directory
            foreach (FileInfo f in dir.GetFiles())
                f.Delete();

            TestData.Create(new FileInfo(Path.Combine(dir.FullName, "Mr.010101"))); //this is legit a dicom file

            try
            {
                RunTest(dir, 1, (o) => o.DicomSearchPattern = "*");
            }
            finally
            {
                // Reset this in case it breaks other tests
                _globals.FileSystemOptions.DicomSearchPattern = "*.dcm";
            }
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, typeof(GuidDatabaseNamer))]
        [TestCase(DatabaseType.MicrosoftSQLServer, typeof(GuidTableNamer))]
        [TestCase(DatabaseType.MySql, typeof(GuidDatabaseNamer))]
        [TestCase(DatabaseType.MySql, typeof(GuidTableNamer))]
        public void IntegrationTest_HappyPath_WithIsolation(DatabaseType databaseType, Type namerType)
        {
            var server = GetCleanedServer(databaseType, ScratchDatabaseName);
            SetupSuite(server, false, "MR_");

            //this ensures that the ExtractionConfiguration.ID and Project.ID properties are out of sync (they are automnums).  Its just a precaution since we are using both IDs in places if we
            //had any bugs where we used the wrong one but they were the same then it would be obscured until production
            var p = new Project(DataExportRepository, "delme");
            p.DeleteInDatabase();

            _globals.DicomRelationalMapperOptions.Guid = new Guid("fc229fc3-f700-4515-86e8-e3d38b3d1823");
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = 5000;
            _globals.DicomRelationalMapperOptions.MinimumBatchSize = 3;
            _globals.DicomRelationalMapperOptions.DatabaseNamerType = namerType.FullName;

            _helper.TruncateTablesIfExists();

            //Create test directory with a single image
            var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(IntegrationTest_HappyPath_WithIsolation)));
            dir.Create();


            var ptIsolation = new ProcessTask(CatalogueRepository, _helper.LoadMetadata, LoadStage.AdjustRaw);
            ptIsolation.CreateArgumentsForClassIfNotExists<PrimaryKeyCollisionIsolationMutilation>();
            ptIsolation.Path = typeof(PrimaryKeyCollisionIsolationMutilation).FullName;
            ptIsolation.ProcessTaskType = ProcessTaskType.MutilateDataTable;
            ptIsolation.SaveToDatabase();

            var arg1 = _helper.LoadMetadata.ProcessTasks.SelectMany(a => a.ProcessTaskArguments).Single(a => a.Name.Equals("TablesToIsolate"));
            arg1.SetValue(new[] { _helper.StudyTableInfo, _helper.SeriesTableInfo, _helper.ImageTableInfo });
            arg1.SaveToDatabase();

            var db = new ExternalDatabaseServer(CatalogueRepository, "IsolationDatabase(live)", null);
            db.SetProperties(server);

            var arg2 = _helper.LoadMetadata.ProcessTasks.SelectMany(a => a.ProcessTaskArguments).Single(a => a.Name.Equals("IsolationDatabase"));
            arg2.SetValue(db);
            arg2.SaveToDatabase();

            var arg3 = _helper.LoadMetadata.ProcessTasks.SelectMany(a => a.ProcessTaskArguments).Single(a => a.Name.Equals("ModalityMatchingRegex"));
            arg3.SetValue(new Regex("([A-z]*)_.*$"));
            arg3.SaveToDatabase();

            //build the joins
            new JoinInfo(CatalogueRepository,
                _helper.ImageTableInfo.ColumnInfos.Single(c => c.GetRuntimeName().Equals("SeriesInstanceUID")),
                _helper.SeriesTableInfo.ColumnInfos.Single(c => c.GetRuntimeName().Equals("SeriesInstanceUID")),
                ExtractionJoinType.Right, null);

            new JoinInfo(CatalogueRepository,
                _helper.SeriesTableInfo.ColumnInfos.Single(c => c.GetRuntimeName().Equals("StudyInstanceUID")),
                _helper.StudyTableInfo.ColumnInfos.Single(c => c.GetRuntimeName().Equals("StudyInstanceUID")),
                ExtractionJoinType.Right, null);

            //start with Study table
            _helper.StudyTableInfo.IsPrimaryExtractionTable = true;
            _helper.StudyTableInfo.SaveToDatabase();

            //clean up the directory
            foreach (FileInfo f in dir.GetFiles())
                f.Delete();

            TestData.Create(new FileInfo(Path.Combine(dir.FullName, "MyTestFile.dcm")));

            var ds1 = new DicomDataset();
            ds1.Add(DicomTag.StudyInstanceUID, "1.2.3");
            ds1.Add(DicomTag.SeriesInstanceUID, "1.2.2");
            ds1.Add(DicomTag.SOPInstanceUID, "1.2.3");
            ds1.Add(DicomTag.PatientAge, "030Y");
            ds1.Add(DicomTag.PatientID, "123");
            ds1.Add(DicomTag.SOPClassUID, "1");
            ds1.Add(DicomTag.Modality, "MR");

            new DicomFile(ds1).Save(Path.Combine(dir.FullName, "abc.dcm"));

            var ds2 = new DicomDataset();
            ds2.Add(DicomTag.StudyInstanceUID, "1.2.3");
            ds2.Add(DicomTag.SeriesInstanceUID, "1.2.4");
            ds2.Add(DicomTag.SOPInstanceUID, "1.2.7");
            ds2.Add(DicomTag.PatientAge, "040Y"); //age is replicated but should be unique at study level so gets isolated
            ds2.Add(DicomTag.PatientID, "123");
            ds2.Add(DicomTag.SOPClassUID, "1");
            ds2.Add(DicomTag.Modality, "MR");

            new DicomFile(ds2).Save(Path.Combine(dir.FullName, "def.dcm"));

            var checks = new ProcessTaskChecks(_helper.LoadMetadata);
            checks.Check(new AcceptAllCheckNotifier());

            RunTest(dir, 1);

            Assert.AreEqual(1, _helper.ImageTable.GetRowCount());

            var isoTable = server.ExpectTable(_helper.ImageTable.GetRuntimeName() + "_Isolation");
            Assert.AreEqual(2, isoTable.GetRowCount());
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, true)]
        [TestCase(DatabaseType.MicrosoftSQLServer, false)]
        [TestCase(DatabaseType.MySql, true)]
        [TestCase(DatabaseType.MySql, false)]
        public void IntegrationTest_HappyPath_WithElevation(DatabaseType databaseType, bool persistentRaw)
        {
            var server = GetCleanedServer(databaseType, ScratchDatabaseName);
            SetupSuite(server, persistentRaw: persistentRaw);

            _globals.DicomRelationalMapperOptions.Guid = new Guid("fc229fc3-f888-4515-86e8-e3d38b3d1823");
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = 5000;

            _helper.TruncateTablesIfExists();

            //add the column to the table
            _helper.ImageTable.AddColumn("d_DerivationCodeMeaning", "varchar(100)", true, 5);

            var archiveTable = _helper.ImageTable.Database.ExpectTable(_helper.ImageTable.GetRuntimeName() + "_Archive");
            if (archiveTable.Exists())
                archiveTable.AddColumn("d_DerivationCodeMeaning", "varchar(100)", true, 5);

            new TableInfoSynchronizer(_helper.ImageTableInfo).Synchronize(new AcceptAllCheckNotifier());

            var elevationRules = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "ElevationConfig.xml"));
            File.WriteAllText(elevationRules.FullName,
@"<!DOCTYPE TagElevationRequestCollection
[
  <!ELEMENT TagElevationRequestCollection (TagElevationRequest*)>
  <!ELEMENT TagElevationRequest (ColumnName,ElevationPathway,Conditional?)>
  <!ELEMENT ColumnName (#PCDATA)>
  <!ELEMENT ElevationPathway (#PCDATA)>
  <!ELEMENT Conditional (ConditionalPathway,ConditionalRegex)>
  <!ELEMENT ConditionalPathway (#PCDATA)>
  <!ELEMENT ConditionalRegex (#PCDATA)>
]>

<TagElevationRequestCollection>
  <TagElevationRequest>
    <ColumnName>d_DerivationCodeMeaning</ColumnName>
    <ElevationPathway>DerivationCodeSequence->CodeMeaning</ElevationPathway>
  </TagElevationRequest>
</TagElevationRequestCollection>");

            var arg = _helper.DicomSourcePipelineComponent.PipelineComponentArguments.Single(a => a.Name.Equals("TagElevationConfigurationFile"));
            arg.SetValue(elevationRules);
            arg.SaveToDatabase();

            //Create test directory with a single image
            var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(IntegrationTest_HappyPath_WithElevation)));
            dir.Create();

            //clean up the directory
            foreach (FileInfo f in dir.GetFiles())
                f.Delete();

            TestData.Create(new FileInfo(Path.Combine(dir.FullName, "MyTestFile.dcm")));

            RunTest(dir, 1);

            var tbl = _helper.ImageTable.GetDataTable();
            Assert.AreEqual(1, tbl.Rows.Count);
            Assert.AreEqual("Full fidelity image, uncompressed or lossless compressed", tbl.Rows[0]["d_DerivationCodeMeaning"]);

            _helper.ImageTable.DropColumn(_helper.ImageTable.DiscoverColumn("d_DerivationCodeMeaning"));

            if (archiveTable.Exists())
                archiveTable.DropColumn(archiveTable.DiscoverColumn("d_DerivationCodeMeaning"));
        }

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void IntegrationTest_BumpyRide(DatabaseType databaseType)
        {
            var server = GetCleanedServer(databaseType, ScratchDatabaseName);
            SetupSuite(server);

            _globals.DicomRelationalMapperOptions.Guid = new Guid("6c7cfbce-1af6-4101-ade7-6537eea72e03");
            _globals.DicomRelationalMapperOptions.QoSPrefetchCount = 5000;
            _globals.IdentifierMapperOptions.QoSPrefetchCount = 50;
            _globals.DicomTagReaderOptions.NackIfAnyFileErrors = false;

            _helper.TruncateTablesIfExists();

            //Create test directory
            var dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(IntegrationTest_BumpyRide)));
            
            var r = new Random(500);

            //create a generator 
            using (var generator = new DicomDataGenerator(r, dir, "CT"))
            {
                generator.GenerateImageFiles(40,r);
                RunTest(dir, 40);
            }
        }
        private void RunTest(DirectoryInfo dir, int numberOfExpectedRows)
        {
            RunTest(dir, numberOfExpectedRows, null);
        }
        private void RunTest(DirectoryInfo dir, int numberOfExpectedRows, Action<FileSystemOptions> adjustFileSystemOptions)
        { 
            TestLogger.Setup();
            var logger = LogManager.GetLogger("MicroservicesIntegrationTest");

            _globals.FileSystemOptions.FileSystemRoot = TestContext.CurrentContext.TestDirectory;

            var readFromFatalErrors = new ConsumerOptions
            {
                QueueName = "TEST.FatalLoggingQueue"
            };

            ///////////////////////////////////// Directory //////////////////////////
            var processDirectoryOptions = new DicomDirectoryProcessorCliOptions();
            processDirectoryOptions.ToProcessDir = dir;
            processDirectoryOptions.DirectoryFormat = "Default";
            
            adjustFileSystemOptions?.Invoke(_globals.FileSystemOptions);

            //////////////////////////////////////////////// Mongo Db Populator ////////////////////////
            // Make this a GUID or something, should be unique per test
            var currentSeriesCollectionName = "Integration_HappyPath_Series" + DateTime.Now.Ticks;
            var currentImageCollectionName = "Integration_HappyPath_Image" + DateTime.Now.Ticks;

            _globals.MongoDbPopulatorOptions.SeriesCollection = currentSeriesCollectionName;
            _globals.MongoDbPopulatorOptions.ImageCollection = currentImageCollectionName;

            //use the test catalogue not the one in the combined app.config

            _globals.RDMPOptions.CatalogueConnectionString = ((TableRepository)RepositoryLocator.CatalogueRepository).DiscoveredServer.Builder.ConnectionString;
            _globals.RDMPOptions.DataExportConnectionString = ((TableRepository)RepositoryLocator.DataExportRepository).DiscoveredServer.Builder.ConnectionString;
            _globals.DicomRelationalMapperOptions.RunChecks = true;

            if (_globals.DicomRelationalMapperOptions.MinimumBatchSize < 1)
                _globals.DicomRelationalMapperOptions.MinimumBatchSize = 1;

            using (var tester = new MicroserviceTester(_globals.RabbitOptions, _globals.CohortExtractorOptions))
            {
                tester.CreateExchange(_globals.ProcessDirectoryOptions.AccessionDirectoryProducerOptions.ExchangeName, _globals.DicomTagReaderOptions);
                tester.CreateExchange(_globals.DicomTagReaderOptions.SeriesProducerOptions.ExchangeName, _globals.MongoDbPopulatorOptions.SeriesQueueConsumerOptions);
                tester.CreateExchange(_globals.DicomTagReaderOptions.ImageProducerOptions.ExchangeName, _globals.IdentifierMapperOptions);
                tester.CreateExchange(_globals.DicomTagReaderOptions.ImageProducerOptions.ExchangeName, _globals.MongoDbPopulatorOptions.ImageQueueConsumerOptions, true);
                tester.CreateExchange(_globals.IdentifierMapperOptions.AnonImagesProducerOptions.ExchangeName, _globals.DicomRelationalMapperOptions);
                tester.CreateExchange(_globals.RabbitOptions.FatalLoggingExchange, readFromFatalErrors);

                tester.CreateExchange(_globals.CohortExtractorOptions.ExtractFilesProducerOptions.ExchangeName, null);
                tester.CreateExchange(_globals.CohortExtractorOptions.ExtractFilesInfoProducerOptions.ExchangeName, null);

                #region Running Microservices

                var processDirectory = new DicomDirectoryProcessorHost(_globals, processDirectoryOptions, loadSmiLogConfig: false);
                processDirectory.Start();
                tester.StopOnDispose.Add(processDirectory);

                var dicomTagReaderHost = new DicomTagReaderHost(_globals, loadSmiLogConfig: false);
                dicomTagReaderHost.Start();
                tester.StopOnDispose.Add(dicomTagReaderHost);

                var mongoDbPopulatorHost = new MongoDbPopulatorHost(_globals, loadSmiLogConfig: false);
                mongoDbPopulatorHost.Start();
                tester.StopOnDispose.Add(mongoDbPopulatorHost);

                var identifierMapperHost = new IdentifierMapperHost(_globals, new SwapForFixedValueTester("FISHFISH"), loadSmiLogConfig: false);
                identifierMapperHost.Start();
                tester.StopOnDispose.Add(identifierMapperHost);

                new TestTimelineAwaiter().Await(() => dicomTagReaderHost.AccessionDirectoryMessageConsumer.AckCount >= 1);
                logger.Info("\n### DicomTagReader has processed its messages ###\n");

                // FIXME: This isn't exactly how the pipeline runs
                new TestTimelineAwaiter().Await(() => identifierMapperHost.Consumer.AckCount >= 1);
                logger.Info("\n### IdentifierMapper has processed its messages ###\n");

                using (var relationalMapperHost = new DicomRelationalMapperHost(_globals, loadSmiLogConfig: false))
                {
                    var start = DateTime.Now;

                    relationalMapperHost.Start();
                    tester.StopOnDispose.Add(relationalMapperHost);

                    Assert.True(mongoDbPopulatorHost.Consumers.Count == 2);
                    new TestTimelineAwaiter().Await(() => mongoDbPopulatorHost.Consumers[0].Processor.AckCount >= 1);
                    new TestTimelineAwaiter().Await(() => mongoDbPopulatorHost.Consumers[1].Processor.AckCount >= 1);
                    logger.Info("\n### MongoDbPopulator has processed its messages ###\n");

                    new TestTimelineAwaiter().Await(() => identifierMapperHost.Consumer.AckCount >= 1);//number of series
                    logger.Info("\n### IdentifierMapper has processed its messages ###\n");

                    Assert.AreEqual(0, dicomTagReaderHost.AccessionDirectoryMessageConsumer.NackCount);
                    Assert.AreEqual(0, identifierMapperHost.Consumer.NackCount);
                    Assert.AreEqual(0, ((Consumer)mongoDbPopulatorHost.Consumers[0]).NackCount);
                    Assert.AreEqual(0, ((Consumer)mongoDbPopulatorHost.Consumers[1]).NackCount);

                    
                    try
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                        new TestTimelineAwaiter().Await(() => relationalMapperHost.Consumer.AckCount >= numberOfExpectedRows, null, 30000, () => relationalMapperHost.Consumer.DleErrors); //number of image files 
                        logger.Info("\n### DicomRelationalMapper has processed its messages ###\n");
                    }
                    finally
                    {
                        //find out what happens from the logging database
                        var rdmpLogging = new Rdmp.Core.Logging.LogManager(_helper.LoadMetadata.GetDistinctLoggingDatabase());
                        
                        //if error was reported during the dicom relational mapper run
                        foreach (var dli in rdmpLogging.GetArchivalDataLoadInfos(_helper.LoadMetadata.GetDistinctLoggingTask(), null, null))
                            if(dli.StartTime > start)
                                foreach (ArchivalFatalError e in dli.Errors)
                                    logger.Error(e.Date.TimeOfDay + ":" + e.Source + ":" + e.Description);
                    }
                    
                    Assert.AreEqual(numberOfExpectedRows, _helper.ImageTable.GetRowCount(), "All images should appear in the image table");
                    Assert.LessOrEqual(_helper.SeriesTable.GetRowCount(), numberOfExpectedRows, "Only unique series data should appear in series table, there should be less unique series than images (or equal)");
                    Assert.LessOrEqual(_helper.StudyTable.GetRowCount(), numberOfExpectedRows, "Only unique study data should appear in study table, there should be less unique studies than images (or equal)");
                    Assert.LessOrEqual(_helper.StudyTable.GetRowCount(), _helper.SeriesTable.GetRowCount(), "There should be less studies than series (or equal)");

                    //make sure that the substitution identifier (that replaces old the PatientId) is the correct substitution (FISHFISH)/
                    Assert.AreEqual("FISHFISH", _helper.StudyTable.GetDataTable().Rows.OfType<DataRow>().First()["PatientId"]);

                    //The file size in the final table should be more than 0
                    Assert.Greater((long)_helper.ImageTable.GetDataTable().Rows.OfType<DataRow>().First()["DicomFileSize"],0);

                    dicomTagReaderHost.Stop("TestIsFinished");

                    mongoDbPopulatorHost.Stop("TestIsFinished");
                    DropMongoTestDb(_globals.MongoDatabases.DicomStoreOptions.HostName, _globals.MongoDatabases.DicomStoreOptions.Port);

                    identifierMapperHost.Stop("TestIsFinished");

                    relationalMapperHost.Stop("Test end");
                }

                //Now do extraction
                var extractorHost = new CohortExtractorHost(_globals, null, null, loadSmiLogConfig: false);

                extractorHost.Start();

                var extract = new ExtractionRequestMessage
                {
                    ExtractionJobIdentifier = Guid.NewGuid(),
                    ProjectNumber = "1234-5678",
                    ExtractionDirectory = "1234-5678_P1",
                    KeyTag = "SeriesInstanceUID",
                };

                foreach (DataRow row in _helper.ImageTable.GetDataTable().Rows)
                {
                    var ser = (string)row["SeriesInstanceUID"];

                    if (!extract.ExtractionIdentifiers.Contains(ser))
                        extract.ExtractionIdentifiers.Add(ser);
                }

                tester.SendMessage(_globals.CohortExtractorOptions, extract);

                //wait till extractor picked up the messages and dispatched the responses
                new TestTimelineAwaiter().Await(() => extractorHost.Consumer.AckCount == 1);

                extractorHost.Stop("TestIsFinished");

                tester.Shutdown();
            }


            #endregion
        }

        private void DropMongoTestDb(string mongoDbHostName, int mongoDbHostPort)
        {
            new MongoClient(new MongoClientSettings { Server = new MongoServerAddress(mongoDbHostName, mongoDbHostPort) }).DropDatabase(MongoTestDbName);
        }

        private class SwapForFixedValueTester : SwapIdentifiers
        {
            private readonly string _swapForString;


            public SwapForFixedValueTester(string swapForString)
            {
                _swapForString = swapForString;
            }


            public override void Setup(IMappingTableOptions mappingTableOptions) { }

            public override string GetSubstitutionFor(string toSwap, out string reason)
            {
                reason = null;
                Success++;
                CacheHit++;
                return _swapForString;
            }

            public override void ClearCache() { }
        }
    }
}
