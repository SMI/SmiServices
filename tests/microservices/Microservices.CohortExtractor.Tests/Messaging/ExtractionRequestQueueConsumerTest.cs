using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.CohortExtractor.Messaging;
using Moq;
using NUnit.Framework;
using Smi.Common.Messaging;
using Smi.Common.Options;


namespace Microservices.CohortExtractor.Tests.Messaging
{
    public class ExtractionRequestQueueConsumerTest
    {
        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp() { }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void Test_ExtractionRequestQueueConsumer_AnonExtraction_RoutingKey()
        {
            var options = new CohortExtractorOptions();

            var mockFulfiller = new Mock<IExtractionRequestFulfiller>(MockBehavior.Strict);
            var mockAuditor = new Mock<IAuditExtractions>(MockBehavior.Strict);
            var mockPathResolver = new Mock<IProjectPathResolver>(MockBehavior.Strict);

            var mockFileMessageProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            var mockFileInfoMessageProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);

            var consumer = new ExtractionRequestQueueConsumer(
                options,
                mockFulfiller.Object,
                mockAuditor.Object, mockPathResolver.Object,
                mockFileMessageProducerModel.Object,
                mockFileInfoMessageProducerModel.Object);
            
            // TODO
            //consumer.ProcessMessage();
            
            Assert.Inconclusive();
        }

        [Test]
        public void Test_ExtractionRequestQueueConsumer_IdentExtraction_RoutingKey()
        {
            Assert.Inconclusive();
        }

        #endregion
    }
}
