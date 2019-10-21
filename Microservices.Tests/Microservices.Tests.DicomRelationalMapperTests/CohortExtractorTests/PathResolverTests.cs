
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.Common.Messages.Extraction;
using NUnit.Framework;
using System.Collections.Generic;

namespace Microservices.Tests.RDMPTests.CohortExtractorTests
{
    [TestFixture]
    public class PathResolverTests
    {
        private const string FilePath = @"2018\01\01\ABCD1234\testDicom.dcm";
        private const string SeriesId = "1.2.3.4";

        private readonly ExtractionRequestMessage _requestMessage = new ExtractionRequestMessage
        {
            ExtractionDirectory = @"1718-0316e\testExtract"
        };


        [Test]
        public void TestDefaultPathResolver()
        {
            var collection = new ExtractImageCollection(SeriesId, SeriesId, new HashSet<string>(new[] { FilePath }));

            Assert.AreEqual(@"testDicom-an.dcm", new DefaultProjectPathResolver().GetOutputPath(FilePath, collection));
        }

        [Test]
        public void TestSeriesPathResolvers()
        {
            var collection = new ExtractImageCollection(SeriesId, SeriesId, new HashSet<string>(new[] { FilePath }));

            Assert.AreEqual(@"1.2.3.4\testDicom-an.dcm", new SeriesKeyPathResolver().GetOutputPath(FilePath, collection));
        }
    }
}
