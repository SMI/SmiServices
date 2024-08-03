
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

            Assert.That(ArrayHelperMethods.GetStringRepresentation(a), Is.EqualTo("10\\123"));

        }
    }
}
