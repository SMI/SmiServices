using IsIdentifiable.Options;
using Microservices.IsIdentifiable.Service;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;

namespace Microservices.IsIdentifiable.Tests.Service
{
    class TesseractStanfordDicomFileClassifierTests
    {
        private MockFileSystem _fileSystem;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new MockFileSystem();
        }

        [Test]
        public void TestDataDirectory_DoesNotExist()
        {
            var d = _fileSystem.DirectoryInfo.New("asdflsdfjadfshsdfdsafldsf;dsfldsafj");
            Assert.Throws<System.IO.DirectoryNotFoundException>(()=>new TesseractStanfordDicomFileClassifier(d, new IsIdentifiableDicomFileOptions()));
        }

        [Test]
        public void TestDataDirectory_Empty()
        {
            var d = _fileSystem.DirectoryInfo.New(nameof(TestDataDirectory_Empty));
            d.Create();
            Assert.Throws<System.IO.FileNotFoundException>(()=>new TesseractStanfordDicomFileClassifier(d, new IsIdentifiableDicomFileOptions()));
        }
    }
}
