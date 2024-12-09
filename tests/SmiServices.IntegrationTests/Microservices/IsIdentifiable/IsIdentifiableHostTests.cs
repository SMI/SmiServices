using NUnit.Framework;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.IsIdentifiable;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.TestCommon;
using System;
using System.IO;

namespace SmiServices.IntegrationTests.Microservices.IsIdentifiable
{
    [TestFixture, RequiresRabbit]
    public class IsIdentifiableHostTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        /// <summary>
        /// The relative path to /data/ from the test bin directory
        /// </summary>
        public const string DataDirectory = @"../../../../../../../data/";

        [Test]
        public void TestClassifierName_NoClassifier()
        {
            var options = new GlobalOptionsFactory().Load(nameof(TestClassifierName_NoClassifier));

            options.IsIdentifiableServiceOptions!.ClassifierType = "";
            var ex = Assert.Throws<ArgumentException>(() => new IsIdentifiableHost(options));
            Assert.That(ex!.Message, Does.Contain("No IClassifier has been set in options.  Enter a value for " + nameof(options.IsIdentifiableServiceOptions.ClassifierType)));
        }

        [Test]
        public void TestClassifierName_NotRecognized()
        {
            var options = new GlobalOptionsFactory().Load(nameof(TestClassifierName_NotRecognized));
            options.IsIdentifiableServiceOptions!.DataDirectory = TestContext.CurrentContext.WorkDirectory;

            options.IsIdentifiableServiceOptions.ClassifierType = "HappyFunTimes";
            var ex = Assert.Throws<TypeLoadException>(() => new IsIdentifiableHost(options));
            Assert.That(ex!.Message, Does.Contain("Could not load type 'HappyFunTimes' from"));
        }

        [Test]
        public void TestClassifierName_ValidClassifier()
        {
            var options = new GlobalOptionsFactory().Load(nameof(TestClassifierName_ValidClassifier));

            var testDcm = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(TestClassifierName_ValidClassifier), "f1.dcm")); Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(TestClassifierName_ValidClassifier), "f1.dcm");
            new TestData().Create(testDcm);

            using var tester = new MicroserviceTester(options.RabbitOptions!, options.IsIdentifiableServiceOptions!);
            tester.CreateExchange(options.IsIdentifiableServiceOptions!.IsIdentifiableProducerOptions!.ExchangeName!, null);

            options.IsIdentifiableServiceOptions.ClassifierType = typeof(RejectAllClassifier).FullName;
            options.IsIdentifiableServiceOptions.DataDirectory = TestContext.CurrentContext.TestDirectory;

            var extractRoot = Path.Join(Path.GetTempPath(), "extractRoot");
            Directory.CreateDirectory(extractRoot);
            options.FileSystemOptions!.ExtractRoot = extractRoot;

            var host = new IsIdentifiableHost(options);
            Assert.That(host, Is.Not.Null);
            host.Start();

            tester.SendMessage(options.IsIdentifiableServiceOptions, new ExtractedFileStatusMessage
            {
                DicomFilePath = "yay.dcm",
                OutputFilePath = testDcm.FullName,
                ProjectNumber = "100",
                ExtractionDirectory = "./fish",
                Modality = "CT",
                StatusMessage = "yay!",
                Status = ExtractedFileStatus.Anonymised
            });

            TestTimelineAwaiter.Await(() => host.Consumer.AckCount == 1);
        }

        [Ignore("Requires leptonica fix")]
        [Test]
        public void TestIsIdentifiable_TesseractStanfordDicomFileClassifier()
        {
            var options = new GlobalOptionsFactory().Load(nameof(TestIsIdentifiable_TesseractStanfordDicomFileClassifier));

            // Create a test data directory containing IsIdentifiableRules with 0 rules, and tessdata with the eng.traineddata classifier
            // TODO(rkm 2020-04-14) This is a stop-gap solution until the tests are properly refactored
            var testRulesDir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "data", "IsIdentifiableRules"));
            testRulesDir.Create();
            options.IsIdentifiableServiceOptions!.DataDirectory = testRulesDir.Parent!.FullName;
            var tessDir = new DirectoryInfo(Path.Combine(testRulesDir.Parent.FullName, "tessdata"));
            tessDir.Create();
            var dest = Path.Combine(tessDir.FullName, "eng.traineddata");
            if (!File.Exists(dest))
                File.Copy(Path.Combine(DataDirectory, "tessdata", "eng.traineddata"), dest);

            var testDcm = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(TestIsIdentifiable_TesseractStanfordDicomFileClassifier), "f1.dcm"));

            Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(TestClassifierName_ValidClassifier), "f1.dcm");
            new TestData().Create(testDcm);

            using var tester = new MicroserviceTester(options.RabbitOptions!, options.IsIdentifiableServiceOptions);
            options.IsIdentifiableServiceOptions.ClassifierType = typeof(TesseractStanfordDicomFileClassifier).FullName;

            var host = new IsIdentifiableHost(options);
            host.Start();

            tester.SendMessage(options.IsIdentifiableServiceOptions, new ExtractedFileStatusMessage
            {
                DicomFilePath = "yay.dcm",
                OutputFilePath = testDcm.FullName,
                ProjectNumber = "100",
                ExtractionDirectory = "./fish",
                StatusMessage = "yay!",
                Status = ExtractedFileStatus.Anonymised
            });

            TestTimelineAwaiter.Await(() => host.Consumer.AckCount == 1 || host.Consumer.NackCount == 1);
            Assert.That(host.Consumer.AckCount, Is.EqualTo(1), "Tesseract not acking");
        }
    }
}
