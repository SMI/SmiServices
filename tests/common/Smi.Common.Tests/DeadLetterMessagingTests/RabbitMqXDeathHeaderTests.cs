
using Smi.Common.Messages;
using NUnit.Framework;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Smi.Common.Tests.DeadLetterMessagingTests
{
    [TestFixture,RequiresRabbit]
    public class RabbitMqXDeathHeaderTests
    {
        private readonly DeadLetterTestHelper _testHelper = new DeadLetterTestHelper();

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testHelper.SetUpSuite();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _testHelper.Dispose();
        }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp()
        {
            _testHelper.ResetSuite();
        }

        #endregion

        #region Tests

        [Test]
        public void TestCaptureOfXDeathHeaders()
        {
            var testMessage = new AccessionDirectoryMessage
            {
                DirectoryPath = @"C:\temp"
            };

            _testHelper.TestProducer.SendMessage(testMessage, null, DeadLetterTestHelper.TestRoutingKey);

            new TestTimelineAwaiter().Await(() => _testHelper.MessageRejectorConsumer.NackCount == 1);

            BasicGetResult getResult = _testHelper.TestModel.BasicGet(DeadLetterTestHelper.TestDlQueueName, true);

            var xHeaders = new RabbitMqXDeathHeaders(getResult.BasicProperties.Headers, Encoding.UTF8);

            // Can't really get the expected time, just have to copy from the result
            long expectedTime = xHeaders.XDeaths[0].Time;

            var expectedHeaders = new RabbitMqXDeathHeaders
            {
                XDeaths = new List<RabbitMqXDeath>
                {
                   new RabbitMqXDeath
                   {
                       Count = 1,
                       Exchange = DeadLetterTestHelper.RejectExchangeName,
                       Queue = DeadLetterTestHelper.RejectQueueName,
                       Reason = "rejected",
                       RoutingKeys = new List<string>
                       {
                           DeadLetterTestHelper.TestRoutingKey
                       },
                       Time = expectedTime
                   }
                },
                XFirstDeathExchange = DeadLetterTestHelper.RejectExchangeName,
                XFirstDeathQueue = DeadLetterTestHelper.RejectQueueName,
                XFirstDeathReason = "rejected",
            };

            Assert.AreEqual(expectedHeaders, xHeaders);
        }

        [Test]
        public void TestCopyHeaders()
        {
            IEnumerable<string> xDeathHeaderNames = typeof(RabbitMqXDeathHeaders)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(fi => (fi.FieldType.IsAssignableFrom(typeof(string))))
                .Select(x => (string)x.GetValue(null));

            Dictionary<string, object> source = xDeathHeaderNames.ToDictionary<string, string, object>(item => item, item => "test");
            source.Add("other", "garbage data");
            var target = new Dictionary<string, object>();

            Assert.DoesNotThrow(() => RabbitMqXDeathHeaders.CopyHeaders(source, target));

            source.Remove("other");
            Assert.True(source.Count == target.Count && !source.Except(target).Any());
        }

        #endregion
    }
}
