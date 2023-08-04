using System.Collections.Generic;
using System.Data;
using System.Linq;
using FAnsi;
using FAnsi.Discovery;
using Microservices.IdentifierMapper.Execution.Swappers;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;
using Tests.Common;
using TypeGuesser;

namespace Microservices.IdentifierMapper.Tests
{
    [RequiresRelationalDb(DatabaseType.MicrosoftSQLServer)]
    [RequiresRelationalDb(DatabaseType.MySql)]
    public class TableLookupWithGuidFallbackSwapperTests : DatabaseTests
    {
        [TestCase(DatabaseType.MySql)]
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        public void Test_Cache1Hit1Miss(DatabaseType dbType)
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

            var options = new IdentifierMapperOptions
            {
                MappingTableName = map.GetFullyQualifiedName(),
                MappingConnectionString = db.Server.Builder.ConnectionString,
                SwapColumnName = "CHI",
                ReplacementColumnName = "ECHI",
                MappingDatabaseType = db.Server.DatabaseType
            };

            var swapper = new TableLookupWithGuidFallbackSwapper();
            swapper.Setup(options);

            //cache hit
            var answer = swapper.GetSubstitutionFor("0101010101",out var reason);
            Assert.AreEqual("0A0A0A0A0A",answer);
            Assert.IsNull(reason);

            var guidTable = swapper.GetGuidTableIfAny(options);
            
            Assert.AreEqual("Map_guid",guidTable.GetRuntimeName());

            //The key column should match the SwapColumnName
            Assert.IsNotNull(guidTable.DiscoverColumn("CHI"));
            
            //but the swap column should always be called guid
            Assert.IsNotNull(guidTable.DiscoverColumn("guid"));

            var answer2 = swapper.GetSubstitutionFor("0202020202",out reason);
            
            //should be a guid e.g. like "bc70d07d-4c77-4086-be1c-2971fd66ccf2"
            Assert.IsNotNull(answer2);
            Assert.AreEqual(4,answer2!.Count(c=>c=='-'),$"Answer '{answer2}' did not look like a guid");
            Assert.IsNull(reason);

            //make sure the guid mapping table has the correct row persisted for repeated calls
            Assert.IsTrue(guidTable.Exists());
            Assert.AreEqual(1,guidTable.GetRowCount());
            Assert.AreEqual("0202020202",guidTable.GetDataTable().Rows[0]["CHI"]);
            Assert.AreEqual(answer2,guidTable.GetDataTable().Rows[0]["guid"]);


            //repeated misses should not result in more rows and should return the same guid (obviously)
            Assert.AreEqual(answer2,swapper.GetSubstitutionFor("0202020202",out reason));
            Assert.AreEqual(answer2,swapper.GetSubstitutionFor("0202020202",out reason));
            Assert.AreEqual(answer2,swapper.GetSubstitutionFor("0202020202",out reason));

            Assert.AreEqual(1,guidTable.GetRowCount());
            Assert.AreEqual("0202020202",guidTable.GetDataTable().Rows[0]["CHI"]);
            Assert.AreEqual(answer2,guidTable.GetDataTable().Rows[0]["guid"]);


            //now insert a legit mapping for 0202020202
            map.Insert(new Dictionary<string, object>
            {{"CHI","0202020202"},{"ECHI","0B0B0B0B0B"}});

            //note that the below line could fail if we ever implement miss caching (i.e. cache that we looked up the value and failed in the lookup swapper in which case this test would need to clearcache)

            //now that we have a cache hit we can lookup the good value
            Assert.AreEqual("0B0B0B0B0B",swapper.GetSubstitutionFor("0202020202",out reason));

        }

        [TestCase(DatabaseType.MySql,true)]
        [TestCase(DatabaseType.MySql,false)]
        [TestCase(DatabaseType.MicrosoftSQLServer,true)]
        [TestCase(DatabaseType.MicrosoftSQLServer,false)]
        public void Test_SwapValueTooLong(DatabaseType dbType, bool createGuidTableUpFront)
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
            
            using (var dt = new DataTable())
            {
                dt.Columns.Add("CHI");
                dt.Columns.Add("guid");

            }

            if(createGuidTableUpFront)
                db.CreateTable("Map_guid",new DatabaseColumnRequest[]
                {
                    new DatabaseColumnRequest("CHI",new DatabaseTypeRequest(typeof(string),30,null)), 
                    new DatabaseColumnRequest("Guid",new DatabaseTypeRequest(typeof(string),36,null)), 
                });


            var options = new IdentifierMapperOptions
            {
                MappingTableName = map.GetFullyQualifiedName(),
                MappingConnectionString = db.Server.Builder.ConnectionString,
                SwapColumnName = "CHI",
                ReplacementColumnName = "ECHI",
                MappingDatabaseType = db.Server.DatabaseType
            };

            var swapper = new TableLookupWithGuidFallbackSwapper();
            swapper.Setup(options);

            //cache hit
            var answer = swapper.GetSubstitutionFor("010101010031002300020320402054240204022433040301",out var reason);
            Assert.IsNull(answer);

            StringAssert.AreEqualIgnoringCase(
                    $"Supplied value was too long (48) - max allowed is ({(createGuidTableUpFront?30:10)})", reason);
        }
    }
}
