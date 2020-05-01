
using Dicom;
using DicomTypeTranslation;
using FAnsi.Discovery;
using Microservices.IdentifierMapper.Execution;
using Microservices.IdentifierMapper.Execution.Swappers;
using Microservices.IdentifierMapper.Messaging;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using BadMedicine;
using BadMedicine.Dicom;
using Tests.Common;
using DatabaseType = FAnsi.DatabaseType;

namespace Microservices.IdentifierMapper.Tests
{
    [TestFixture]
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    public class IdentifierMapperTests : DatabaseTests
    {
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void TestIdentifierSwap(DatabaseType type)
        {
            var mappingDataTable = new DataTable("IdMap");
            mappingDataTable.Columns.Add("priv");
            mappingDataTable.Columns.Add("pub");
            mappingDataTable.Rows.Add("010101", "020202");

            var db = GetCleanedServer(type);

            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;
            options.MappingTableName = db.CreateTable("IdMap", mappingDataTable).GetFullyQualifiedName();
            options.SwapColumnName = "priv";
            options.ReplacementColumnName = "pub";
            options.MappingDatabaseType = type;
            options.TimeoutInSeconds = 500;
            
            var swapper = new PreloadTableSwapper();
            swapper.Setup(options);

            var consumer = new IdentifierMapperQueueConsumer(Mock.Of<IProducerModel>(), swapper);

            var msg = GetTestDicomFileMessage();

            string reason;
            consumer.SwapIdentifier(msg, out reason);

            AssertDicomFileMessageHasPatientID(msg, "020202");
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, Test.Normal)]
        [TestCase(DatabaseType.MicrosoftSQLServer, Test.ProperlyFormatedChi)]
        [TestCase(DatabaseType.MySql, Test.Normal)]
        [TestCase(DatabaseType.MySql, Test.ProperlyFormatedChi)]
        public void TestIdentifierSwap_NoCache(DatabaseType type, Test test)
        {
            var mappingDataTable = new DataTable("IdMap");
            mappingDataTable.Columns.Add("priv");
            mappingDataTable.Columns.Add("pub");
            mappingDataTable.Rows.Add("010101", "020202");
            mappingDataTable.Rows.Add("0101010101", "0202020202");

            var db = GetCleanedServer(type);

            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;
            options.MappingTableName = db.CreateTable("IdMap", mappingDataTable).GetFullyQualifiedName();
            options.SwapColumnName = "priv";
            options.ReplacementColumnName = "pub";
            options.MappingDatabaseType = type;
            options.TimeoutInSeconds = 500;
            
            var swapper = new TableLookupSwapper();
            swapper.Setup(options);

            var consumer = new IdentifierMapperQueueConsumer(Mock.Of<IProducerModel>(), swapper);
            consumer.AllowRegexMatching = true;

            var msg = GetTestDicomFileMessage(test);

            string reason;
            consumer.SwapIdentifier(msg, out reason);

            if (test == Test.Normal)
                AssertDicomFileMessageHasPatientID(msg, "020202");
            else if (test == Test.ProperlyFormatedChi)
                AssertDicomFileMessageHasPatientID(msg, "0202020202");
            else
                Assert.Fail("Wrong test case?");
        }

        [TestCase(DatabaseType.MicrosoftSQLServer, 100, 25), RequiresRabbit]
        [TestCase(DatabaseType.MicrosoftSQLServer, 100, 0), RequiresRabbit]
        [TestCase(DatabaseType.MySql, 100, 0), RequiresRabbit]
        public void TestIdentifierSwap_RegexVsDeserialize(DatabaseType type, int batchSize, int numberOfRandomTagsPerDicom)
        {

            var options = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);

            var mappingDataTable = new DataTable("IdMap");
            mappingDataTable.Columns.Add("priv");
            mappingDataTable.Columns.Add("pub");
            mappingDataTable.Rows.Add("010101", "020202");
            mappingDataTable.Rows.Add("0101010101", "0202020202");


            var db = GetCleanedServer(type);

            options.IdentifierMapperOptions.MappingConnectionString = db.Server.Builder.ConnectionString;
            options.IdentifierMapperOptions.MappingTableName = db.CreateTable("IdMap", mappingDataTable).GetFullyQualifiedName();
            options.IdentifierMapperOptions.SwapColumnName = "priv";
            options.IdentifierMapperOptions.ReplacementColumnName = "pub";
            options.IdentifierMapperOptions.MappingDatabaseType = type;
            options.IdentifierMapperOptions.TimeoutInSeconds = 500;

            
            var swapper = new PreloadTableSwapper();
            swapper.Setup(options.IdentifierMapperOptions);

            var goodChis = new List<DicomFileMessage>();
            var badChis = new List<DicomFileMessage>();

            Console.WriteLine("Generating Test data...");

            List<Task> tasks = new List<Task>();
            object oTaskLock = new object();

            for (int i = 0; i < batchSize; i++)
            {
                var t = new Task(() =>
                {
                    var a = GetTestDicomFileMessage(Test.ProperlyFormatedChi, numberOfRandomTagsPerDicom);
                    var b = GetTestDicomFileMessage(Test.ProperlyFormatedChi, numberOfRandomTagsPerDicom);
                    lock (oTaskLock)
                    {
                        goodChis.Add(a);
                        badChis.Add(b);
                    }
                });

                t.Start();
                tasks.Add(t);


                if (i % Environment.ProcessorCount == 0)
                {
                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();
                }

                if (i % 100 == 0)
                    Console.WriteLine(i + " pairs done");
            }

            Task.WaitAll(tasks.ToArray());

            options.IdentifierMapperOptions.AllowRegexMatching = true;

            using (var tester = new MicroserviceTester(options.RabbitOptions, options.IdentifierMapperOptions))
            {
                tester.CreateExchange(options.IdentifierMapperOptions.AnonImagesProducerOptions.ExchangeName, null);

                Console.WriteLine("Pushing good messages to Rabbit...");
                tester.SendMessages(options.IdentifierMapperOptions, goodChis, true);

                var host = new IdentifierMapperHost(options, swapper, false);
                tester.StopOnDispose.Add(host);

                Console.WriteLine("Starting host");

                Stopwatch sw = Stopwatch.StartNew();
                host.Start();

                new TestTimelineAwaiter().Await(() => host.Consumer.AckCount == batchSize);

                Console.WriteLine("Good message processing (" + batchSize + ") took:" + sw.ElapsedMilliseconds + "ms");
                host.Stop("Test finished");
            }

            options.IdentifierMapperOptions.AllowRegexMatching = false;

            using (var tester = new MicroserviceTester(options.RabbitOptions, options.IdentifierMapperOptions))
            {
                tester.CreateExchange(options.IdentifierMapperOptions.AnonImagesProducerOptions.ExchangeName, null);

                Console.WriteLine("Pushing bad messages to Rabbit...");
                tester.SendMessages(options.IdentifierMapperOptions, badChis, true);

                var host = new IdentifierMapperHost(options, swapper,loadSmiLogConfig:false);
                tester.StopOnDispose.Add(host);

                Console.WriteLine("Starting host");

                Stopwatch sw = Stopwatch.StartNew();
                host.Start();

                new TestTimelineAwaiter().Await(() => host.Consumer.AckCount == batchSize);

                Console.WriteLine("Bad message processing (" + batchSize + ") took:" + sw.ElapsedMilliseconds + "ms");

                host.Stop("Test finished");
            }
        }

        [Ignore("Slow, tests lookup scalability")]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void TestIdentifierSwap_MillionsOfRows(DatabaseType type)
        {
            Console.WriteLine("DatabaseType:" + type);

            var mappingDataTable = new DataTable("IdMap");
            mappingDataTable.Columns.Add("priv");
            mappingDataTable.Columns.Add("pub");
            mappingDataTable.Rows.Add("abclkjlkjdefghijiklaskdf", Guid.NewGuid().ToString());
            var db = GetCleanedServer(type);

            DiscoveredTable tbl;

            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;
            options.MappingTableName = (tbl = db.CreateTable("IdMap", mappingDataTable)).GetFullyQualifiedName();
            options.SwapColumnName = "priv";
            options.ReplacementColumnName = "pub";
            options.MappingDatabaseType = type;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            mappingDataTable.Rows.Clear();
            using (var blk = tbl.BeginBulkInsert())
                for (int i = 0; i < 9999999; i++) //9 million
                {
                    mappingDataTable.Rows.Add(i.ToString(), Guid.NewGuid().ToString());

                    if (i % 100000 == 0)
                    {
                        blk.Upload(mappingDataTable);
                        mappingDataTable.Rows.Clear();
                        Console.WriteLine("Upload Table " + i + " rows " + sw.ElapsedMilliseconds);
                    }
                }

            sw.Stop();
            sw.Reset();


            sw.Start();
            var swapper = new PreloadTableSwapper();
            swapper.Setup(options);

            sw.Stop();
            Console.WriteLine("PreloadTableSwapper.Setup:" + sw.ElapsedMilliseconds);
            sw.Reset();

            sw.Start();
            string reason;
            var answer = swapper.GetSubstitutionFor("12325", out reason);
            sw.Stop();
            Console.WriteLine("Lookup Key:" + sw.ElapsedMilliseconds);
            sw.Reset();

            Assert.IsNotNull(answer);
            Assert.IsTrue(answer.Length > 20);
        }

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.Oracle)]
        public void TestIdentifierSwapForGuid(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);
            var mapTbl = db.ExpectTable("Map");

            //the declaration of what the guid namer table should be
            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;
            options.MappingTableName = mapTbl.GetFullyQualifiedName();
            options.SwapColumnName = "priv";
            options.ReplacementColumnName = "pub";
            options.MappingDatabaseType = dbType;

            var swapper = new ForGuidIdentifierSwapper();
            swapper.Setup(options);
            swapper.Setup(options);
            swapper.Setup(options); //this isn't just for the lols, this will test both the 'create it mode' and the 'discover it mode'

            var consumer = new IdentifierMapperQueueConsumer(Mock.Of<IProducerModel>(), swapper);

            var msg = GetTestDicomFileMessage();

            string reason;
            consumer.SwapIdentifier(msg, out reason);

            var newDs = DicomTypeTranslater.DeserializeJsonToDataset(msg.DicomDataset);
            var guidAllocated = newDs.GetValue<string>(DicomTag.PatientID, 0);

            var dt = mapTbl.GetDataTable();
            Assert.AreEqual(1, dt.Rows.Count);

            //e.g. '841A2E3E-B7C9-410C-A5D1-816B95C0E806'
            Assert.AreEqual(36, guidAllocated.Length);
        }


        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.Oracle)]
        public void TestIdentifierSwap2ForGuids(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);
            var mapTbl = db.ExpectTable("Map");

            //the declaration of what the guid namer table should be
            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;
            options.MappingTableName = mapTbl.GetFullyQualifiedName();
            options.SwapColumnName = "priv";
            options.ReplacementColumnName = "pub";
            options.MappingDatabaseType = dbType;

            var swapper = new ForGuidIdentifierSwapper();
            swapper.Setup(options);

            string reason;
            Assert.AreEqual(36, swapper.GetSubstitutionFor("01010101", out reason).Length);
            Assert.AreEqual(36, swapper.GetSubstitutionFor("02020202", out reason).Length);

            var answer1 = swapper.GetSubstitutionFor("03030303", out reason);

            var answer2 = swapper.GetSubstitutionFor("04040404", out reason);

            var answer3 = swapper.GetSubstitutionFor("03030303", out reason);

            Assert.AreEqual(answer1, answer3);

            Assert.AreNotEqual(answer1, answer2);
        }


        /// <summary>
        /// Tests two microservices inserting a guid at the same time (neither has a cached answer each thinks it's guid it allocated
        /// will be respected).  Correct behaviour is for the swappers to always read guids only from the database and in transaction
        /// safe manner.
        /// </summary>
        /// <param name="dbType"></param>
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.Oracle)]
        public void TestIdentifierSwap2ForGuids_WithSeperateSwappers(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);
            var mapTbl = db.ExpectTable("Map");

            //the declaration of what the guid namer table should be
            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;
            options.MappingTableName = mapTbl.GetFullyQualifiedName();
            options.SwapColumnName = "priv";
            options.ReplacementColumnName = "pub";
            options.MappingDatabaseType = dbType;

            var swapper1 = new ForGuidIdentifierSwapper();
            swapper1.Setup(options);

            var swapper2 = new ForGuidIdentifierSwapper();
            swapper2.Setup(options);

            var answer1 = swapper1.GetSubstitutionFor("01010101", out _);
            var answer2 = swapper2.GetSubstitutionFor("01010101", out _);

            Assert.AreEqual(answer1, answer2);

            Assert.IsNotNull(answer1);
            Assert.IsNotNull(answer2);
        }

        public enum Test
        {
            Normal,
            NoPatientTag,
            EmptyInPatientTag,
            ProperlyFormatedChi
        }


        [Test]
        [TestCase(Test.NoPatientTag)]
        [TestCase(Test.EmptyInPatientTag)]
        public void Test_BlankPatientIdentifier(Test testCase)
        {
            var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

            //the declaration of what the guid namer table should be
            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;

            var swapper = new SwapForFixedValueTester("meeee");
            swapper.Setup(options);

            var consumer = new IdentifierMapperQueueConsumer(Mock.Of<IProducerModel>(), swapper);

            var msg = GetTestDicomFileMessage(testCase: testCase);

            string reason;
            Assert.False(consumer.SwapIdentifier(msg, out reason));

            switch (testCase)
            {
                case Test.EmptyInPatientTag:
                    Assert.AreEqual("PatientID was blank", reason);
                    break;
                case Test.NoPatientTag:
                    Assert.AreEqual("Dataset did not contain PatientID", reason);
                    break;
            }
        }


        [Test]
        public void Test_NoMatchingIdentifierFound()
        {
            var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

            //the declaration of what the guid namer table should be
            var options = new IdentifierMapperOptions();
            options.MappingConnectionString = db.Server.Builder.ConnectionString;

            //null here means it will never find any identifier
            var swapper = new SwapForFixedValueTester(null);
            swapper.Setup(options);

            var consumer = new IdentifierMapperQueueConsumer(Mock.Of<IProducerModel>(), swapper);

            var msg = GetTestDicomFileMessage();
            string reason;

            Assert.False(consumer.SwapIdentifier(msg, out reason));
            Assert.AreEqual("Swapper Microservices.IdentifierMapper.Tests.SwapForFixedValueTester returned null", reason);
        }

        private void AssertDicomFileMessageHasPatientID(DicomFileMessage msg, string patientId)
        {
            var newDs = DicomTypeTranslater.DeserializeJsonToDataset(msg.DicomDataset);
            Assert.AreEqual(newDs.GetValue<string>(DicomTag.PatientID, 0), patientId);
        }

        private DicomFileMessage GetTestDicomFileMessage(Test testCase = Test.Normal, int numberOfRandomTagsPerDicom = 0)
        {
            var msg = new DicomFileMessage()
            {
                DicomFilePath = "Path/To/The/File.dcm",
                NationalPACSAccessionNumber = "1234",
                SOPInstanceUID = "1.2.3.4",
                SeriesInstanceUID = "1.2.3.4",
                StudyInstanceUID = "1.2.3.4",
            };

            DicomDataset ds;

            Random r = new Random(123);

            
            using (var generator = new DicomDataGenerator(r, null, "CT"))
                ds = generator.GenerateTestDataset(new Person(r), r);

            ds.AddOrUpdate(DicomTag.AccessionNumber, "1234");
            ds.AddOrUpdate(DicomTag.SOPInstanceUID, "1.2.3.4");
            ds.AddOrUpdate(DicomTag.SeriesInstanceUID, "1.2.3.4");
            ds.AddOrUpdate(DicomTag.StudyInstanceUID, "1.2.3.4");
            
            switch (testCase)
            {
                case Test.Normal:
                    ds.AddOrUpdate(DicomTag.PatientID, "010101");
                    break;
                case Test.NoPatientTag:
                    ds.Remove(DicomTag.PatientID);
                    break;
                case Test.EmptyInPatientTag:
                    ds.AddOrUpdate(DicomTag.PatientID, string.Empty);
                    break;
                case Test.ProperlyFormatedChi:
                    ds.AddOrUpdate(DicomTag.PatientID, "0101010101");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("testCase");
            }


            msg.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);

            return msg;
        }

        /// <summary>
        /// Tests that the control queue consumer correctly calls <see cref="ISwapIdentifiers.ClearCache"/>.
        /// Each implementation of the swapper should be tested to ensure it correctly deals with the message
        /// </summary>
        [Test]
        public void TestIdentifierSwap_ControlQueueRefresh()
        {
            TestLogger.Setup();

            var mockSwapper = new Mock<ISwapIdentifiers>();


            var controlConsumer = new IdentifierMapperControlMessageHandler(mockSwapper.Object);

            controlConsumer.ControlMessageHandler("refresh");

            mockSwapper.Verify(x => x.ClearCache(), Times.Once);
        }

        [Test]
        public void TestSwapCache()
        {
            var mappingDataTable = new DataTable("IdMap");
            mappingDataTable.Columns.Add("priv");
            mappingDataTable.Columns.Add("pub");

            mappingDataTable.Rows.Add("CHI-1", "REP-1");
            mappingDataTable.Rows.Add("CHI-2", "REP-2");

            DiscoveredDatabase db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

            GlobalOptions options = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);
            options.IdentifierMapperOptions = new IdentifierMapperOptions
            {
                MappingConnectionString = db.Server.Builder.ConnectionString,
                MappingTableName = db.CreateTable("IdMap", mappingDataTable).GetFullyQualifiedName(),
                SwapColumnName = "priv",
                ReplacementColumnName = "pub",
                MappingDatabaseType = DatabaseType.MicrosoftSQLServer,
                TimeoutInSeconds = 500
            };

            var swapper = new TableLookupSwapper();
            swapper.Setup(options.IdentifierMapperOptions);

            string _;
            string swapped = swapper.GetSubstitutionFor("CHI-1", out _);
            Assert.AreEqual("REP-1", swapped);
            swapped = swapper.GetSubstitutionFor("CHI-1", out _);
            Assert.AreEqual("REP-1", swapped);

            Assert.AreEqual(2, swapper.Success);
            Assert.AreEqual(1, swapper.CacheHit);

            swapped = swapper.GetSubstitutionFor("CHI-2", out _);
            Assert.AreEqual("REP-2", swapped);
            swapped = swapper.GetSubstitutionFor("CHI-2", out _);
            Assert.AreEqual("REP-2", swapped);

            Assert.AreEqual(4, swapper.Success);
            Assert.AreEqual(2, swapper.CacheHit);

            // Just to make sure...

            swapped = swapper.GetSubstitutionFor("CHI-1", out _);
            Assert.AreEqual("REP-1", swapped);

            Assert.AreEqual(5, swapper.Success);
            Assert.AreEqual(2, swapper.CacheHit);
        }
    }
}
