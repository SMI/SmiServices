using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests
{
    class NoChisInAnyColumnsConstraintTests
    {
        [TestCase("0101010101")]
        [TestCase("0101010101 fish")]
        [TestCase("test)0101010101.")]
        [TestCase("1110101010.1")]
        public void Test_SingleValue_IsChi(string testValue)
        {
            var constraint = new NoChisInAnyColumnsConstraint();
            Assert.IsTrue(constraint.ContainsChi(testValue));
        }

        [TestCase("test)4401010101.")] //not a chi because theres no 44th day of the month
        [TestCase("test)1120010101.")] //not a chi because theres no 20th month
        [TestCase("11101010101")] //not a chi because digit is 11 long
        public void Test_SingleValue_IsNotChi(string testValue)
        {
            var constraint = new NoChisInAnyColumnsConstraint();
            Assert.IsFalse(constraint.ContainsChi(testValue));
        }

    }
}
