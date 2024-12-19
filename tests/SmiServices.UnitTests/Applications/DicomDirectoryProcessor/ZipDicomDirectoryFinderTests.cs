using Moq;
using NUnit.Framework;
using SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.UnitTests.Common;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework.Legacy;
using SmiServices.UnitTests.Common.Messaging;


namespace SmiServices.UnitTests.Applications.DicomDirectoryProcessor
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
        }

        [Test]
        public void FindRandomDicomsOrZipsDirectory()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.GetFullPath("/PACS/FFF/DDD/a.dcm"), new MockFileData([0x12, 0x34, 0x56, 0xd2] ) },
                { Path.GetFullPath("/PACS/FFF/b.dcm"), new MockFileData([0x12, 0x34, 0x56, 0xd2] ) },
                { Path.GetFullPath("/PACS/CCC/c.zip"), new MockFileData([0x12, 0x34, 0x56, 0xd2] ) },
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

            var rootDir = Path.GetFullPath("/PACS");
            var mockProducerModel = new TestProducer<AccessionDirectoryMessage>();
            var ddf = new ZipDicomDirectoryFinder(rootDir, fileSystem, "*.dcm", mockProducerModel);
            ddf.SearchForDicomDirectories(rootDir);

            Assert.That(new AccessionDirectoryMessage[] { m1, m2, m3 }, Is.EqualTo(mockProducerModel.Bodies).AsCollection);
        }
    }
}
