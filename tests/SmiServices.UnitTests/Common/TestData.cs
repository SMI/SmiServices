using System.IO;
using NUnit.Framework;

namespace SmiServices.UnitTests.Common
{
    public sealed class TestData
    {
        // Paths to the test DICOM files relative to TestContext.CurrentContext.TestDirectory
        // TODO(rkm 2020-11-16) Enum-ify these members so they can be strongly-typed instead of stringly-typed
        private const string TEST_DATA_DIR = "TestData";
        public static string IMG_013 = Path.Combine(TEST_DATA_DIR, "IM-0001-0013.dcm");
        public static string IMG_019 = Path.Combine(TEST_DATA_DIR, "IM-0001-0019.dcm");
        public static string IMG_024 = Path.Combine(TEST_DATA_DIR, "IM-0001-0024.dcm");
        public static string MANY_TAGS = Path.Combine(TEST_DATA_DIR, "FileWithLotsOfTags.dcm");
        public static string INVALID_DICOM = Path.Combine(TEST_DATA_DIR, "NotADicomFile.txt");
        public static string BURNED_IN_TEXT_IMG = Path.Combine(TEST_DATA_DIR, "burned-in-text-test.dcm");

        /// <summary>
        /// Creates the test image <see cref="IMG_013"/> in the file location specified
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="testFile">The test file to create, should be a static member of this class.  Defaults to <see cref="IMG_013"/></param>
        /// <returns></returns>
        public static FileInfo Create(FileInfo fileInfo, string? testFile = null)
        {
            var from = Path.Combine(TestContext.CurrentContext.TestDirectory, testFile ?? IMG_013);

            if (!fileInfo.Directory!.Exists)
                fileInfo.Directory.Create();

            File.Copy(from, fileInfo.FullName, true);

            return fileInfo;
        }
    }
}
