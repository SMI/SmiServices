using System;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class QueryToExecuteTests : Tests.Common.UnitTests
    {
        [Test]
        public void Test_QueryToExecute_BasicSQL()
        {
            var cata = WhenIHaveA<Catalogue>();

            var ex = Assert.Throws<ArgumentNullException>(() => new QueryToExecuteColumnSet(cata, null, null, null, null));
            Assert.That(ex!.Message, Does.Match(@"Parameter.+filePathColumn"));
        }
    }
}
