using NUnit.Framework;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;

namespace SmiServices.IntegrationTests.Microservices.DicomRelationalMapper.RunMeFirstTests
{
    [Category("RunMeFirst")]
    public class RunMeFirstMongoServers
    {
        [Test, RequiresMongoDb]
        public void TestMongoAvailable()
        {
            Assert.Pass();
        }


        [Test, RequiresRabbit]
        public void RabbitAvailable()
        {
            var options = new GlobalOptionsFactory().Load(nameof(RabbitAvailable));
            var rabbitOptions = options.RabbitOptions!;

            Assert.DoesNotThrow(() => _ = new RabbitMQBroker(rabbitOptions, nameof(RabbitAvailable)), $"Rabbit failed with the following configuration:{Environment.NewLine}{rabbitOptions}");
        }
    }
}
