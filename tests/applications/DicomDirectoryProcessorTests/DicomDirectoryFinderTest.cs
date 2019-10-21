using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Microservices.Common.Messages;
using Microservices.Common.Messaging;
using Microservices.ProcessDirectory.Execution.DirectoryFinders;
using Moq;
using NLog;
using NUnit.Framework;

namespace Microservices.ProcessDirectory.Tests
{
    /// <summary>
    /// Unit tests for BasicDicomDirectoryFinder.
    /// </summary>
    [TestFixture]
    public class DicomDirectoryFinderTest
    {
        [Test]
        public void FindingAccessionDirectory()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\root\foo\01\123.dcm", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 } ) },
                { @"c:\root\foo\02\456.dcm", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 } ) }
            });

            var rootDir = @"c:\root";

            var mockProducerModel = new Mock<IProducerModel>();

            var m1 = new AccessionDirectoryMessage
            {
                NationalPACSAccessionNumber = "01",
                DirectoryPath = @"foo\01"
            };

            var m2 = new AccessionDirectoryMessage
            {
                NationalPACSAccessionNumber = "02",
                DirectoryPath = @"foo\02"
            };


            LogManager.Configuration = new NLog.Config.LoggingConfiguration();

            var ddf = new BasicDicomDirectoryFinder(rootDir, fileSystem, "*.dcm", mockProducerModel.Object);
            ddf.SearchForDicomDirectories(rootDir);

            mockProducerModel.Verify(pm => pm.SendMessage(m1, It.IsAny<MessageHeader>(), It.IsAny<string>()));
            mockProducerModel.Verify(pm => pm.SendMessage(m2, It.IsAny<MessageHeader>(), It.IsAny<string>()));
        }
    }
}
