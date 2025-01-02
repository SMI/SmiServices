using Moq;
using NUnit.Framework;
using SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;


namespace SmiServices.UnitTests.Applications.DicomDirectoryProcessor;

/// <summary>
/// Unit tests for BasicDicomDirectoryFinder
/// </summary>
[TestFixture]
public class DicomDirectoryFinderTest
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [Test]
    public void FindingAccessionDirectory()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.GetFullPath("/PACS/2019/01/01/foo/01/a.dcm"), new MockFileData([0x12, 0x34, 0x56, 0xd2] ) },
            { Path.GetFullPath("/PACS/2019/01/02/foo/02/a.dcm"), new MockFileData([0x12, 0x34, 0x56, 0xd2] ) },
        });

        var m1 = new AccessionDirectoryMessage
        {
            //NOTE: These can't be rooted, so can't easily use Path.GetFullPath
            DirectoryPath = "2019/01/01/foo/01".Replace('/', Path.DirectorySeparatorChar)
        };

        var m2 = new AccessionDirectoryMessage
        {
            DirectoryPath = "2019/01/02/foo/02".Replace('/', Path.DirectorySeparatorChar)
        };

        string rootDir = Path.GetFullPath("/PACS");
        var mockProducerModel = new Mock<IProducerModel>();
        var ddf = new BasicDicomDirectoryFinder(rootDir, fileSystem, "*.dcm", mockProducerModel.Object);
        ddf.SearchForDicomDirectories(rootDir);

        mockProducerModel.Verify(pm => pm.SendMessage(m1, null, It.IsAny<string>()));
        mockProducerModel.Verify(pm => pm.SendMessage(m2, null, It.IsAny<string>()));
    }
}
