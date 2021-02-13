﻿using NUnit.Framework;
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
            var options = new GlobalOptionsFactory().Load();
            var rabbitOptions = options.RabbitOptions;

            Console.WriteLine("Checking the following configuration:" + Environment.NewLine + rabbitOptions);
            try
            {
                var adapter = new RabbitMqAdapter(rabbitOptions.CreateConnectionFactory(), "TestHost");
            }
            catch (Exception)
            {
                Assert.Fail("Could not access Rabbit MQ Server");
            }

            Assert.Pass();
        }
    }
}
