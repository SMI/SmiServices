using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.IdentifierMapper.Execution.Swappers;
using Moq;
using NUnit.Framework;
using Smi.Common;
using Smi.Common.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microservices.CohortExtractor.Tests
{
    internal class CohortExtractorHostTests
    {
        [Test]
        public void TestRightSwapperType_NoRedis()
        {
            var opts = new GlobalOptions();
            opts.HostProcessName = "tests";

            opts.CohortExtractorOptions.RequestFulfillerType = typeof(FakeFulfiller).Name;

            opts.CohortExtractorOptions.ExtractionIdentifierSwapping = new ExtractionIdentifierSwappingOptions
            {
                MappingConnectionString = "ff",
                MappingDatabaseType = FAnsi.DatabaseType.MySql,
                MappingTableName = "mytbl",
                ReplacementColumnName = "anon",
                SwapColumnName = "priv",
                SwapperType = typeof(TableLookupWithGuidFallbackSwapper).FullName,
                TimeoutInSeconds = 100,
                //RedisConnectionString = "test"                
            };

            var host = new CohortExtractorHost(opts,null,null,Mock.Of<IRabbitMqAdapter>());

            var initSwapper = typeof(CohortExtractorHost).GetMethod("SetupSwapper", BindingFlags.Instance | BindingFlags.NonPublic);
            initSwapper.Invoke(host, null);

            Assert.IsInstanceOf<TableLookupWithGuidFallbackSwapper>(host.Swapper);
        }
        [Test]
        public void TestRightSwapperType_WithRedis()
        {
            var opts = new GlobalOptions();
            opts.HostProcessName = "tests";
            
            opts.CohortExtractorOptions.RequestFulfillerType = typeof(FakeFulfiller).Name;

            opts.CohortExtractorOptions.ExtractionIdentifierSwapping = new ExtractionIdentifierSwappingOptions
            {
                MappingConnectionString = "ff",
                MappingDatabaseType = FAnsi.DatabaseType.MySql,
                MappingTableName = "mytbl",
                ReplacementColumnName = "anon",
                SwapColumnName = "priv",
                SwapperType = typeof(TableLookupWithGuidFallbackSwapper).FullName,
                TimeoutInSeconds = 100,
                RedisConnectionString = "test"                
            };

            var host = new CohortExtractorHost(opts, null, null, Mock.Of<IRabbitMqAdapter>());

            var initSwapper = typeof(CohortExtractorHost).GetMethod("SetupSwapper", BindingFlags.Instance | BindingFlags.NonPublic);
            var ex = Assert.Throws<TargetInvocationException>(()=>initSwapper.Invoke(host, null));
            Assert.IsInstanceOf<RedisException>(ex.InnerException);
        }
    }
}
