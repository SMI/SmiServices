using FAnsi;
using FAnsi.Discovery;
using Tests.Common;

namespace SmiServices.IntegrationTests;

internal static class PostgresFixes
{
    public static void GetCleanedServerPostgresFix(TestDatabasesSettings settings, DatabaseType type)
    {
        if (type != DatabaseType.PostgreSql)
            return;

        var server = new DiscoveredServer(settings.PostgreSql, DatabaseType.PostgreSql);
        if (server.GetCurrentDatabase() != null)
            return;

        var testDatabaseName = TestDatabaseNames.GetConsistentName("ScratchArea");
        server.ChangeDatabase("postgres");
        var database = server.ExpectDatabase(testDatabaseName);
        if (!database.Exists())
            database.Create();
    }

}
