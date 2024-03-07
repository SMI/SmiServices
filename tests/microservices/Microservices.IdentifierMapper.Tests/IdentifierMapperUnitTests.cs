using System.Linq;
using NLog;
using NLog.Targets;
using NUnit.Framework;

namespace Microservices.IdentifierMapper.Tests
{
    public class IdentifierMapperUnitTests
    {
        [Test]
        public void Test_IdentifierMapper_LoggingCounts()
        {
            MemoryTarget target = new();                                                  
            target.Layout = "${message}";
            
            var mapper = new SwapForFixedValueTester("fish");
            StringAssert.AreEqualIgnoringCase("fish",mapper.GetSubstitutionFor("heyyy", out _));

            Assert.AreEqual(1,mapper.Success);

            LogManager.Setup().LoadConfiguration(x => x.ForLogger(LogLevel.Debug).WriteTo(target));

            Logger logger = LogManager.GetLogger("Example");

            mapper.LogProgress(logger, LogLevel.Info);

            StringAssert.StartsWith("SwapForFixedValueTester: CacheRatio=1:0 SuccessRatio=1:0:0",target.Logs.Single());
        }
    }
}
