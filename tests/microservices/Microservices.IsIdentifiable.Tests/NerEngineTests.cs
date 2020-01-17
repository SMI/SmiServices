using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Whitelists;
using Moq;
using NUnit.Framework;

namespace Microservices.IsIdentifiable.Tests
{
    class NerEngineTests
    {
        public const string ClassifierPath = @"C:\temp\stanford-ner-2017-06-09\classifiers\english.all.3class.distsim.crf.ser.gz";

        [Test]
        public void NerEngine_TestWhitelist()
        {
            string f = ClassifierPath;

            if (!File.Exists(f))
                Assert.Inconclusive("File did not exist " + f);

            var whitelist = Mock.Of<IWhitelistSource>(
                w => w.GetWhitelist() == new[] {"Good afternoon Rajat Raina, how are you today?"});

            var engine = new NerEngine(f,whitelist);
            
            Assert.IsEmpty(engine.MatchNames("Good afternoon Rajat Raina, how are you today?"));

            engine = new NerEngine(f, null);

            Assert.IsNotEmpty(engine.MatchNames("Good afternoon Rajat Raina, how are you today?"));
        }

        [Test]
        public void Test_CsvWhitelist()
        {
            var f = Path.Combine(TestContext.CurrentContext.TestDirectory, "test.txt");

            File.WriteAllText(f,"abc\r\ncde\r\n\r\n"); //2 rows no header (ignore whitespace)

            var w = new CsvWhitelist(f);

            var result = w.GetWhitelist().ToArray();
            
            Assert.AreEqual(2,result.Count());
            Assert.AreEqual("abc",result[0]);
            Assert.AreEqual("cde", result[1]);

        }
    }
}