
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using NUnit.Framework;
using Smi.Common.Messages.Extraction;
using System.Collections.Generic;

namespace Microservices.CohortExtractor.Tests
{
    [TestFixture]
    public class PathResolverTests
    {
        private const string FilePath = "2018/01/01/AAAA/testDicom.dcm";
        private const string SeriesId = "1.2.3.4";

        [Test]
        public void TestDefaultPathResolver()
        {
            var collection = new ExtractImageCollection(SeriesId, SeriesId, new HashSet<string>(new[] { FilePath }));

            Assert.AreEqual("testDicom-an.dcm", new DefaultProjectPathResolver().GetOutputPath(FilePath, collection));
        }

        [Test]
        public void TestSeriesPathResolvers()
        {
            var collection = new ExtractImageCollection(SeriesId, SeriesId, new HashSet<string>(new[] { FilePath }));

            Assert.AreEqual("1.2.3.4/testDicom-an.dcm", new SeriesKeyPathResolver().GetOutputPath(FilePath, collection));
        }
    }
}
