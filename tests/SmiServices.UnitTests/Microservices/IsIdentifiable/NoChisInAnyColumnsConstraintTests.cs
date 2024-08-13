using NUnit.Framework;
using SmiServices.Microservices.IsIdentifiable;

namespace SmiServices.UnitTests.Microservices.IsIdentifiable
{
    class NoChisInAnyColumnsConstraintTests
    {
        [TestCase("0101010101")]
        [TestCase("0101010101 fish")]
        [TestCase("test)0101010101.")]
        [TestCase("1110101010.1")]
        public void Test_SingleValue_IsChi(string testValue)
        {
            _ = new NoChisInAnyColumnsConstraint();
            Assert.That(NoChisInAnyColumnsConstraint.Validate([testValue], [nameof(testValue)]), Is.EqualTo($"Found chi in field {nameof(testValue)}"));
        }

        [TestCase("test)4401010101.")] //not a chi because there's no 44th day of the month
        [TestCase("test)1120010101.")] //not a chi because there's no 20th month
        [TestCase("11101010101")] //not a chi because 11 digits is too long
        public void Test_SingleValue_IsNotChi(string testValue)
        {
            _ = new NoChisInAnyColumnsConstraint();
            Assert.That(NoChisInAnyColumnsConstraint.Validate([testValue], [nameof(testValue)]), Is.Null);
        }

    }
}
