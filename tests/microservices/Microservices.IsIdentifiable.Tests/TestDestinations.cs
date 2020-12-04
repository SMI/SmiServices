using System;
using System.Data;
using System.IO;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Reporting.Destinations;
using Microservices.IsIdentifiable.Reporting.Reports;
using NUnit.Framework;
using Smi.Common.Tests;

namespace Microservices.IsIdentifiable.Tests
{
    internal class TestDestinations
    {
        [Test]
        public void TestCsvDestination_Normal()
        {
            var outDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            var opts = new IsIdentifiableRelationalDatabaseOptions { DestinationCsvFolder = outDir.FullName };
            var dest = new CsvDestination(opts, "test",false);

            var report = new TestFailureReport(dest);
            report.WriteToDestinations();
            report.CloseReport();

            string fileCreatedContents = File.ReadAllText(Path.Combine(outDir.FullName, "test.csv"));
            fileCreatedContents = fileCreatedContents.Replace("\r\n", Environment.NewLine);

            TestHelpers.AreEqualIgnoringLineEndings(@"col1,col2
""cell1 with some new 
 lines and 	 tabs"",cell2
", fileCreatedContents);
        }

        [Test]
        public void TestCsvDestination_NormalButNoWhitespace()
        {
            var outDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            var opts = new IsIdentifiableRelationalDatabaseOptions { DestinationNoWhitespace = true, DestinationCsvFolder = outDir.FullName };
            var dest = new CsvDestination(opts, "test",false);
            
            var report = new TestFailureReport(dest);
            report.WriteToDestinations();
            report.CloseReport();

            var fileCreatedContents = File.ReadAllText(Path.Combine(outDir.FullName, "test.csv"));
            fileCreatedContents = fileCreatedContents.Replace("\r\n", Environment.NewLine);

            TestHelpers.AreEqualIgnoringLineEndings(@"col1,col2
cell1 with some new lines and tabs,cell2
", fileCreatedContents);
        }

        [Test]
        public void TestCsvDestination_Tabs()
        {
            var outDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            var opts = new IsIdentifiableRelationalDatabaseOptions
            {
                // This is slash t, not an tab
                DestinationCsvSeparator = "\\t",
                DestinationNoWhitespace = true,
                DestinationCsvFolder = outDir.FullName
            };

            var dest = new CsvDestination(opts, "test",false);
            
            var report = new TestFailureReport(dest);
            report.WriteToDestinations();
            report.CloseReport();

            string fileCreatedContents = File.ReadAllText(Path.Combine(outDir.FullName, "test.csv"));
            fileCreatedContents = fileCreatedContents.Replace("\r\n", Environment.NewLine);

            TestHelpers.AreEqualIgnoringLineEndings(@"col1	col2
cell1 with some new lines and tabs	cell2
", fileCreatedContents);
        }
    }

    internal class TestFailureReport : IFailureReport
    {
        private readonly IReportDestination _dest;

        private readonly DataTable _dt = new DataTable();

        public TestFailureReport(IReportDestination dest)
        {
            _dest = dest;

            _dt.Columns.Add("col1");
            _dt.Columns.Add("col2");
            _dt.Rows.Add("cell1 with some new \r\n lines and \t tabs", "cell2");
        }


        public void AddDestinations(IsIdentifiableAbstractOptions options) { }

        public void DoneRows(int numberDone) { }

        public void Add(Failure failure) { }

        public void CloseReport()
        {
            _dest.Dispose();
        }

        public void WriteToDestinations()
        {
            _dest.WriteItems(_dt);
        }
    }
}
