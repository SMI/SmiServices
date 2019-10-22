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
    public class RequiresRelationalDb : CategoryAttribute, IApplyToContext
    {
        private readonly DatabaseType _type;

        public RequiresRelationalDb(DatabaseType type)
        {
            _type = type;
        }

        public void ApplyToContext(TestExecutionContext context)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            string filename = "RelationalDatabases.yaml";

            var connectionStrings = deserializer.Deserialize<ConStrs>(new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, filename)));
            
            FAnsi.Implementation.ImplementationManager.Load(
                typeof(FAnsi.Implementations.MySql.MySqlImplementation).Assembly,
                typeof(FAnsi.Implementations.MicrosoftSQL.MicrosoftSQLImplementation).Assembly,
                typeof(FAnsi.Implementations.Oracle.OracleImplementation).Assembly
                );

            string str;
            switch (_type)
            {
                case DatabaseType.MicrosoftSQLServer:
                    str = connectionStrings.SqlServer;
                    break;
                case DatabaseType.MySql:
                    str = connectionStrings.MySql;
                    break;
                case DatabaseType.Oracle:
                    str = connectionStrings.Oracle;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if(string.IsNullOrEmpty(str))
                Assert.Ignore("No connection string configured in "+filename +" for DatabaseType " + _type);

            var server = new DiscoveredServer(str, _type);

            if(!server.Exists())
                Assert.Ignore(_type + " is not running at '" + server.Name +"'");
        }

        class ConStrs
        {
            public string MySql { get; set; }
            public string SqlServer { get; set; }
            public string Oracle { get; set; }
        }
    }
}