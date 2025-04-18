using FAnsi.Discovery;
using FAnsi;
using NUnit.Framework;
using Npgsql;
using Tests.Common;

namespace SmiServices.IntegrationTests;

internal static class PostgresFixes
{
    /// <summary>
    /// RDMP DatabaseTests does not currently work with Postgres due to a bug in FAnsi with checking server existence
    /// on the non-default "postgres" database
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="type"></param>
    public static void GetCleanedServerPostgresFix(TestDatabasesSettings settings, DatabaseType type)
    {
        if (type != DatabaseType.PostgreSql)
            return;

        var server = new DiscoveredServer(settings.PostgreSql, DatabaseType.PostgreSql);
        var scratchDatabaseName = TestDatabaseNames.GetConsistentName("ScratchArea");

        try
        {
            server.CreateDatabase(scratchDatabaseName);
        }
        catch (PostgresException e)
        {
            if(!e.Message.Contains("already exists"))
                throw;
        }

        Assert.That(server.ExpectDatabase(scratchDatabaseName).Exists(), Is.True);
    }
}
