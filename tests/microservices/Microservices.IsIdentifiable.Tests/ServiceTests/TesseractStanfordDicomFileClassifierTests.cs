using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Service;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests.ServiceTests
{
    class TesseractStanfordDicomFileClassifierTests
    {

        //e.g. english.all.3class.distsim.crf.ser.gz
        [Test]
        public void TestDataDirectory_DoesNotExist()
        {
            var d = new DirectoryInfo("asdflsdfjadfshsdfdsafldsf;dsfldsafj");
            Assert.Throws<DirectoryNotFoundException>(()=>new TesseractStanfordDicomFileClassifier(d, new IsIdentifiableServiceOptions()));
        }
        [Test]
        public void TestDataDirectory_Empty()
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, nameof(TestDataDirectory_Empty));

            var d = new DirectoryInfo(path);
            d.Create();
            Assert.Throws<FileNotFoundException>(()=>new TesseractStanfordDicomFileClassifier(d, new IsIdentifiableServiceOptions()));
        }
    }
}
