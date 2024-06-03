using System;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Tests.Common;

namespace Microservices.CohortExtractor.Tests
{
    public class QueryToExecuteTests : UnitTests
    {
        [Test]
        public void Test_QueryToExecute_BasicSQL()
        {
            var cata = WhenIHaveA<Catalogue>();

            var ex = Assert.Throws<ArgumentNullException>(()=>new QueryToExecuteColumnSet(cata, null,null,null,null));
            Assert.That(ex!.Message,Does.Match(@"Parameter.+filePathColumn"));
        }
    }
}
