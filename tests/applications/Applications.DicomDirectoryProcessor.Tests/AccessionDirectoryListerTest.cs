
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
        
	private String GetListContent()
	{
            StringBuilder accessionList = new StringBuilder();

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
            MockFileSystem mockFilesystem = new MockFileSystem(null,Environment.CurrentDirectory);
			string rootDir = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory),"PACS");

	    string testDicom = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/AAA/test.dcm"));
	    mockFilesystem.AddFile(testDicom, MockFileData.NullObject);
	    
	    string specialCase1 = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/E-123/test.dcm"));
	    mockFilesystem.AddFile(specialCase1, MockFileData.NullObject);

	    string specialCase2 = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/01.01.2018/test.dcm"));
	    mockFilesystem.AddFile(specialCase2, MockFileData.NullObject);
	    
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
	    
	    Assert.AreEqual(3, totalSent);
        }
    }
}
