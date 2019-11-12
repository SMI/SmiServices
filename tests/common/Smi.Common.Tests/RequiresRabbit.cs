
using System;
using System.IO;
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
                TestContext.WriteLine("Rabbit Uri:" + factory.Uri);
                TestContext.WriteLine("Rabbit Host:" + factory.HostName);
                TestContext.WriteLine("Rabbit VirtualHost:" + factory.VirtualHost);
                TestContext.WriteLine("Rabbit UserName:" + factory.UserName);
                TestContext.WriteLine("Rabbit Port:" + factory.Port);

                Assert.Ignore($"Could not connect to RabbitMQ: {e.Message}");
            }
        }

    }
}