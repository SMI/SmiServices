
using Applications.DicomDirectoryProcessor.Execution.DirectoryFinders;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Tests;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO;
using System.IO.Abstractions.TestingHelpers;


namespace Applications.DicomDirectoryProcessor.Tests
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
            TestLogger.Setup();
        }
        
        // TODO(rkm 2020-02-12) Things to test
        // - Valid CSV file
        // - CSVs with various invalid data / lines
	private String GetListContent()
	{
            StringBuilder accessionList = new StringBuilder();

	    accessionList.AppendLine("/PACS/2018/01/01/AAA,");           // exists and has dicom files - pass 
	    accessionList.AppendLine("/PACS/2018/01/01/AAA/,");          // exists and has dicom files - pass
	    accessionList.AppendLine("/PACS/2018/01/01/BBB,");           // does exist but has no dicom files - fail
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
            string rootDir = Path.GetFullPath("/PACS");
            MockFileSystem mockFilesystem = new MockFileSystem();

	    string testDicom = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/AAA/test.dcm"));
	    mockFilesystem.AddFile(testDicom, MockFileData.NullObject);

	    string testBad = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/BBB/test.txt"));
	    mockFilesystem.AddFile(testBad, MockFileData.NullObject);
	    
	    // Mock input file 
	    string accessionList = Path.GetFullPath(Path.Combine(rootDir, "accessions.csv"));
	    var mockInputFile = new MockFileData(GetListContent());
	    mockFilesystem.AddFile(accessionList, mockInputFile);

	    // Mock producer
	    var totalSent = 0;

            Mock<IProducerModel> mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(),
                                            null,
                                            ""))
                .Callback(() => ++totalSent);

            AccessionDirectoryLister accessionLister = new AccessionDirectoryLister(rootDir, mockFilesystem, "*.dcm", mockProducerModel.Object);

            accessionLister.SearchForDicomDirectories(accessionList);
	    
	    Assert.AreEqual(totalSent, 2);
        }
    }
}
