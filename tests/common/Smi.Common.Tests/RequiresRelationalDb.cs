using System;
using System.IO;
using FAnsi;
using FAnsi.Discovery;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using YamlDotNet.Serialization;

namespace Smi.Common.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface |
                    AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresRelationalDb : RequiresExternalService, IApplyToContext
    {
        private readonly DatabaseType _type;
        private const string Filename = "RelationalDatabases.yaml";

        public RequiresRelationalDb(DatabaseType type)
        {
            _type = type;
        }

        public void ApplyToContext(TestExecutionContext context)
        {
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<MicrosoftSQLImplementation>();
            ImplementationManager.Load<OracleImplementation>();
            ImplementationManager.Load<PostgreSqlImplementation>();

            var connectionStrings = GetRelationalDatabaseConnectionStrings();
            var server = connectionStrings.GetServer(_type);

            if (server.Exists())
                return;

            if (!FailIfUnavailable)
                Assert.Ignore(_type + " is not running at '" + server.Name + "'");
            else
                Assert.Fail(_type + " is not running at '" + server.Name + "'");
        }

        public static ConStrs GetRelationalDatabaseConnectionStrings()
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<ConStrs>(new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, Filename)));
        }

        public class ConStrs
        {
            private string _MySql;
            public string MySql { get { return _MySql; } set { _MySql = value.Replace("ssl-mode","sslmode",StringComparison.OrdinalIgnoreCase); } }
            public string SqlServer { get; set; }
            public string Oracle { get; set; }

            public DiscoveredServer GetServer(DatabaseType dbType)
            {
                string str;
                switch (dbType)
                {
                    case DatabaseType.MicrosoftSQLServer:
                        str = SqlServer;
                        break;
                    case DatabaseType.MySql:
                        str = MySql;
                        break;
                    case DatabaseType.Oracle:
                        str = Oracle;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (string.IsNullOrEmpty(str))
                    Assert.Ignore("No connection string configured in " + Filename + " for DatabaseType " + dbType);

                return new DiscoveredServer(str, dbType);
            }
        }
    }
}