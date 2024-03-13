using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Microservices.CohortExtractor.Tests
{
    public class ColumnValuesRejectorTests
    {
        private const string PatColName = "PatientID";

        [Test]
        public void Test_ColumnValuesRejector_MissingColumn_Throws()
        {
            var rejector = new ColumnValuesRejector("fff",new HashSet<string>{ "dave","frank"});

            var moqDave = new Mock<DbDataReader>();
            moqDave
                .Setup(x => x["fff"])
                .Throws<IndexOutOfRangeException>();

            var exc = Assert.Throws<IndexOutOfRangeException>(() => rejector.Reject(moqDave.Object, out var _));
            Assert.That(exc!.Message, Does.Contain($"Expected a column called fff"));
        }

        [Test]
        public void Test_ColumnValuesRejectorTests()
        {
            var rejector = new ColumnValuesRejector(PatColName,new HashSet<string>(new []{ "Frank","Peter","David"},StringComparer.CurrentCultureIgnoreCase));

            var moqDave = new Mock<DbDataReader>();
            moqDave.Setup(x => x[PatColName])
                .Returns("Dave");

            Assert.That(rejector.Reject(moqDave.Object, out var reason), Is.False);
            Assert.That(reason, Is.Null);

            var moqFrank = new Mock<DbDataReader>();
            moqFrank.Setup(x => x[PatColName])
                .Returns("Frank");

            Assert.Multiple(() =>
            {
                Assert.That(rejector.Reject(moqFrank.Object, out reason), Is.True);
                Assert.That(reason, Is.EqualTo("Patient or Identifier was in reject list"));
            });

            var moqLowerCaseFrank = new Mock<DbDataReader>();
            moqLowerCaseFrank.Setup(x => x[PatColName])
                .Returns("frank");

            Assert.Multiple(() =>
            {
                Assert.That(rejector.Reject(moqLowerCaseFrank.Object, out reason), Is.True);
                Assert.That(reason, Is.EqualTo("Patient or Identifier was in reject list"));
            });
        }
    }
}
