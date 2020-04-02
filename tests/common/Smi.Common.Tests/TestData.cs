using System.IO;
using NUnit.Framework;

namespace Smi.Common.Tests
{
    public sealed class TestData
    {
        // Paths to the test DICOM files relative to TestContext.CurrentContext.TestDirectory
        private const string TEST_DATA_DIR = "TestData";
        public static string IMG_013 = System.IO.Path.Combine(TEST_DATA_DIR, "IM-0001-0013.dcm");
        public static string IMG_019 = System.IO.Path.Combine(TEST_DATA_DIR, "IM-0001-0019.dcm");
        public static string IMG_024 = System.IO.Path.Combine(TEST_DATA_DIR, "IM-0001-0024.dcm");
        public static string MANY_TAGS = System.IO.Path.Combine(TEST_DATA_DIR, "FileWithLotsOfTags.dcm");
        public static string INVALID_DICOM = System.IO.Path.Combine(TEST_DATA_DIR, "NotADicomFile.txt");

        /// <summary>
        /// Creates the test image <see cref="IMG_013"/> in the file location specified
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="testFile">The test file to create, should be a static member of this class.  Defaults to <see cref="IMG_013"/></param>
        /// <returns></returns>
        public static FileInfo Create(FileInfo fileInfo, string testFile=null)
        {
            var from = Path.Combine(TestContext.CurrentContext.TestDirectory, testFile??IMG_013);

            if(!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();

            File.Copy(from,fileInfo.FullName,true);

            return fileInfo;
        }
    }
}
