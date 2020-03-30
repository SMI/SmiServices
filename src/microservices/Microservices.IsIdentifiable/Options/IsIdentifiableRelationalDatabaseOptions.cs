using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using FAnsi;

namespace Microservices.IsIdentifiable.Options
{
    [Verb("db")]
    public class IsIdentifiableRelationalDatabaseOptions : IsIdentifiableAbstractOptions
    {
        [Option('d', HelpText = "Full connection string to the database storing the table to be evaluated", Required = true)]
        public string DatabaseConnectionString { get; set; }

        [Option('t', HelpText = "The unqualified name of the table to evaluate", Required = true)]
        public string TableName { get; set; }

        [Option('p', HelpText = "DBMS Provider type - 'MicrosoftSQLServer','MySql' or 'Oracle'", Required = true)]
        public DatabaseType DatabaseType { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Run on a MySql database", new IsIdentifiableRelationalDatabaseOptions
                {
                    DatabaseConnectionString = "Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;",
                    DatabaseType = DatabaseType.MySql,
                    TableName = "MyTable",
                    StoreReport = true

                });
                yield return new Example(
                    "Run on an Sql Server database",
                    new IsIdentifiableRelationalDatabaseOptions
                    {
                        DatabaseConnectionString = "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;",
                        DatabaseType = DatabaseType.MicrosoftSQLServer,
                        TableName = "MyTable",
                        StoreReport = true
                    });
            }
        }


        public override string GetTargetName()
        {
            return TableName;
        }
    }
}
