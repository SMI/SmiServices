using System;
using System.IO;
using FAnsi;
using FAnsi.Discovery;
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
            FansiImplementations.Load();
            
            var connectionStrings = GetRelationalDatabaseConnectionStrings();
            var server = connectionStrings.GetServer(_type);

            if (server.Exists())
                return;

            string msg = $"Could not connect to {_type} at '{server.Name}' with the provided connection options";
            if (FailIfUnavailable)
                Assert.Fail(msg);
            else
                Assert.Ignore(msg);
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
            public string MySql {
                get => _MySql;
                set => _MySql = value.Replace("ssl-mode","sslmode",StringComparison.OrdinalIgnoreCase);
            }
            public string? SqlServer { get; set; }
            public string? Oracle { get; set; }

            public DiscoveredServer GetServer(DatabaseType dbType)
            {
                string? str = dbType switch
                {
                    DatabaseType.MicrosoftSQLServer => SqlServer,
                    DatabaseType.MySql => MySql,
                    DatabaseType.Oracle => Oracle,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (string.IsNullOrEmpty(str))
                    Assert.Ignore($"No connection string configured in {Filename} for DatabaseType {dbType}");

                return new DiscoveredServer(str, dbType);
            }
        }
    }
}
