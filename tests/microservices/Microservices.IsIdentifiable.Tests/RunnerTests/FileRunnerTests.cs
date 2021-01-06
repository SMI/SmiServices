using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Runners;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microservices.IsIdentifiable.Tests.RunnerTests
{
    class FileRunnerTests
    {
        [Test]
        public void FileRunner_CsvWithCHI()
        {
            var fi = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory,"testfile.csv"));
            using(var s = fi.CreateText())
            {
                s.WriteLine("Fish,Chi,Bob");
                s.WriteLine("123,0102821172,32 Ankleberry lane");
                s.Flush();
                s.Close();
            }
            
            var runner = new FileRunner(new Options.IsIdentifiableFileOptions(){File = fi, StoreReport = true});
            
            var reporter = new Mock<IFailureReport>(MockBehavior.Strict);
            
            reporter.Setup(f=>f.Add(It.IsAny<Failure>())).Callback<Failure>(f=>Assert.AreEqual("0102821172",f.ProblemValue));
            reporter.Setup(f=>f.DoneRows(1));
            reporter.Setup(f=>f.CloseReport());


            runner.Reports.Add(reporter.Object);

            runner.Run();

            reporter.Verify();
        }
    }
}
