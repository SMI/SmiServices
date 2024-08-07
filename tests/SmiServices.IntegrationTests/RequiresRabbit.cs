
using NUnit.Framework;
using NUnit.Framework.Internal;
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
        protected override void ApplyToContextImpl(TestExecutionContext context)
        {

            var factory = GetConnectionFactory();
            factory.ContinuationTimeout = TimeSpan.FromSeconds(5);
            factory.HandshakeContinuationTimeout = TimeSpan.FromSeconds(5);
            factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(5);
            factory.SocketReadTimeout = TimeSpan.FromSeconds(5);
            factory.SocketWriteTimeout = TimeSpan.FromSeconds(5);

            try
            {
                using var conn = factory.CreateConnection();
                using var model = conn.CreateModel();
                model.ExchangeDeclare("TEST.ControlExchange", ExchangeType.Topic, durable: true);
            }
            catch (BrokerUnreachableException e)
            {
                StringBuilder sb = new();

                sb.AppendLine($"Uri:         {factory.Uri}");
                sb.AppendLine($"Host:        {factory.HostName}");
                sb.AppendLine($"VirtualHost: {factory.VirtualHost}");
                sb.AppendLine($"UserName:    {factory.UserName}");
                sb.AppendLine($"Port:        {factory.Port}");

                string msg = $"Could not connect to RabbitMQ {Environment.NewLine}{sb}{Environment.NewLine}{e.Message}";

                // NOTE(rkm 2021-01-30) Don't fail for Windows CI builds
                bool shouldFail = FailIfUnavailable && !Environment.OSVersion.ToString().ToLower().Contains("windows");

                if (shouldFail)
                    Assert.Fail(msg);
                else
                    Assert.Ignore(msg);
            }
        }

        public static ConnectionFactory GetConnectionFactory()
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<ConnectionFactory>(new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, "Rabbit.yaml")));
        }

    }
}
