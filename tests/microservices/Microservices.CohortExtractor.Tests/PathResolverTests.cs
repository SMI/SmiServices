
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Microservices.CohortExtractor.Execution.RequestFulfillers;


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
            var collection = new ExtractImageCollection(SeriesId);
                collection.Add(SeriesId,
                    new HashSet<QueryToExecuteResult>(new[]
                        {new QueryToExecuteResult(FilePath, null, SeriesId, null, false, null)}));

            Assert.AreEqual("testDicom-an.dcm", new DefaultProjectPathResolver().GetOutputPath(FilePath, collection));
        }

        [Test]
        public void TestSeriesPathResolvers()
        {
            var collection = new ExtractImageCollection(SeriesId);
            collection.Add(SeriesId,
                new HashSet<QueryToExecuteResult>(new[]
                    {new QueryToExecuteResult(FilePath, null, SeriesId, null, false, null)}));

            Assert.AreEqual(
                "1.2.3.4/testDicom-an.dcm".Replace('/', Path.DirectorySeparatorChar),
                new SeriesKeyPathResolver().GetOutputPath(FilePath, collection));
        }
    }
}
