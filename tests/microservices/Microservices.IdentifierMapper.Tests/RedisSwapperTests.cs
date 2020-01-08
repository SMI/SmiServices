using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FAnsi;
using FAnsi.Discovery;
using Microservices.IdentifierMapper.Execution.Swappers;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;
using StackExchange.Redis;
using Tests.Common;

namespace Microservices.IdentifierMapper.Tests
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [RequiresRelationalDb(DatabaseType.MySql)]
    class RedisSwapperTests : DatabaseTests
    {
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
                map = db.CreateTable("Map",dt);
            }

            var options = new IdentifierMapperOptions()
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
                swapper = new RedisSwapper();
                swapper.Setup(options);
                swapper.ClearCache();
            }
            catch (RedisConnectionException  )
            {
                Assert.Inconclusive();
                throw new Exception("To keep static analysis happy, btw Redis was unavailable");
            }
            
            //hit on the lookup table
            string answer = swapper.GetSubstitutionFor("0101010101",out string reason);
            Assert.AreEqual("0A0A0A0A0A",answer);
            Assert.IsNull(reason);

            //hit didn't come from Redis
            Assert.AreEqual(0,swapper.CacheHit);
            Assert.AreEqual(1,swapper.Success);

            
            //hit from Redis
            string answer2 = swapper.GetSubstitutionFor("0101010101",out string reason2);
            Assert.AreEqual("0A0A0A0A0A",answer);
            Assert.IsNull(reason);
            
            //hit must come from Redis
            Assert.AreEqual(1,swapper.CacheHit);
            Assert.AreEqual(2,swapper.Success);


        }
    }
}
