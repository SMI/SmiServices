using Microservices.IsIdentifiable.Service;
using Moq;
using NUnit.Framework;
using Smi.Common.Messaging;
using Smi.Common.Tests;
using System;

namespace Microservices.IsIdentifiable.Tests.Service
{
    public class IsIdentifiableQueueConsumerTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

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
        public void Constructor_NullProducerModel_ThrowsException()
        {
            var exc = Assert.Throws<ArgumentNullException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    null,
                    "foo",
                    new Mock<IClassifier>().Object
                );
            });
            Assert.AreEqual("Value cannot be null. (Parameter 'producer')", exc.Message);
        }

        [Test]
        public void Constructor_NullOrWhitespaceExtractionRoot_ThrowsException()
        {
            var exc = Assert.Throws<ArgumentException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    new Mock<IProducerModel>().Object,
                    null,
                    new Mock<IClassifier>().Object
                );
            });
            Assert.AreEqual("Argument cannot be null or whitespace (Parameter 'extractionRoot')", exc.Message);

            exc = Assert.Throws<ArgumentException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    new Mock<IProducerModel>().Object,
                    "   ",
                    new Mock<IClassifier>().Object
                );
            });
            Assert.AreEqual("Argument cannot be null or whitespace (Parameter 'extractionRoot')", exc.Message);
        }

        [Test]
        public void Constructor_NullClassifier_ThrowsException()
        {
            var exc = Assert.Throws<ArgumentNullException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    new Mock<IProducerModel>().Object,
                    "foo",
                    null
                );
            });
            Assert.AreEqual("Value cannot be null. (Parameter 'classifier')", exc.Message);
        }

        #endregion
    }
}
