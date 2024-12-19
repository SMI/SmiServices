using NUnit.Framework;
using SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders;
using SmiServices.Common.Messages;
using SmiServices.UnitTests.Common.Messaging;
using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Text;


namespace SmiServices.UnitTests.Applications.DicomDirectoryProcessor
{
    /// <summary>
    /// Unit tests for AccessionDirectoryLister
    /// </summary>
    [TestFixture]
    public class AccessionDirectoryListerTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        private static string GetListContent()
        {
            StringBuilder accessionList = new();

            accessionList.AppendLine("/PACS/2018/01/01/AAA,");           // exists and has dicom files - fail (requires indication that is dir) 
            accessionList.AppendLine("/PACS/2018/01/01/AAA/,");          // exists and has dicom files - pass
            accessionList.AppendLine("/PACS/2018/01/01/E-123/,");        // exists and has dicom files - pass
            accessionList.AppendLine("/PACS/2018/01/01/01.01.2018/,");   // exists and has dicom files - pass
            accessionList.AppendLine("/PACS/2018/01/01/BBB/,");          // does exist but has no dicom files - fail
            accessionList.AppendLine("/PACS/2018/01/01/CCC/,");          // does not exist - fail
            accessionList.AppendLine("/PACS/2018/01/01/,");              // not pointing to accession directory - fail
            accessionList.AppendLine("/PACS/2018/01/01/testDicom.dcm,"); // not pointing to accession directory - fail
            accessionList.AppendLine("                 ");               // not pointing to accession directory - fail
            accessionList.AppendLine("NULL");                            // not pointing to accession directory - fail
            accessionList.AppendLine(",,,,");                            // not pointing to accession directory - fail

            return accessionList.ToString();
        }

        [Test]
        public void TestAccessionDirectoryLister()
        {
            // Mock file system referenced in accession list
            MockFileSystem mockFilesystem = new(null, Environment.CurrentDirectory);
            var rootDir = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, "PACS");

            var testDicom = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/AAA/test.dcm"));
            mockFilesystem.AddFile(testDicom, null);

            var specialCase1 = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/E-123/test.dcm"));
            mockFilesystem.AddFile(specialCase1, null);

            var specialCase2 = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/01.01.2018/test.dcm"));
            mockFilesystem.AddFile(specialCase2, null);

            var testBad = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/BBB/test.txt"));
            mockFilesystem.AddFile(testBad, null);

            // Mock input file 
            var accessionList = Path.GetFullPath(Path.Combine(rootDir, "accessions.csv"));
            var mockInputFile = new MockFileData(GetListContent());
            mockFilesystem.AddFile(accessionList, mockInputFile);

            // Mock producer
            var mockProducerModel = new TestProducer<AccessionDirectoryMessage>();
            AccessionDirectoryLister accessionLister = new(rootDir, mockFilesystem, "*.dcm", mockProducerModel);

            accessionLister.SearchForDicomDirectories(accessionList);

            Assert.That(mockProducerModel.TotalSent, Is.EqualTo(3));
        }
    }
}
