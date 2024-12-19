using NUnit.Framework;
using SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders;
using SmiServices.Common.Messages;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using SmiServices.UnitTests.Common.Messaging;


namespace SmiServices.UnitTests.Applications.DicomDirectoryProcessor
{
    [TestFixture]
    public class PacsDirectoryFinderTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        [Test]
        public void TestRegexMatches()
        {
            string rootDir = Path.GetFullPath("/PACS");
            var mockFs = new MockFileSystem();

            string testFile = Path.GetFullPath(Path.Combine(rootDir, "2018/01/01/AAA/testDicom.dcm"));
            mockFs.AddFile(testFile, null);

            string specialCase1 = Path.GetFullPath(Path.Combine(rootDir, "2016/01/01/E-12345/testDicom.dcm"));
            mockFs.AddFile(specialCase1, null);

            string specialCase2 = Path.GetFullPath(Path.Combine(rootDir, "2017/01/01/01.01.2017/testDicom.dcm"));
            mockFs.AddFile(specialCase2, null);

            string multiLayer1 = Path.GetFullPath(Path.Combine(rootDir, "2015/01/01/E-12345/testDicom.dcm"));
            mockFs.AddFile(multiLayer1, null);

            string multiLayer2 = Path.GetFullPath(Path.Combine(rootDir, "2015/01/01/AAA/testDicom.dcm"));
            mockFs.AddFile(multiLayer2, null);

            string multiLayer3 = Path.GetFullPath(Path.Combine(rootDir, "2015/01/01/BBB/testDicom.dcm"));
            mockFs.AddFile(multiLayer3, null);

            // Test case, expected messages
            var testCases = new Dictionary<string, int>
            {
                { "2018",                   1 },
                { "2018/",                  1 },
                { "2018/01",                1 },
                { "2018/01/",               1 },
                { "2018/01/01",             1 },
                { "2018/01/01/",            1 },
                { "2015/01/01/",            3 },
                { "2018/01/01/AAA",         0 },
                { "2018/01/01/AAA/",        1 },
                { "2016/01/01/E-12345/",    1 },
                { "2017/01/01/01.01.2017/", 1 }
            };

            var mockProducerModel = new TestProducer<AccessionDirectoryMessage>();
            var pacsFinder = new PacsDirectoryFinder(rootDir, mockFs, "*.dcm", mockProducerModel);

            foreach (var (key, value) in testCases)
            {
                var prevTotalSent = mockProducerModel.TotalSent;
                pacsFinder.SearchForDicomDirectories(Path.GetFullPath(Path.Combine(rootDir, key)));

                Assert.That(mockProducerModel.TotalSent, Is.EqualTo(prevTotalSent + value));
            }
        }
    }
}
