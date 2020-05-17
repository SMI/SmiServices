﻿
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using System.IO;

namespace Microservices.CohortExtractor.Tests
{
    [TestFixture]
    public class DefaultProjectPathResolverTest
    {
        [TestCase("study", "series")]
        [TestCase("study", null)]
        [TestCase(null, "series")]
        [TestCase(null, null)]
        public void TestDefaultProjectPathResolver_IdParts(string study, string series)
        {
            var result = new QueryToExecuteResult(
                "foo.dcm",
                study,
                series,
                "sop",
                false,
                null);

            Assert.AreEqual(
                Path.Combine(
                    study ?? "unknown",
                    series ?? "unknown",
                    "foo-an.dcm"),
                new DefaultProjectPathResolver().GetOutputPath(result, null));
        }

        [TestCase("file-an.dcm", "file.dcm")]
        [TestCase("file-an.dcm", "file.dicom")]
        [TestCase("file-an.dcm", "file")]
        [TestCase("file.foo-an.dcm", "file.foo")]
        public void TestDefaultProjectPathResolver_Extensions(string expectedOutput, string inputFile)
        {
            var result = new QueryToExecuteResult(
                Path.Combine("foo", inputFile),
                "study",
                "series",
                "sop",
                false,
                null);

            Assert.AreEqual(
                Path.Combine(
                    "study",
                    "series",
                    expectedOutput),
                new DefaultProjectPathResolver().GetOutputPath(result, null));
        }

        [Test]
        public void TestDefaultProjectPathResolver_Both()
        {
            var result = new QueryToExecuteResult(
                Path.Combine("foo", "file"),
                "study",
                null,
                "sop",
                false,
                null);

            Assert.AreEqual(
                Path.Combine(
                    "study",
                    "unknown",
                    "file-an.dcm"),
                new DefaultProjectPathResolver().GetOutputPath(result, null));
        }
    }
}
