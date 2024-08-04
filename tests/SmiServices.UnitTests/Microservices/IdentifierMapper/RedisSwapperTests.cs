using System;
using System.Data;
using System.Linq;
using FAnsi;
using FAnsi.Discovery;
using NUnit.Framework;
using SmiServices.Common.Options;
using SmiServices.Microservices.IdentifierMapper.Swappers;
using SmiServices.UnitTests.Common;
using StackExchange.Redis;
using Tests.Common;

namespace SmiServices.UnitTests.Microservices.IdentifierMapper
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [RequiresRelationalDb(DatabaseType.MySql)]
    class RedisSwapperTests : DatabaseTests
    {
        private const string TestRedisServer = "localhost";

        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void Test_Redist_CacheUsage(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            DiscoveredTable map;

            using (var dt = new DataTable())
            {
                dt.Columns.Add("CHI");
                dt.Columns.Add("ECHI");

                dt.Rows.Add("0101010101", "0A0A0A0A0A");
                map = db.CreateTable("Map", dt);
            }

            var options = new IdentifierMapperOptions
            {
                MappingTableName = map.GetFullyQualifiedName(),
                MappingConnectionString = db.Server.Builder.ConnectionString,
                SwapColumnName = "CHI",
                ReplacementColumnName = "ECHI",
                MappingDatabaseType = db.Server.DatabaseType
            };

            RedisSwapper swapper;

            try
            {
                swapper = new RedisSwapper(TestRedisServer, new TableLookupWithGuidFallbackSwapper());
                swapper.Setup(options);

                ClearRedisServer();
            }
            catch (RedisConnectionException)
            {
                Assert.Inconclusive();
                throw new Exception("To keep static analysis happy, btw Redis was unavailable");
            }

            //hit on the lookup table
            string? answer = swapper.GetSubstitutionFor("0101010101", out string? reason);
            Assert.Multiple(() =>
            {
                Assert.That(answer, Is.EqualTo("0A0A0A0A0A"));
                Assert.That(reason, Is.Null);

                //hit didn't come from Redis
                Assert.That(swapper.CacheHit, Is.EqualTo(0));
                Assert.That(swapper.Success, Is.EqualTo(1));
            });


            //hit from Redis
            string? answer2 = swapper.GetSubstitutionFor("0101010101", out string? reason2);
            Assert.Multiple(() =>
            {
                Assert.That(answer, Is.EqualTo("0A0A0A0A0A"));
                Assert.That(reason, Is.Null);

                //hit must come from Redis
                Assert.That(swapper.CacheHit, Is.EqualTo(1));
                Assert.That(swapper.Success, Is.EqualTo(2));
            });
        }




        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void Test_Redist_CacheMisses(DatabaseType dbType)
        {
            var db = GetCleanedServer(dbType);

            DiscoveredTable map;

            using (var dt = new DataTable())
            {
                dt.Columns.Add("CHI");
                dt.Columns.Add("ECHI");

                dt.Rows.Add("0101010101", "0A0A0A0A0A");
                map = db.CreateTable("Map", dt);
            }

            var options = new IdentifierMapperOptions
            {
                MappingTableName = map.GetFullyQualifiedName(),
                MappingConnectionString = db.Server.Builder.ConnectionString,
                SwapColumnName = "CHI",
                ReplacementColumnName = "ECHI",
                MappingDatabaseType = db.Server.DatabaseType
            };

            RedisSwapper swapper;

            try
            {
                swapper = new RedisSwapper(TestRedisServer, new TableLookupSwapper());
                swapper.Setup(options);

                ClearRedisServer();
            }
            catch (RedisConnectionException)
            {
                Assert.Inconclusive();
                throw new Exception("To keep static analysis happy, btw Redis was unavailable");
            }

            //hit on the lookup table
            string? answer = swapper.GetSubstitutionFor("GOGOGO", out string? reason);
            Assert.Multiple(() =>
            {
                Assert.That(answer, Is.Null);
                Assert.That(reason, Is.EqualTo("No match found for 'GOGOGO'"));

                //hit didn't come from Redis
                Assert.That(swapper.CacheHit, Is.EqualTo(0));
                Assert.That(swapper.Fail, Is.EqualTo(1));
            });

            //hit from Redis
            string? answer2 = swapper.GetSubstitutionFor("GOGOGO", out string? reason2);
            Assert.Multiple(() =>
            {
                Assert.That(answer2, Is.Null);
                Assert.That(reason2, Is.EqualTo("Value 'GOGOGO' was cached in Redis as missing (i.e. no mapping was found)"));

                //hit must come from Redis
                Assert.That(swapper.CacheHit, Is.EqualTo(1));
                Assert.That(swapper.Fail, Is.EqualTo(2));
            });
        }

        private void ClearRedisServer()
        {
            using var admin = ConnectionMultiplexer.Connect($"{TestRedisServer},allowAdmin=true");
            foreach (var server in admin.GetEndPoints().Select(e => admin.GetServer(e)))
                server.FlushAllDatabases();
        }
    }
}
