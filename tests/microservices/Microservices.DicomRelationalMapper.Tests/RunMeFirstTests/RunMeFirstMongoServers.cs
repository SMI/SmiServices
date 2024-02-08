using NUnit.Framework;
using Smi.Common;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;

namespace Microservices.DicomRelationalMapper.Tests.RunMeFirstTests
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

            Assert.DoesNotThrow(()=> _=new RabbitMQBroker(rabbitOptions,nameof(RabbitAvailable)), $"Rabbit failed with the following configuration:{Environment.NewLine}{rabbitOptions}");
        }
    }
}
