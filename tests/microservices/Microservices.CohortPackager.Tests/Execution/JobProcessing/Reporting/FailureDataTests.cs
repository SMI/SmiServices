using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;

namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting
{
    public class FailureDataTests
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
        public void Constructor_NullOrEmptyFailureParts_ThrowsException()
        {
            // Arrange

            IEnumerable<FailurePart> parts = null;

            // Act
            // Assert

            Assert.Throws<ArgumentNullException>(() =>
            {
                new FailureData(parts, "foo", "bar");
            });

            // Arrange

            parts = new List<FailurePart>();

            // Act
            // Assert

            Assert.Throws<ArgumentException>(() =>
            {
                new FailureData(parts, "foo", "bar");
            });
        }

        [Test]
        public void Constructor_NullOrWhitespaceProblemField_ThrowsException()
        {
            // Arrange

            string problemField = null;

            // Act
            // Assert

            Assert.Throws<ArgumentException>(() =>
            {
                new FailureData(new List<FailurePart>(), problemField, "bar");
            });

            // Arrange

            problemField = "   ";

            // Act
            // Assert

            Assert.Throws<ArgumentException>(() =>
            {
                new FailureData(new List<FailurePart>(), problemField, "bar");
            });
        }

        [Test]
        public void Constructor_NullOrWhitespaceProblemValue_ThrowsException()
        {
            // Arrange

            string problemValue = null;

            // Act
            // Assert

            Assert.Throws<ArgumentException>(() =>
            {
                new FailureData(new List<FailurePart>(), "foo", problemValue);
            });

            // Arrange

            problemValue = "   ";

            // Act
            // Assert

            Assert.Throws<ArgumentException>(() =>
            {
                new FailureData(new List<FailurePart>(), "foo", problemValue);
            });
        }

        [Test]
        public void FromFailure_HappyPath()
        {
            // Arrange

            var parts = new List<FailurePart>()
            {
                new FailurePart("foo", FailureClassification.Person, 0),
            };
            var f = new Failure(parts)
            {
                Resource = "foo.dcm",
                ResourcePrimaryKey = "1.2.3.4",
                ProblemField = "TextValue",
                ProblemValue = "foo bar",
            };

            // Act

            var failureData = FailureData.FromFailure(f);

            // Assert

            Assert.AreEqual(1, failureData.Parts.Count);
            Assert.AreEqual(new FailurePart("foo", FailureClassification.Person, 0), failureData.Parts[0]);
            Assert.AreEqual("TextValue", failureData.ProblemField);
            Assert.AreEqual("foo bar", failureData.ProblemValue);
        }

        #endregion
    }
}
