using Applications.DicomDirectoryProcessor.Execution.DirectoryFinders;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Tests;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;


namespace Applications.DicomDirectoryProcessor.Tests
{
    [TestFixture]
    public class PacsDirectoryFinderTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [Test]
        public void TestRegexMatches()
        {
            string rootDir = Path.GetFullPath("/PACS");
            string testFile = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/AAA/testDicom.dcm"));
            var mockFs = new MockFileSystem();
            mockFs.AddFile(testFile, MockFileData.NullObject);

            // Test case, expected messages
            var testCases = new Dictionary<string, int>
            {
                { "2018",               1 },
                { "2018/",              1 },
                { "2018/01",            1 },
                { "2018/01/",           1 },
                { "2018/01/01",         1 },
                { "2018/01/01/",        1 },
                { "2018/01/01/AAA",     1 },
                { "2018/01/01/AAA/",    1 }
            };

            var totalSent = 0;

            var mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(),
                                            null,
                                            ""))
                .Callback(() => ++totalSent);

            var pacsFinder = new PacsDirectoryFinder(rootDir, mockFs, "*.dcm", mockProducerModel.Object);

            foreach (KeyValuePair<string, int> item in testCases)
            {
                totalSent = 0;
                pacsFinder.SearchForDicomDirectories(Path.GetFullPath(Path.Combine(rootDir, item.Key)));

                Assert.AreEqual(item.Value, totalSent);
            }
        }
    }
}
