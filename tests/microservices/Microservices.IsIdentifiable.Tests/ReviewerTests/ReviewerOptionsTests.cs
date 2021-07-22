using IsIdentifiableReviewer;
using NUnit.Framework;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservices.IsIdentifiable.Tests.ReviewerTests
{
    class ReviewerOptionsTests
    {
        [Test]
        public void TestFillMissingWithValuesUsing_MissingValues()
        {
            var global =  new IsIdentifiableReviewerGlobalOptions();
            var local = new IsIdentifiableReviewerOptions();

            global.IgnoreList = "aa";
            global.RedList = "bb";
            global.TargetsFile = "cc";
            global.Theme = "dd";

            local.FillMissingWithValuesUsing(global);

            Assert.AreEqual("aa", local.IgnoreList);
            Assert.AreEqual("bb", local.RedList);
            Assert.AreEqual("cc", local.TargetsFile);
            Assert.AreEqual("dd", local.Theme.Name);
        }
        [Test]
        public void TestFillMissingWithValuesUsing_DoNotOverride()
        {
            var global = new IsIdentifiableReviewerGlobalOptions();
            var local = new IsIdentifiableReviewerOptions();

            global.IgnoreList = "aa";
            global.RedList = "bb";
            global.TargetsFile = "cc";
            global.Theme = "dd";


            local.IgnoreList = "11";
            local.RedList = "22";
            local.TargetsFile = "33";
            local.Theme = new System.IO.FileInfo("44");

            local.FillMissingWithValuesUsing(global);

            Assert.AreEqual("11", local.IgnoreList);
            Assert.AreEqual("22", local.RedList);
            Assert.AreEqual("33", local.TargetsFile);
            Assert.AreEqual("44", local.Theme.Name);
        }
    }
}
