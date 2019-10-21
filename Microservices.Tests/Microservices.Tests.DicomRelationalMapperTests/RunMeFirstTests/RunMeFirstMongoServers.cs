using System;
using Microservices.Common;
using Microservices.Common.Options;
using NUnit.Framework;
using Tests.Common.Smi;

namespace Microservices.Tests.RDMPTests.RunMeFirstTests
{
    [Category("RunMeFirst")]
    public class RunMeFirstMongoServers
    {
        [Test, RequiresMongoDb]
        public void TestMongoAvailable()
        {
            Assert.Pass();
        }

        
        [Test,RequiresRabbit]
        public void RabbitAvailable()
        {
            var options = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);
            var rabbitOptions = options.RabbitOptions;

            Console.WriteLine("Checking the following configuration:" + Environment.NewLine + rabbitOptions);
            try
            {
                var adapter = new RabbitMqAdapter(rabbitOptions, "TestHost");
            }
            catch (Exception)
            {
                Assert.Fail("Could not access Rabbit MQ Server");
            }
            
            Assert.Pass();
        }
    }
}
