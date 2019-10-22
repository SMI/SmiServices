
using Applications.DicomRepopulator.Execution;
using Applications.DicomRepopulator.Options;
using Dicom;
using NUnit.Framework;
using Smi.Common.Tests;
using System.Collections.Generic;
using System.IO;

namespace DicomRepopulatorTests
{
    [TestFixture]
    public class DicomRepopulatorTests
    {
        private readonly string _inputFileBase = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestInput");
        private readonly string _outputFileBase = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestOutput");
        private readonly string _seriesFilesBase = Path.Combine(TestContext.CurrentContext.TestDirectory, "MultipleSeriesTest");

        //FIXME: Test file paths
        private const string IM_0001_0013_NAME = "IM_0001_0013.dcm";
        private const string IM_0001_0019_NAME = "IM_0001_0019.dcm";

        [SetUp]
        public void SetUp()
        {
            //TODO Move this to Test/Reusable project
            TestLogger.Setup();

            Directory.CreateDirectory(_inputFileBase);
            Directory.CreateDirectory(_outputFileBase);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_inputFileBase)) Directory.Delete(_inputFileBase, true);
            if (Directory.Exists(_outputFileBase)) Directory.Delete(_outputFileBase, true);
            if (Directory.Exists(_seriesFilesBase)) Directory.Delete(_seriesFilesBase, true);
        }

        [Test]
        public void SingleFileBasicOperationTest()
        {
            string inputDirPath = Path.Combine(_inputFileBase, "SingleFileBasicOperationTest");
            const string testFileName = IM_0001_0013_NAME;

            Directory.CreateDirectory(inputDirPath);
            //File.WriteAllBytes(Path.Combine(inputDirPath, testFileName), TestDicomFiles.IM_0001_0013);

            string outputDirPath = Path.Combine(_outputFileBase, "SingleFileBasicOperationTest");
            string expectedFile = Path.Combine(outputDirPath, testFileName);

            var options = new DicomRepopulatorOptions
            {
                CsvFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "BasicTest.csv"),
                DirectoryToProcessPath = inputDirPath,
                OutputDirectoryPath = outputDirPath,
                Mappings = new List<string> { "ID:PatientID" },
                Key = "SeriesInstanceUID:SeriesInstanceUID",
                NumThreads = 4
            };

            int result = new DicomRepopulatorProcessor(TestContext.CurrentContext.TestDirectory).Process(options);
            Assert.AreEqual(0, result);

            Assert.True(File.Exists(expectedFile), "Expected output file {0} to exist", expectedFile);

            DicomFile file = DicomFile.Open(expectedFile);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));
        }

        [Test]
        public void KeyNotFirstColumn()
        {
            string inputDirPath = Path.Combine(_inputFileBase, "KeyNotFirstColumn");
            const string testFileName = IM_0001_0013_NAME;

            Directory.CreateDirectory(inputDirPath);
            //File.WriteAllBytes(Path.Combine(inputDirPath, testFileName), TestDicomFiles.IM_0001_0013);

            string outputDirPath = Path.Combine(_outputFileBase, "KeyNotFirstColumn");
            string expectedFile = Path.Combine(outputDirPath, testFileName);

            var options = new DicomRepopulatorOptions
            {
                CsvFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "KeyNotFirstColumn.csv"),
                DirectoryToProcessPath = inputDirPath,
                OutputDirectoryPath = outputDirPath,
                Mappings = new List<string> { "ID:PatientID" },
                Key = "sopid:SOPInstanceUID",
                NumThreads = 4
            };

            int result = new DicomRepopulatorProcessor(TestContext.CurrentContext.TestDirectory).Process(options);
            Assert.AreEqual(0, result);

            Assert.True(File.Exists(expectedFile), "Expected output file {0} to exist", expectedFile);

            DicomFile file = DicomFile.Open(expectedFile);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));
        }

        [Test]
        public void DateRepopulation()
        {
            string inputDirPath = Path.Combine(_inputFileBase, "DateRepopulation");
            const string testFileName = IM_0001_0013_NAME;

            Directory.CreateDirectory(inputDirPath);
            //File.WriteAllBytes(Path.Combine(inputDirPath, testFileName), TestDicomFiles.IM_0001_0013);

            string outputDirPath = Path.Combine(_outputFileBase, "DateRepopulation");
            string expectedFile = Path.Combine(outputDirPath, testFileName);

            var options = new DicomRepopulatorOptions
            {
                CsvFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "WithDate.csv"),
                DirectoryToProcessPath = inputDirPath,
                OutputDirectoryPath = outputDirPath,
                Mappings = new List<string> { "ID:PatientID", "Date:StudyDate" },
                Key = "SeriesInstanceUID:SeriesInstanceUID",
                NumThreads = 4
            };

            int result = new DicomRepopulatorProcessor(TestContext.CurrentContext.TestDirectory).Process(options);
            Assert.AreEqual(0, result);

            Assert.True(File.Exists(expectedFile), "Expected output file {0} to exist", expectedFile);

            DicomFile file = DicomFile.Open(expectedFile);

            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));
            Assert.AreEqual("20180601", file.Dataset.GetValue<string>(DicomTag.StudyDate, 0));
        }

        [Test]
        public void OneCsvColumnToMultipleDicomTags()
        {
            string inputDirPath = Path.Combine(_inputFileBase, "OneCsvColumnToMultipleDicomTags");
            const string testFileName = IM_0001_0013_NAME;

            Directory.CreateDirectory(inputDirPath);
            //File.WriteAllBytes(Path.Combine(inputDirPath, testFileName), TestDicomFiles.IM_0001_0013);

            string outputDirPath = Path.Combine(_outputFileBase, "OneCsvColumnToMultipleDicomTags");
            string expectedFile = Path.Combine(outputDirPath, testFileName);

            var options = new DicomRepopulatorOptions
            {
                CsvFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "WithDate.csv"),
                DirectoryToProcessPath = inputDirPath,
                OutputDirectoryPath = outputDirPath,
                Mappings = new List<string> { "ID:PatientID", "Date:StudyDate", "Date:SeriesDate" },
                Key = "SeriesInstanceUID:SeriesInstanceUID",
                NumThreads = 1
            };

            int result = new DicomRepopulatorProcessor(TestContext.CurrentContext.TestDirectory).Process(options);
            Assert.AreEqual(0, result);

            Assert.True(File.Exists(expectedFile), "Expected output file {0} to exist", expectedFile);

            DicomFile file = DicomFile.Open(expectedFile);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));
            Assert.AreEqual("20180601", file.Dataset.GetValue<string>(DicomTag.StudyDate, 0));
            Assert.AreEqual("20180601", file.Dataset.GetValue<string>(DicomTag.SeriesDate, 0));
        }

        [Test]
        public void SpacesInCsvHeaderTest()
        {
            string inputDirPath = Path.Combine(_inputFileBase, "SpacesInCsvHeaderTest");
            const string testFileName = IM_0001_0013_NAME;

            Directory.CreateDirectory(inputDirPath);
            //File.WriteAllBytes(Path.Combine(inputDirPath, testFileName), TestDicomFiles.IM_0001_0013);

            string outputDirPath = Path.Combine(_outputFileBase, "SpacesInCsvHeaderTest");
            string expectedFile = Path.Combine(outputDirPath, testFileName);

            var options = new DicomRepopulatorOptions
            {
                CsvFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "SpacesInCsvHeaderTest.csv"),
                DirectoryToProcessPath = inputDirPath,
                OutputDirectoryPath = outputDirPath,
                Mappings = new List<string> { "ID:PatientID" },
                Key = "SeriesInstanceUID:SeriesInstanceUID",
                NumThreads = 1
            };

            int result = new DicomRepopulatorProcessor(TestContext.CurrentContext.TestDirectory).Process(options);
            Assert.AreEqual(0, result);

            Assert.True(File.Exists(expectedFile), "Expected output file {0} to exist", expectedFile);

            DicomFile file = DicomFile.Open(expectedFile);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));
        }

        [Test]
        public void MultipleFilesSameSeriesTest()
        {
            string inputDirPath = Path.Combine(_inputFileBase, "MultipleFilesSameSeriesTest");
            const string testFileName1 = IM_0001_0013_NAME;
            const string testFileName2 = IM_0001_0019_NAME;

            Directory.CreateDirectory(inputDirPath);
            //File.WriteAllBytes(Path.Combine(inputDirPath, testFileName1), TestDicomFiles.IM_0001_0013);
            //File.WriteAllBytes(Path.Combine(inputDirPath, testFileName2), TestDicomFiles.IM_0001_0013);

            string outputDirPath = Path.Combine(_outputFileBase, "MultipleFilesSameSeriesTest");
            string expectedFile1 = Path.Combine(outputDirPath, testFileName1);
            string expectedFile2 = Path.Combine(outputDirPath, testFileName2);

            var options = new DicomRepopulatorOptions
            {
                CsvFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "BasicTest.csv"),
                DirectoryToProcessPath = inputDirPath,
                OutputDirectoryPath = outputDirPath,
                Mappings = new List<string> { "ID:PatientID" },
                Key = "SeriesInstanceUID:SeriesInstanceUID",
                NumThreads = 1
            };

            int result = new DicomRepopulatorProcessor(TestContext.CurrentContext.TestDirectory).Process(options);
            Assert.AreEqual(0, result);

            Assert.True(File.Exists(expectedFile1), "Expected output file {0} to exist", expectedFile1);
            Assert.True(File.Exists(expectedFile2), "Expected output file {0} to exist", expectedFile2);

            DicomFile file = DicomFile.Open(expectedFile1);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));

            file = DicomFile.Open(expectedFile2);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));
        }

        [Test]
        public void MultipleSeriesTest()
        {
            string inputDirPath = Path.Combine(_seriesFilesBase, "TestInput");
            const string testFileName1 = IM_0001_0013_NAME;
            const string testFileName2 = IM_0001_0019_NAME;

            Directory.CreateDirectory(Path.Combine(inputDirPath, "Series1"));
            Directory.CreateDirectory(Path.Combine(inputDirPath, "Series2"));
            //File.WriteAllBytes(Path.Combine(inputDirPath, "Series1", testFileName1), TestDicomFiles.IM_0001_0013);
            //File.WriteAllBytes(Path.Combine(inputDirPath, "Series1", testFileName2), TestDicomFiles.IM_0001_0013);
            //File.WriteAllBytes(Path.Combine(inputDirPath, "Series2", testFileName1), TestDicomFiles.IM_0001_0019);
            //File.WriteAllBytes(Path.Combine(inputDirPath, "Series2", testFileName2), TestDicomFiles.IM_0001_0019);

            string outputDirPath = Path.Combine(_seriesFilesBase, "TestOutput");
            string expectedFile1 = Path.Combine(outputDirPath, "Series1", testFileName1);
            string expectedFile2 = Path.Combine(outputDirPath, "Series1", testFileName2);
            string expectedFile3 = Path.Combine(outputDirPath, "Series2", testFileName1);
            string expectedFile4 = Path.Combine(outputDirPath, "Series2", testFileName2);

            var options = new DicomRepopulatorOptions
            {
                CsvFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TwoSeriesCsv.csv"),
                DirectoryToProcessPath = inputDirPath,
                OutputDirectoryPath = outputDirPath,
                Mappings = new List<string> { "ID:PatientID" },
                Key = "SeriesInstanceUID:SeriesInstanceUID",
                NumThreads = 4
            };

            int result = new DicomRepopulatorProcessor(TestContext.CurrentContext.TestDirectory).Process(options);
            Assert.AreEqual(0, result);

            Assert.True(File.Exists(expectedFile1), "Expected output file {0} to exist", expectedFile1);
            Assert.True(File.Exists(expectedFile2), "Expected output file {0} to exist", expectedFile2);
            Assert.True(File.Exists(expectedFile3), "Expected output file {0} to exist", expectedFile3);
            Assert.True(File.Exists(expectedFile4), "Expected output file {0} to exist", expectedFile4);

            DicomFile file = DicomFile.Open(expectedFile1);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));

            file = DicomFile.Open(expectedFile2);
            Assert.AreEqual("NewPatientID1", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));

            file = DicomFile.Open(expectedFile3);
            Assert.AreEqual("NewPatientID2", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));

            file = DicomFile.Open(expectedFile4);
            Assert.AreEqual("NewPatientID2", file.Dataset.GetValue<string>(DicomTag.PatientID, 0));
        }
    }
}
