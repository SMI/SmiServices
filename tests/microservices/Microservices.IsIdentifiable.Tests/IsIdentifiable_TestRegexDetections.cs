using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Runners;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests
{
    public class IsIdentifiableRunnerTests
    {
        [Test]
        public void TestChiInString()
        {
            var runner = new TestRunner("hey there,0101010101 excited to see you");
            runner.Run();

            FailurePart p = runner.ResultsOfValidate.Single();

            Assert.AreEqual("0101010101", p.Word);
            Assert.AreEqual(10, p.Offset);
        }

        [TestCase("DD3 7LB")]
        [TestCase("dd3 7lb")]
        [TestCase("dd37lb")]
        public void IsIdentifiable_TestPostcodes(string code)
        {
            var runner = new TestRunner("Patient lives at " + code);
            runner.Run();

            var p = runner.ResultsOfValidate.Single();

            //this would be nice
            Assert.AreEqual(code, p.Word);
            Assert.AreEqual(17, p.Offset);
            Assert.AreEqual(FailureClassification.Postcode, p.Classification);
        }

        [TestCase("DD3 7LB")]
        [TestCase("dd3 7lb")]
        [TestCase("dd37lb")]
        public void IsIdentifiable_TestPostcodes_WhitelistDD3(string code)
        {

            var runner = new TestRunner("Patient lives at " + code);
            
            runner.LoadRules(
                @"
BasicRules:
  - Action: Ignore
    IfPattern: DD3");

            runner.Run();
            
            Assert.IsEmpty(runner.ResultsOfValidate);
        }

        [TestCase("DD3 7LB")]
        [TestCase("dd3 7lb")]
        [TestCase("dd37lb")]
        public void IsIdentifiable_TestPostcodes_IgnorePostcodesFlagSet(string code)
        {
            //since allow postcodes flag is set
            var runner = new TestRunner("Patient lives at " + code, new TestOpts() { IgnorePostcodes = true });
            runner.Run();

            //there won't be any failure results reported
            Assert.IsEmpty(runner.ResultsOfValidate);
        }


        //no longer detected, that's fine
        //[TestCase("Patient_lives_at_DD28DD", "DD28DD")]
        [TestCase("^DD28DD^", "DD28DD")]
        [TestCase("dd3^7lb", "dd3 7lb")]
        public void IsIdentifiable_TestPostcodes_EmbeddedInText(string find, string expectedMatch)
        {
            var runner = new TestRunner(find);
            runner.Run();

            var p = runner.ResultsOfValidate.Single();

            //this would be nice
            Assert.AreEqual(expectedMatch, p.Word);
            Assert.AreEqual(FailureClassification.Postcode, p.Classification);
        }

        [TestCase("dd3000")]
        [TestCase("dd3 000")]
        [TestCase("1444DD2011FD1118E63006097D2DF4834C9D2777977D811907000065B840D9CA50000000837000000FF0100A601000000003800A50900000700008001000000AC020000008000000D0000805363684772696400A8480000E6FBFFFF436174616C6F6775654974656D07000000003400A50900000700008002000000A402000000800000090000805363684772696400A84800001E2D0000436174616C6F67756500000000008000A50900000700008003000000520000000180000058000080436F6E74726F6C00A747000")]
        public void IsIdentifiable_TestNotAPostcode(string code)
        {

            var runner = new TestRunner("Patient lives at " + code);
            runner.Run();

            Assert.IsEmpty(runner.ResultsOfValidate);
        }


        [TestCase("Friday, 29 May 2015", "29 May", "May 2015", null)]
        [TestCase("Friday, 29 May 2015 05:50", "29 May", "May 2015", null)]
        [TestCase("Friday, 29 May 2015 05:50 AM", "29 May", "May 2015", null)]
        [TestCase("Friday, 29th May 2015 5:50", "29th May", "May 2015", null)]
        [TestCase("Friday, May 29th 2015 5:50 AM", "May 29th", null, null)]
        [TestCase("Friday, 29-May-2015 05:50:06", "29-May", "May-2015", null)]
        [TestCase("05/29/2015 05:50", "05/29/2015",null, null)]
        [TestCase("05-29-2015 05:50 AM", "05-29-2015", null, null)]
        [TestCase("2015-05-29 5:50", "2015-05-29", null, null)]
        [TestCase("05/29/2015 5:50 AM", "05/29/2015", null, null)]
        [TestCase("05/29/2015 05:50:06", "05/29/2015", null, null)]
        [TestCase("May-29", "May-29", null, null)]
        [TestCase("Jul-29th", "Jul-29th", null, null)]
        [TestCase("July-1st", "July-1st", null, null)]
        [TestCase("2015-05-16T05:50:06.7199222-04:00", "2015-05-16T", null, null)]
        [TestCase("2015-05-16T05:50:06", "2015-05-16T", null, null)]
        [TestCase("Fri, 16 May 2015 05:50:06 GMT", "16 May", "May 2015", null)]
        //[TestCase("05:50", "05:50", null, null)]
        //[TestCase("5:50 AM", "5:50 AM", null, null)]
        //[TestCase("05:50", "05:50", null, null)]
        //[TestCase("5:50 AM", "5:50 AM", null, null)]
        //[TestCase("05:50:06", "05:50:06", null, null)]
        [TestCase("2015 May", "2015 May", null, null)]
        //[TestCase("AB 13:10", "13:10", null, null)]
        public void IsIdentifiable_TestDates(string date, string expectedMatch1, string expectedMatch2, string expectedMatch3)
        {
            var runner = new TestRunner("Patient next appointment is " + date);
            runner.Run();
            
            Assert.AreEqual(expectedMatch1, runner.ResultsOfValidate[0].Word);
            Assert.AreEqual(FailureClassification.Date, runner.ResultsOfValidate[0].Classification);

            if (expectedMatch2 != null)
            {
                Assert.AreEqual(expectedMatch2, runner.ResultsOfValidate[1].Word);
                Assert.AreEqual(FailureClassification.Date, runner.ResultsOfValidate[1].Classification);
            }
            if (expectedMatch3 != null)
            {
                Assert.AreEqual(expectedMatch3, runner.ResultsOfValidate[2].Word);
                Assert.AreEqual(FailureClassification.Date, runner.ResultsOfValidate[2].Classification);
            }
        }

        [TestCase("We are going to the pub on Friday at about 3'o clock")]
        [TestCase("We may go there in August some time")]
        [TestCase("I will be 30 in September")]
        [TestCase("Prescribed volume is is 32.0 ml")]
        [TestCase("2001.1.2")]
        [TestCase("AB13:10")]
        public void IsIdentifiable_Test_NotADate(string input)
        {
            var runner = new TestRunner(input);
            runner.Run();

            Assert.IsEmpty(runner.ResultsOfValidate);
        }

        [Test]
        public void TestChiAndNameInString()
        {
            var runner = new TestRunner("David Smith should be referred to with chi 0101010101");

            runner.Run();
            Assert.AreEqual(1, runner.ResultsOfValidate.Count);

            FailurePart w1 = runner.ResultsOfValidate[0];
            
            /* Names are now picked up by the Socket NER Daemon
            //FailurePart w2 = runner.ResultsOfValidate[1];
            //FailurePart w3 = runner.ResultsOfValidate[2];

            
            Assert.AreEqual("David", w1.Word);
            Assert.AreEqual(0, w1.Offset);

            Assert.AreEqual("Smith", w2.Word);
            Assert.AreEqual(6, w2.Offset);
            */

            Assert.AreEqual("0101010101", w1.Word);
            Assert.AreEqual(43, w1.Offset);
        }

        [Test]
        public void TestSopDoesNotMatch()
        {
            const string sopKey = "SOPInstanceUID";
            const string exampleSop = "1.2.392.200036.9116.2.6.1.48.1214834115.1486205112.923825";
            var testOpts = new TestOpts
            {
                SkipColumns = sopKey
            };

            var runner = new TestRunner(exampleSop, testOpts, sopKey);

            runner.Run();
            Assert.AreEqual(0, runner.ResultsOfValidate.Count);
        }

        private class TestRunner : IsIdentifiableAbstractRunner
        {
            private readonly string _fieldToTest;
            private readonly string _valueToTest;

            public readonly List<FailurePart> ResultsOfValidate = new List<FailurePart>();

            public TestRunner(string valueToTest)
                : base(new TestOpts())
            {
                _valueToTest = valueToTest;
            }

            public TestRunner(string valueToTest, TestOpts opts, string fieldToTest = "field")
                : base(opts)
            {
                _fieldToTest = fieldToTest;
                _valueToTest = valueToTest;
            }

            public override int Run()
            {
                ResultsOfValidate.AddRange(Validate(_fieldToTest, _valueToTest).OrderBy(v => v.Offset));
                CloseReports();
                return 0;
            }
        }

        private class TestOpts : IsIdentifiableAbstractOptions
        {
            public TestOpts()
            {
                DestinationCsvFolder = TestContext.CurrentContext.TestDirectory;
                StoreReport = true;
            }
            public override string GetTargetName()
            {
                return "abc";
            }
        }
    }
}
