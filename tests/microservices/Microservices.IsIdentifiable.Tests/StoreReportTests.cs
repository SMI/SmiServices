using System;
using System.Collections.Generic;
using System.Text;
using Microservices.IsIdentifiable.Failure;
using Microservices.IsIdentifiable.Reporting.Destinations;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests
{
    class StoreReportTests
    {

        [Test]
        public void Test_Includes()
        {
            string origin = "this word fff is the problem";
            
            var part = new FailurePart("fff", FailureClassification.Organization, origin.IndexOf("fff"));
            
            Assert.IsFalse(part.Includes(0));
            Assert.IsFalse(part.Includes(9));
            Assert.IsTrue(part.Includes(10));
            Assert.IsTrue(part.Includes(11));
            Assert.IsTrue(part.Includes(12));
            Assert.IsFalse(part.Includes(13));
        }

        [Test]
        public void Test_IncludesSingleChar()
        {
            string origin = "this word f is the problem";
            
            var part = new FailurePart("f", FailureClassification.Organization, origin.IndexOf("f"));
            
            Assert.IsFalse(part.Includes(0));
            Assert.IsFalse(part.Includes(9));
            Assert.IsTrue(part.Includes(10));
            Assert.IsFalse(part.Includes(11));
            Assert.IsFalse(part.Includes(12));
            Assert.IsFalse(part.Includes(13));
        }
    }
}
