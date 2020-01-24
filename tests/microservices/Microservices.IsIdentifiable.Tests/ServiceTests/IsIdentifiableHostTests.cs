using System;
using System.Collections.Generic;
using System.Text;
using Microservices.IsIdentifiable.Service;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;

namespace Microservices.IsIdentifiable.Tests.ServiceTests
{
    [TestFixture, RequiresRabbit]
    class IsIdentifiableHostTests
    {
        [Test]
        public void TestCreatingOne()
        {
            var options = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);
            
            var host = new IsIdentifiableHost(options, false);

            Assert.IsNotNull(host);
        }
    }
}
