
using NUnit.Framework;
using Smi.Common.Tests;


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
    }
}
