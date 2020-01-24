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
    {[Test]
        public void Test_ClassifierNoClassifier()
        {
            var options = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);
            
            options.IsIdentifiableOptions.ClassifierType = "";
            var ex = Assert.Throws<ArgumentException>(()=>new IsIdentifiableHost(options, false));
            StringAssert.Contains("No IClassifier has been set in options.  Enter a value for " + nameof(options.IsIdentifiableOptions.ClassifierType),ex.Message);
        }
        [Test]
        public void Test_ClassifierNotRecognized()
        {
            var options = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);
            
            options.IsIdentifiableOptions.ClassifierType = "HappyFunTimes";
            var ex = Assert.Throws<TypeLoadException>(()=>new IsIdentifiableHost(options, false));
            StringAssert.Contains("Could not load type 'HappyFunTimes' from",ex.Message);
        }
    }
}
