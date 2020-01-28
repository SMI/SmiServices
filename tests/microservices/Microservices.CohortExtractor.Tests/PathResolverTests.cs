
using System;
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
        public void TestSeriesPathResolvers()
        {
            Assert.AreEqual(
                "1.2.3.4/testDicom-an.dcm".Replace('/', Path.DirectorySeparatorChar),
                new DefaultProjectPathResolver().GetOutputPath(new QueryToExecuteResult(FilePath, null, SeriesId, null,
                    false, null),null));
        }

        [TestCase(typeof(DefaultProjectPathResolver),"mypic",true,true,true,"1.2.3/4.5.6/mypic-an.dcm")]
        [TestCase(typeof(DefaultProjectPathResolver),"mypic.dcm",true,true,true,"1.2.3/4.5.6/mypic-an.dcm")]
        [TestCase(typeof(DefaultProjectPathResolver),"mypic",false,true,true,"4.5.6/mypic-an.dcm")]
        [TestCase(typeof(DefaultProjectPathResolver),"mypic",true,false,true,"1.2.3/mypic-an.dcm")]
        public void TestPathResolver(Type resolverType,string inputFile,bool hasStudy,bool hasSeries,bool hasSop, string expectedSubdirectory)
        {
            var instance = (IProjectPathResolver)Activator.CreateInstance(resolverType);
            
            Assert.AreEqual(expectedSubdirectory,
                    instance.GetOutputPath(new QueryToExecuteResult("/omg/whoknows/" + inputFile,
                hasStudy ? "1.2.3" : null,
                hasSeries ? "4.5.6" : null,
                hasSop ? "7.8.9" : null,
                false,
                null),null)
                        //don't trip up on linux vs windows slash directions
                        .Replace('\\','/'));
        }
    }
}
