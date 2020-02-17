
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

	    accessionList.AppendLine("/PACS/2018/01/01/AAA,");
	    accessionList.AppendLine("/PACS/2018/01/01/AAA/,");
	    accessionList.AppendLine("/PACS/2018/01/01/,");
	    accessionList.AppendLine("/PACS/2018/01/01/test.dcm,");
	    accessionList.AppendLine("                 ");
	    accessionList.AppendLine("NULL");
	    accessionList.AppendLine(",,,,");

	    return accessionList.ToString(); 
	}

	[Test]
	public void TestAccessionDirectoryLister()
        {
            // Mock file system referenced in accession list
            string rootDir = Path.GetFullPath("/PACS");
            MockFileSystem mockFilesystem = new MockFileSystem();
	    
	    // Mock input file 
	    string testFile = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/AAA/testDicom.dcm"));
	    var mockInputFile = new MockFileData(GetListContent());
	    mockFilesystem.AddFile(testFile, mockInputFile);

	    // Mock producer
	    var totalSent = 0;

            Mock<IProducerModel> mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(),
                                            null,
                                            ""))
                .Callback(() => ++totalSent);

            AccessionDirectoryLister accessionLister = new AccessionDirectoryLister(rootDir, mockFilesystem, "*.dcm", mockProducerModel.Object);

            accessionLister.SearchForDicomDirectories(testFile);
        }
    }
}
