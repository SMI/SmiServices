
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
    public class RequiresRabbit : CategoryAttribute, IApplyToContext
    {
        public void ApplyToContext(TestExecutionContext context)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            var factory = deserializer.Deserialize<ConnectionFactory>(new StreamReader(Path.Combine(TestContext.CurrentContext.TestDirectory, "Rabbit.yaml")));

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
                
                sb.AppendLine("Rabbit Uri:" + factory.Uri);
                sb.AppendLine("Rabbit Host:" + factory.HostName);
                sb.AppendLine("Rabbit VirtualHost:" + factory.VirtualHost);
                sb.AppendLine("Rabbit UserName:" + factory.UserName);
                sb.AppendLine("Rabbit Port:" + factory.Port);

                Assert.Ignore($"Could not connect to RabbitMQ {Environment.NewLine + sb + Environment.NewLine} : {e.Message}");
            }
        }

    }
}