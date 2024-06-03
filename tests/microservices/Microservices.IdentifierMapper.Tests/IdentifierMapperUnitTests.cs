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
            Assert.Multiple(() =>
            {
                Assert.That(mapper.GetSubstitutionFor("heyyy",out _),Is.EqualTo("fish").IgnoreCase);

                Assert.That(mapper.Success,Is.EqualTo(1));
            });

            LogManager.Setup().LoadConfiguration(x => x.ForLogger(LogLevel.Debug).WriteTo(target));

            Logger logger = LogManager.GetLogger("Example");

            mapper.LogProgress(logger, LogLevel.Info);

            Assert.That(target.Logs.Single(),Does.StartWith("SwapForFixedValueTester: CacheRatio=1:0 SuccessRatio=1:0:0"));
        }
    }
}
