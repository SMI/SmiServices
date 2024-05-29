using NUnit.Framework;
using Smi.Common.Options;
using System;
using Applications.TriggerUpdates.Execution;
using Applications.TriggerUpdates.Options;


namespace Applications.TriggerUpdates.Tests
{
    class MapperSourceUnitTests
    {
        [Test]
        public void TestNoIdentifierMapperOptions()
        {
            var ex = Assert.Throws<ArgumentException>(() => new MapperSource(new GlobalOptions(), new TriggerUpdatesFromMapperOptions()));
            Assert.That(ex!.Message,Is.EqualTo("No SwapperType has been specified in GlobalOptions.IdentifierMapperOptions"));

        }
        [Test]
        public void TestNoSwapper()
        {
            var ex = Assert.Throws<ArgumentException>(() => new MapperSource(new GlobalOptions { IdentifierMapperOptions = new IdentifierMapperOptions() }, new TriggerUpdatesFromMapperOptions()));
            Assert.That(ex!.Message,Is.EqualTo("No SwapperType has been specified in GlobalOptions.IdentifierMapperOptions"));
        }
        [Test]
        public void InvalidSwapper()
        {
            var ex = Assert.Throws<Exception>(() => new MapperSource(new GlobalOptions
            {
                IdentifierMapperOptions = new IdentifierMapperOptions
                {
                    SwapperType = "Trollolol"
                }
            }
            , new TriggerUpdatesFromMapperOptions()));

            Assert.That(ex!.Message,Is.EqualTo("Could not create IdentifierMapper Swapper with SwapperType:Trollolol"));
        }
    }
}
