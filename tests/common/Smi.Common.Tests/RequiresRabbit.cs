
using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using RabbitMQ.Client;
using YamlDotNet.Serialization;

namespace Smi.Common.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface |
                    AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresRabbit : RequiresExternalService, IApplyToContext
    {
        public void ApplyToContext(TestExecutionContext context)
        {

            var factory = GetConnectionFactory();

            try
            {
                factory.ContinuationTimeout = new TimeSpan(0, 0, 5);
                factory.HandshakeContinuationTimeout = new TimeSpan(0, 0, 5);
                factory.RequestedConnectionTimeout = 5000;
                factory.SocketReadTimeout = 5000;
                factory.SocketWriteTimeout = 5000;

                IConnection conn = factory.CreateConnection();
                conn.Close();

            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Uri:         " + factory.Uri);
                sb.AppendLine("Host:        " + factory.HostName);
                sb.AppendLine("VirtualHost: " + factory.VirtualHost);
                sb.AppendLine("UserName:    " + factory.UserName);
                sb.AppendLine("Port:        " + factory.Port);

                string msg = $"Could not connect to RabbitMQ {Environment.NewLine}{sb}{Environment.NewLine}{e.Message}";
                if (!FailIfUnavailable)
                    Assert.Ignore(msg);
                else
                    Assert.Fail(msg);
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