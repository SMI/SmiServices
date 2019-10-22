namespace Smi.Common.Tests
{
    public sealed class TestData
    {
        // Paths to the test DICOM files relative to TestContext.CurrentContext.TestDirectory
        private const string TEST_DATA_DIR = "TestData";
        public string IMG_013 = System.IO.Path.Combine(TEST_DATA_DIR, "IM-0001-0013.dcm");
        public string IMG_019 = System.IO.Path.Combine(TEST_DATA_DIR, "IM-0001-0019.dcm");
        public string IMG_024 = System.IO.Path.Combine(TEST_DATA_DIR, "IM-0001-0024.dcm");
        public string MANY_TAGS = System.IO.Path.Combine(TEST_DATA_DIR, "FileWithLotsOfTags.dcm");
        public string INVALID_DICOM = System.IO.Path.Combine(TEST_DATA_DIR, "NotADicomFile.txt");
    }
}
