using FAnsi;
using FAnsi.Discovery;
using NUnit.Framework;
using SmiServices.Common.Options;
using SmiServices.Microservices.IdentifierMapper.Swappers;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Tests.Common;
using TypeGuesser;

namespace SmiServices.IntegrationTests.Microservices.IdentifierMapper;

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

        var swapper = new TableLookupWithGuidFallbackSwapper();
        swapper.Setup(options);

        //cache hit
        var answer = swapper.GetSubstitutionFor("0101010101", out var reason);
        Assert.Multiple(() =>
        {
            Assert.That(answer, Is.EqualTo("0A0A0A0A0A"));
            Assert.That(reason, Is.Null);
        });

        var guidTable = swapper.GetGuidTableIfAny(options);

        Assert.Multiple(() =>
        {
            Assert.That(guidTable!.GetRuntimeName(), Is.EqualTo("Map_guid"));

            //The key column should match the SwapColumnName
            Assert.That(guidTable.DiscoverColumn("CHI"), Is.Not.Null);

            //but the swap column should always be called guid
            Assert.That(guidTable.DiscoverColumn("guid"), Is.Not.Null);
        });

        var answer2 = swapper.GetSubstitutionFor("0202020202", out reason);

        //should be a guid e.g. like "bc70d07d-4c77-4086-be1c-2971fd66ccf2"
        Assert.That(answer2, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(answer2!.Count(c => c == '-'), Is.EqualTo(4), $"Answer '{answer2}' did not look like a guid");
            Assert.That(reason, Is.Null);

            //make sure the guid mapping table has the correct row persisted for repeated calls
            Assert.That(guidTable, Is.Not.Null);
            Assert.That(guidTable?.Exists(), Is.True);
            Assert.That(guidTable?.GetRowCount(), Is.EqualTo(1));
            Assert.That(guidTable?.GetDataTable().Rows[0]["CHI"], Is.EqualTo("0202020202"));
            Assert.That(guidTable?.GetDataTable().Rows[0]["guid"], Is.EqualTo(answer2));


            //repeated misses should not result in more rows and should return the same guid (obviously)
            Assert.That(swapper.GetSubstitutionFor("0202020202", out reason), Is.EqualTo(answer2));
        });
        Assert.That(swapper.GetSubstitutionFor("0202020202", out reason), Is.EqualTo(answer2));
        Assert.Multiple(() =>
        {
            Assert.That(swapper.GetSubstitutionFor("0202020202", out reason), Is.EqualTo(answer2));

            Assert.That(guidTable?.GetRowCount(), Is.EqualTo(1));
            Assert.That(guidTable?.GetDataTable().Rows[0]["CHI"], Is.EqualTo("0202020202"));
            Assert.That(guidTable?.GetDataTable().Rows[0]["guid"], Is.EqualTo(answer2));
        });


        //now insert a legit mapping for 0202020202
        map.Insert(new Dictionary<string, object>
        {{"CHI","0202020202"},{"ECHI","0B0B0B0B0B"}});

        //note that the below line could fail if we ever implement miss caching (i.e. cache that we looked up the value and failed in the lookup swapper in which case this test would need to clearcache)

        //now that we have a cache hit we can lookup the good value
        Assert.That(swapper.GetSubstitutionFor("0202020202", out reason), Is.EqualTo("0B0B0B0B0B"));

    }

    [TestCase(DatabaseType.MySql, true)]
    [TestCase(DatabaseType.MySql, false)]
    [TestCase(DatabaseType.MicrosoftSQLServer, true)]
    [TestCase(DatabaseType.MicrosoftSQLServer, false)]
    public void Test_SwapValueTooLong(DatabaseType dbType, bool createGuidTableUpFront)
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

        using (var dt = new DataTable())
        {
            dt.Columns.Add("CHI");
            dt.Columns.Add("guid");

        }

        if (createGuidTableUpFront)
            db.CreateTable("Map_guid",
            [
                new("CHI",new DatabaseTypeRequest(typeof(string),30,null)),
                new("Guid",new DatabaseTypeRequest(typeof(string),36,null)),
            ]);


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
        var answer = swapper.GetSubstitutionFor("010101010031002300020320402054240204022433040301", out var reason);
        Assert.Multiple(() =>
        {
            Assert.That(answer, Is.Null);

            Assert.That(
reason, Is.EqualTo($"Supplied value was too long (48) - max allowed is ({(createGuidTableUpFront ? 30 : 10)})").IgnoreCase);
        });
    }
}
