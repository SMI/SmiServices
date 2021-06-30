using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Service;
using NUnit.Framework;
using Smi.Common.Tests;
using System.IO;
using Smi.Common.Options;
using Tesseract;


namespace Microservices.IsIdentifiable.Tests
{
    public class TesseractLibTest
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
        public void TestFoo()
        {
            string cwd = TestContext.CurrentContext.TestDirectory;
            string dataDir = Path.Combine(cwd, "data");
            var tessDir = new DirectoryInfo(Path.Combine(dataDir, "tessdata"));
            tessDir.Create();
            string dest = Path.Combine(tessDir.FullName, "eng.traineddata");

            if (!File.Exists(dest)) {
                string projDataDir = Path.Combine(TestHelpers.GetProjectRoot(), "data");
                File.Copy(Path.Combine(projDataDir, "tessdata", "eng.traineddata"), dest);
            }

            var _ = new TesseractEngine(tessDir.FullName, "eng", EngineMode.Default);
        }

        #endregion
    }
}
