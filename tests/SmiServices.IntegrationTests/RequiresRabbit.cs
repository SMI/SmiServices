
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace SmiServices.IntegrationTests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface |
                    AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresRabbit : RequiresExternalService
    {
        public static readonly Lazy<IConnection> Connection = new(GetConnectionFactory);

        protected override string? ApplyToContextImpl()
        {
            try
            {
                using var model = Connection.Value.CreateModel();
                model.ExchangeDeclare("TEST.ControlExchange", ExchangeType.Topic, durable: true);
                return null;
            }
            catch (BrokerUnreachableException e)
            {
                return $"Could not connect to RabbitMQ{Environment.NewLine}{e.Message}";
            }
        }

        private static IConnection GetConnectionFactory()
        {
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var factory = deserializer.Deserialize<ConnectionFactory>(
                new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, "Rabbit.yaml")));
            factory.ContinuationTimeout = TimeSpan.FromSeconds(5);
            factory.HandshakeContinuationTimeout = TimeSpan.FromSeconds(5);
            factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(5);
            factory.SocketReadTimeout = TimeSpan.FromSeconds(5);
            factory.SocketWriteTimeout = TimeSpan.FromSeconds(5);
            return factory.CreateConnection();
        }
    }
}
