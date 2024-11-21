
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.IO;
using YamlDotNet.Serialization;

namespace SmiServices.IntegrationTests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresMongoDb : RequiresExternalService
    {
        protected override string? ApplyToContextImpl()
        {
            var address = GetMongoClientSettings();

            Console.WriteLine("Checking the following configuration:" + Environment.NewLine + address);

            var client = new MongoClient(address);

            try
            {
                IAsyncCursor<BsonDocument> _ = client.ListDatabases();
            }
            catch (Exception e)
            {
                return
                    e is MongoNotPrimaryException
                        ? "Connected to non-primary MongoDB server. Check replication is enabled"
                        : $"Could not connect to MongoDB at {address}: {e}";
            }

            return null;
        }

        public static MongoClientSettings GetMongoClientSettings()
        {
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<A>(new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, "Mongo.yaml")));
        }

        class A : MongoClientSettings
        {
            private string? _host;
            private int _port;

            public string? Host
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

                DirectConnection = true;
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
