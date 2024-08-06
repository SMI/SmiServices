using NLog;
using NLog.Config;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace SmiServices.UnitTests.Common
{
    class LoggingTests
    {
        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void InvalidConfiguration_ThrowsException()
        {
            const string fileName = "fakeconfig.xml";

            File.WriteAllLines(fileName, new List<string> { "totally an xml file" });

            // No exception
            LogManager.Configuration = new XmlLoggingConfiguration(fileName);

            LogManager.ThrowConfigExceptions = true;
            Assert.Throws<NLogConfigurationException>(() => LogManager.Configuration = new XmlLoggingConfiguration(fileName));
        }

        #endregion
    }
}
