using System;
using System.IO;
using MongoDB.Driver;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using YamlDotNet.Serialization;

namespace Smi.Common.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface |
                    AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresMongoDb : CategoryAttribute,IApplyToContext
    {
        public void ApplyToContext(TestExecutionContext context)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            
            var address = deserializer.Deserialize<A>(new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, "Mongo.yaml")));
            Console.WriteLine("Checking the following configuration:" + Environment.NewLine + address);
            
            var client = new MongoClient(address);

            try
            {
                var dbs = client.ListDatabases();
            }
            catch (Exception)
            {
                Assert.Ignore("MongoDb is not running");
            }
        }
        

        class A :MongoClientSettings
        {
            private string _host;
            private int _port;

            public string Host
            {
                get => _host;
                set
                {
                    _host = value;
                    Server = new MongoServerAddress(_host, _port);
                }
            }

            public int Port
            {
                get => _port;
                set
                {
                    _port = value; 
                    Server = new MongoServerAddress(_host, _port);
                }
            }

            public A()
            {
                ConnectionMode = ConnectionMode.Direct;
                ConnectTimeout = new TimeSpan(0, 0, 0, 5);
                SocketTimeout = new TimeSpan(0, 0, 0, 5);
                HeartbeatTimeout = new TimeSpan(0, 0, 0, 5);
                ServerSelectionTimeout = new TimeSpan(0, 0, 0, 5);
                WaitQueueTimeout = new TimeSpan(0, 0, 05);
            }
            public override string ToString()
            {
                return Host + ":" + Port;
            }
        }
    }
}
