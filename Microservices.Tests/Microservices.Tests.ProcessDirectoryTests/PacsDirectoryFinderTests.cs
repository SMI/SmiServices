using Microservices.Common.Messages;
using Microservices.Common.Tests;
using Microservices.ProcessDirectory.Execution.DirectoryFinders;
using NUnit.Framework;
using System.Collections.Generic;
using Microservices.Common.Messaging;
using System.IO.Abstractions.TestingHelpers;
using Moq;

namespace Microservices.ProcessDirectory.Tests
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
            const string rootDir = @"C:\PACS\";
            var mockFs = new MockFileSystem();
            mockFs.AddFile(rootDir + @"2018\01\01\ABC123\testDicom.dcm", MockFileData.NullObject);

            // Test case, expected messages
            var testCases = new Dictionary<string, int>
            {
                { @"2018",                  1 },
                { @"2018\",                 1 },
                { @"2018\01",               1 },
                { @"2018\01\",              1 },
                { @"2018\01\01",            1 },
                { @"2018\01\01\",           1 },
                { @"2018\01\01\ABC123",     1 },
                { @"2018\01\01\ABC123\",    1 }
            };

            var totalSent = 0;

            var mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel
                .Setup(x => x.SendMessage(  It.IsAny<IMessage>(),
                                            null,
                                            ""))
                .Callback(()=> ++totalSent);

            var pacsFinder = new PacsDirectoryFinder(@"C:\PACS\", mockFs,"*.dcm", mockProducerModel.Object);

            foreach (KeyValuePair<string, int> item in testCases)
            {
                totalSent = 0;
                pacsFinder.SearchForDicomDirectories(rootDir + item.Key);

                Assert.AreEqual(item.Value, totalSent);
            }
        }
    }
}
