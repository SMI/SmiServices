using Microservices.UpdateValues.Execution;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microservices.UpdateValues.Tests
{
    class UpdateTableAuditTests
    {
        [Test]
        public void TestTwoQueriesAtOnce()
        {
            var audit = new UpdateTableAudit(null);

            Assert.AreEqual(0,audit.ExecutingQueries);

            audit.StartOne();
            audit.StartOne();

            Assert.AreEqual(2,audit.ExecutingQueries);
            Assert.AreEqual(2,audit.Queries);

            audit.EndOne(2);
            audit.EndOne(5);
            
            Assert.AreEqual(0,audit.ExecutingQueries);
            Assert.AreEqual(2,audit.Queries);
            Assert.AreEqual(7,audit.AffectedRows);
        }
        [Test]
        public void TestManyQueriesAtOnce_MultiThreaded()
        {
            var audit = new UpdateTableAudit(null);

            Assert.AreEqual(0,audit.ExecutingQueries);

            List<Task> tasks = new();

            for(int i=0;i<50;i++)
            {
                tasks.Add(Task.Run(()=>{
                    audit.StartOne();
                    Task.Delay(TimeSpan.FromSeconds(5));
                    audit.EndOne(1);
                       }));
            }
                
            Task.WaitAll(tasks.ToArray());

            Assert.AreEqual(0,audit.ExecutingQueries);
            Assert.AreEqual(50,audit.Queries);
            Assert.IsFalse(audit.Stopwatch.IsRunning);
            Assert.LessOrEqual(audit.Stopwatch.ElapsedMilliseconds,TimeSpan.FromSeconds(10).TotalMilliseconds);
        }
    }
}
