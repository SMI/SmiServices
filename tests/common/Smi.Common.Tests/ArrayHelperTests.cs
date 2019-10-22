
using DicomTypeTranslation.Helpers;
using NUnit.Framework;

namespace Smi.Common.Tests
{
    public class ArrayHelperTests
    {
        [Test]
        public void TestStringRepresentation()
        {
            var a = new uint[2];
            a[0] = 10;
            a[1] = 123;

            Assert.AreEqual("10\\123", ArrayHelperMethods.GetStringRepresentation(a));

        }
    }
}
