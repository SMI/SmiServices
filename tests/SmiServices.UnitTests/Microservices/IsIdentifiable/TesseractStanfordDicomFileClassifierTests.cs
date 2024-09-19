using IsIdentifiable;
using IsIdentifiable.Options;
using NUnit.Framework;
using SmiServices.Microservices.IsIdentifiable;
using System.IO;
using Tesseract;

namespace SmiServices.UnitTests.Microservices.IsIdentifiable
{
    class TesseractStanfordDicomFileClassifierTests
    {
        [Test]
        public void TestDataDirectory_DoesNotExist()
        {
            var d = new DirectoryInfo("asdflsdfjadfshsdfdsafldsf;dsfldsafj");
            Assert.Throws<DirectoryNotFoundException>(() => new TesseractStanfordDicomFileClassifier(d, new IsIdentifiableDicomFileOptions()));
        }
        [Test]
        public void TestDataDirectory_Empty()
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, nameof(TestDataDirectory_Empty));

            var d = new DirectoryInfo(path);
            d.Create();
            Assert.Throws<FileNotFoundException>(() => new TesseractStanfordDicomFileClassifier(d, new IsIdentifiableDicomFileOptions()));
        }

        [Test]
        [Platform(Exclude="Win")]
        public void TesseractEngine_CanBeConstructed()
        {
            // Arrange
            const string tessdataDirectory = @"../../../../../data/tessdata";
            var d = new DirectoryInfo(tessdataDirectory);

            TesseractLinuxLoaderFix.Patch();

            // Act
            // Assert
            Assert.DoesNotThrow(() => new TesseractEngine(d.FullName, "eng", EngineMode.Default));
        }
    }
}
