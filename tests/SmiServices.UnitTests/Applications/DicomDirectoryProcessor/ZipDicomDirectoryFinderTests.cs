
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Tests;
using SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;


namespace Applications.DicomDirectoryProcessor.Tests
{
    /// <summary>
    /// Unit tests for ZipDicomDirectoryFinder
    /// </summary>
    [TestFixture]
    public class ZipDicomDirectoryFinderTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [Test]
        public void FindRandomDicomsOrZipsDirectory()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.GetFullPath("/PACS/FFF/DDD/a.dcm"), new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 } ) },
                { Path.GetFullPath("/PACS/FFF/b.dcm"), new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 } ) },
                { Path.GetFullPath("/PACS/CCC/c.zip"), new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 } ) },
            });

            var m1 = new AccessionDirectoryMessage
            {
                //NOTE: These can't be rooted, so can't easily use Path.GetFullPath
                DirectoryPath = "CCC".Replace('/', Path.DirectorySeparatorChar)
            };

            var m2 = new AccessionDirectoryMessage
            {
                DirectoryPath = "FFF".Replace('/', Path.DirectorySeparatorChar)
            };


            var m3 = new AccessionDirectoryMessage
            {
                DirectoryPath = "FFF/DDD".Replace('/', Path.DirectorySeparatorChar)
            };

            string rootDir = Path.GetFullPath("/PACS");
            var mockProducerModel = new Mock<IProducerModel>();
            var ddf = new ZipDicomDirectoryFinder(rootDir, fileSystem, "*.dcm", mockProducerModel.Object);
            ddf.SearchForDicomDirectories(rootDir);

            mockProducerModel.Verify(pm => pm.SendMessage(m1, null, It.IsAny<string>()));
            mockProducerModel.Verify(pm => pm.SendMessage(m2, null, It.IsAny<string>()));
            mockProducerModel.Verify(pm => pm.SendMessage(m3, null, It.IsAny<string>()));
        }
    }
}
