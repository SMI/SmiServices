﻿using System;
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
                swapper = new RedisSwapper(TestRedisServer,new TableLookupWithGuidFallbackSwapper());
                swapper.Setup(options);

                ClearRedisServer();
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
                swapper = new RedisSwapper(TestRedisServer,new TableLookupSwapper());
                swapper.Setup(options);

                ClearRedisServer();
            }
            catch (RedisConnectionException  )
            {
                Assert.Inconclusive();
                throw new Exception("To keep static analysis happy, btw Redis was unavailable");
            }
            
            //hit on the lookup table
            string answer = swapper.GetSubstitutionFor("GOGOGO",out string reason);
            Assert.IsNull(answer);
            Assert.AreEqual("No match found for 'GOGOGO'",reason);

            //hit didn't come from Redis
            Assert.AreEqual(0,swapper.CacheHit);
            Assert.AreEqual(1,swapper.Fail);
            
            //hit from Redis
            string answer2 = swapper.GetSubstitutionFor("GOGOGO",out string reason2);
            Assert.IsNull(answer2);
            Assert.AreEqual("Value 'GOGOGO' was cached in Redis as missing (i.e. no mapping was found)",reason2);
            
            //hit must come from Redis
            Assert.AreEqual(1,swapper.CacheHit);
            Assert.AreEqual(2,swapper.Fail);
        }

        private void ClearRedisServer()
        {
            using(var admin = ConnectionMultiplexer.Connect(TestRedisServer +",allowAdmin=true"))
                foreach (var server in admin.GetEndPoints().Select(e=> admin.GetServer(e)))
                    server.FlushAllDatabases();
        }
    }
}
