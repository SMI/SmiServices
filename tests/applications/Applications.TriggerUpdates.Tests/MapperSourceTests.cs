using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using TriggerUpdates;
using TriggerUpdates.Execution;

namespace Applications.TriggerUpdates.Tests
{
    class MapperSourceTests
    {
        [Test]
        public void TestMapperSource()
        {
            var soruce = new MapperSource(new TriggerUpdatesFromMapperOptions());
            Assert.Pass();
        }
    }
}
