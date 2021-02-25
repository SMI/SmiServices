﻿
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;
using System.Threading.Tasks;

namespace Smi.Common.MongoDB.Tests
{
    [TestFixture, RequiresMongoDb]
    public class MongoQueryParserTests
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private MongoDbOptions _mongoOptions;

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            GlobalOptions globalOptions = new GlobalOptionsFactory().Load();
            _mongoOptions = globalOptions.MongoDatabases.DicomStoreOptions;
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

        private const string QUERY_BASIC = "{\"find\":{\"SeriesDate\":{\"$regex\":\"^2007\"}}}";
        private const string QUERY_ADVANCED = "{\"find\":{\"SeriesDate\":\"20170201\"},\"sort\":{\"_id\":-1},\"skip\":123,\"limit\":456}";

        [Test]
        [TestCase(QUERY_BASIC, null, null)]
        [TestCase(QUERY_ADVANCED, 123, 456)]
        public void TestParseQuery(string jsonQuery, int? expectedSkip, int? expectedLimit)
        {
            MongoClient mongoClient = MongoClientHelpers.GetMongoClient(_mongoOptions, "MongoQueryParserTests");

            IMongoDatabase database = mongoClient.GetDatabase("test");
            IMongoCollection<BsonDocument> coll = database.GetCollection<BsonDocument>("test");

            var findOptions = new FindOptions<BsonDocument> { BatchSize = 1 };

            Task<IAsyncCursor<BsonDocument>> t = MongoQueryParser.GetCursor(coll, findOptions, jsonQuery);

            t.Wait(1_000);
            Assert.IsTrue(t.IsCompleted);
            Assert.IsFalse(t.IsFaulted);

            using (IAsyncCursor<BsonDocument> _ = t.Result)
            {
                _logger.Info("Received new batch");

                Assert.AreEqual(expectedSkip, findOptions.Skip);
                Assert.AreEqual(expectedLimit, findOptions.Limit);
            }
        }

        #endregion
    }
}
