using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Applications.ServiceTester.Tests
{
    internal class ProgramTests
    {
        private StringWriter _outStringWriter;

        [SetUp]
        public void SetUp()
        {
            SmiCliInit.InitSmiLogging = false;

            _outStringWriter = new StringWriter();
            Console.SetOut(_outStringWriter);
        }

        [Test]
        public void Program_NoArgs_ExitsWithError()
        {
            // Arrange

            var args = new List<string>() { };

            // Act

            var rc = Program.Main(args);

            // Assert

            Assert.AreEqual(1, rc);
        }

        [Test]
        public void Program_PrintMessageTemplate_InvalidMessageType()
        {
            // Arrange

            var args = new List<string>() { "-p", "IAmNotAMessage", };

            // Act

            var main = () => Program.Main(args);

            // Assert

            var exc = Assert.Throws<ArgumentException>(() => main());
            Assert.True(exc.Message.Contains("Could not find type 'IAmNotAMessage'"));
        }

        [Test]
        public void Program_PrintMessageTemplate_AccessionDirectoryMessage()
        {
            // Arrange

            var args = new List<string>() { "-p", "AccessionDirectoryMessage", };

            var expected = @"{
  ""class"": ""AccessionDirectoryMessage"",
  ""DirectoryPath"": """"
}";

            // Act

            var rc = Program.Main(args);

            // Assert

            Assert.AreEqual(0, rc);
            Assert.True(_outStringWriter.ToString().StartsWith(expected));
        }

        [Test]
        public void Program_PrintMessageTemplate_AllMessageTypes()
        {
            // Arrange

            var allMessageTypes =
                Assembly.GetAssembly(typeof(IMessage)).GetTypes()
                .Where(x =>
                    typeof(IMessage).IsAssignableFrom(x) &&
                    !(x.IsInterface || x.IsAbstract)
                );

            foreach (var msgType in allMessageTypes)
            {
                var args = new List<string>() { "-p", msgType.Name, };

                // Act

                var rc = Program.Main(args);

                // Assert

                Assert.AreEqual(0, rc);
                Assert.True(_outStringWriter.ToString().Contains(msgType.Name));
            }
        }
    }

    [RequiresRabbit]
    internal class ProgramTests_WithRabbit
    {
        [Test]
        public void Program_SendMessage_AccessionDirectoryMessage()
        {
            // Arrange

            var testMsgPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "t.json");
            File.WriteAllText(testMsgPath, @"{ ""class"": ""AccessionDirectoryMessage"", ""DirectoryPath"": ""testDir""}");

            var factory = RequiresRabbit.GetConnectionFactory();
            using var conn = factory.CreateConnection();
            using var channel = conn.CreateModel();
            channel.ExchangeDeclare("TestExchange", ExchangeType.Topic, durable: true);
            channel.QueueDeclare("TestQueue", durable: false);
            channel.QueueBind("TestQueue", "TestExchange", "");

            string receivedJson = null;
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, ea) =>
            {
                receivedJson = Encoding.UTF8.GetString(ea.Body.ToArray());
            };
            channel.BasicConsume("TestQueue", autoAck: true, consumer);

            var args = new List<string>()
            {
                "-f", testMsgPath,
                "-e", "TestExchange"
            };

            // Act

            var rc = Program.Main(args);

            // Assert

            Assert.AreEqual(0, rc);
            Assert.AreEqual(@"{""DirectoryPath"":""testDir""}", receivedJson);
        }
    }
}
