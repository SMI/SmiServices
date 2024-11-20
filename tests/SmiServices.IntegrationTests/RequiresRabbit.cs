
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
        protected override string? ApplyToContextImpl()
        {
            var factory = GetConnectionFactory();
            factory.ContinuationTimeout = TimeSpan.FromSeconds(5);
            factory.HandshakeContinuationTimeout = TimeSpan.FromSeconds(5);
            factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(5);
            factory.SocketReadTimeout = TimeSpan.FromSeconds(5);
            factory.SocketWriteTimeout = TimeSpan.FromSeconds(5);

            try
            {
                using var conn = factory.CreateConnectionAsync().Result;
                using var model = conn.CreateChannelAsync().Result;
                model.ExchangeDeclareAsync("TEST.ControlExchange", ExchangeType.Topic, durable: true).Wait();
                return null;
            }
            catch (BrokerUnreachableException e)
            {
                StringBuilder sb = new();

                sb.AppendLine($"Uri:         {factory.Uri}");
                sb.AppendLine($"Host:        {factory.HostName}");
                sb.AppendLine($"VirtualHost: {factory.VirtualHost}");
                sb.AppendLine($"UserName:    {factory.UserName}");
                sb.AppendLine($"Port:        {factory.Port}");

                return $"Could not connect to RabbitMQ {Environment.NewLine}{sb}{Environment.NewLine}{e.Message}";
            }
        }

        public static ConnectionFactory GetConnectionFactory()
        {
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<ConnectionFactory>(new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, "Rabbit.yaml")));
        }
    }
}
