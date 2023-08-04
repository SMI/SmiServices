
using Microservices.MongoDBPopulator.Execution;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Smi.Common.Tests;
using System.Collections.Generic;

namespace Microservices.MongoDBPopulator.Tests.Execution
{
    [TestFixture, RequiresMongoDb]
    public class MongoDbAdapterTests
    {
        private MongoDbPopulatorTestHelper _helper = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _helper = new MongoDbPopulatorTestHelper();
            _helper.SetupSuite();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _helper.Dispose();
        }

        /// <summary>
        /// Test basic write operation of the adapter
        /// </summary>
        [Test]
        public void TestBasicWrite()
        {
            string collectionName = MongoDbPopulatorTestHelper.GetCollectionNameForTest("TestBasicWrite");
            var adapter = new MongoDbAdapter("TestApplication", _helper.Globals.MongoDatabases.DicomStoreOptions!,
                collectionName);

            var testDoc = new BsonDocument
            {
                {"hello", "world"}
            };

            WriteResult result = adapter.WriteMany(new List<BsonDocument> { testDoc });

            Assert.True(result == WriteResult.Success);
            Assert.True(_helper.TestDatabase.GetCollection<BsonDocument>(collectionName)
                            .CountDocuments(new BsonDocument()) == 1);

            BsonDocument doc =
                _helper.TestDatabase.GetCollection<BsonDocument>(collectionName).Find(_ => true).ToList()[0];

            Assert.True(doc.Equals(testDoc));

            var toWrite = new List<BsonDocument>();

            for (var i = 0; i < 99; i++)
                toWrite.Add(new BsonDocument { { "hello", i } });

            result = adapter.WriteMany(toWrite);

            Assert.True(result == WriteResult.Success);
            Assert.True(_helper.TestDatabase.GetCollection<BsonDocument>(collectionName)
                            .CountDocuments(new BsonDocument()) == 100);
        }
    }
}
