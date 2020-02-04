
using Microservices.CohortPackager.Execution;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.MessageSerialization;
using Smi.Common.Tests;
using System.Threading;

namespace Microservices.CohortPackager.Tests.Execution
{
    [TestFixture, RequiresMongoDb, RequiresRabbit]
    public class CohortPackagerHostTests
    {
        private readonly CohortPackagerTestHelper _helper = new CohortPackagerTestHelper();

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _helper.SetUpSuite();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp()
        {
            _helper.ResetSuite();
        }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void TestBasicOperation_Single()
        {
            using (
                var tester = new MicroserviceTester(
                    _helper.Options.RabbitOptions,
                    _helper.Options.CohortPackagerOptions.VerificationStatusOptions,
                    _helper.Options.CohortPackagerOptions.FileCollectionInfoOptions,
                    _helper.Options.CohortPackagerOptions.ExtractRequestInfoOptions))
            {
                var host = new CohortPackagerHost(_helper.Options, _helper.MockFileSystem, loadSmiLogConfig: false);
                tester.StopOnDispose.Add(host);

                tester.SendMessage(_helper.Options.CohortPackagerOptions.ExtractRequestInfoOptions, _helper.TestExtractRequestInfoMessage);
                tester.SendMessage(_helper.Options.CohortPackagerOptions.FileCollectionInfoOptions, _helper.TestFileCollectionInfoMessage);
                tester.SendMessage(_helper.Options.CohortPackagerOptions.VerificationStatusOptions, _helper.TestExtractFileStatusMessage);

                host.Start();
                Thread.Sleep(1000);
                host.JobWatcher.ProcessJobs();

                new TestTimelineAwaiter().Await(() => host.JobWatcher.JobsCompleted == 1);

                host.Stop("Test end");
                tester.Shutdown();
            }

        }

        [Test]
        public void TestBasicOperation_Multiple()
        {
            using (
                var tester = new MicroserviceTester(_helper.Options.RabbitOptions,
                    _helper.Options.CohortPackagerOptions.VerificationStatusOptions,
                    _helper.Options.CohortPackagerOptions.FileCollectionInfoOptions,
                    _helper.Options.CohortPackagerOptions.ExtractRequestInfoOptions))
            {
                var host = new CohortPackagerHost(_helper.Options, _helper.MockFileSystem, loadSmiLogConfig: false);
                tester.StopOnDispose.Add(host);

                _helper.TestFileCollectionInfoMessage.ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    SerializeableKeys = new[] { new MessageHeader(), new MessageHeader(), new MessageHeader() },
                    SerializeableValues = new[] { "AnonymisedTestFile1.dcm", "AnonymisedTestFile2.dcm", "AnonymisedTestFile3.dcm" }
                };

                tester.SendMessage(_helper.Options.CohortPackagerOptions.ExtractRequestInfoOptions, _helper.TestExtractRequestInfoMessage);
                tester.SendMessage(_helper.Options.CohortPackagerOptions.FileCollectionInfoOptions, _helper.TestFileCollectionInfoMessage);
                tester.SendMessage(_helper.Options.CohortPackagerOptions.VerificationStatusOptions, _helper.TestExtractFileStatusMessage);

                host.Start();
                Thread.Sleep(1000);
                host.JobWatcher.ProcessJobs();

                new TestTimelineAwaiter().Await(() => host.JobWatcher.JobsCompleted == 1);

                host.Stop("Test end");
                tester.Shutdown();
            }

        }

        #endregion
    }
}
