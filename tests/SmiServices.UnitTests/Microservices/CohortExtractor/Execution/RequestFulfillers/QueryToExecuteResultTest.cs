using NUnit.Framework;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System;


namespace SmiServices.UnitTests.Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class QueryToExecuteResultTest
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
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

        /// <summary>
        /// Asserts that we always have a rejection reason when rejection=true
        /// </summary>
        [Test]
        public void Test_QueryToExecuteResult_RejectReasonNullOrEmpty_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var _ = new QueryToExecuteResult("foo", "bar", "baz", "whee", rejection: true, rejectionReason: null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                var _ = new QueryToExecuteResult("foo", "bar", "baz", "whee", rejection: true, rejectionReason: "  ");
            });
        }

        #endregion
    }
}
