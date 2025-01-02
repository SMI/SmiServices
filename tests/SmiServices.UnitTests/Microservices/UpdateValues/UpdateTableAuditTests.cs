using NUnit.Framework;
using SmiServices.Microservices.UpdateValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmiServices.UnitTests.Microservices.UpdateValues;

class UpdateTableAuditTests
{
    [Test]
    public void TestTwoQueriesAtOnce()
    {
        var audit = new UpdateTableAudit(null);

        Assert.That(audit.ExecutingQueries, Is.EqualTo(0));

        audit.StartOne();
        audit.StartOne();

        Assert.Multiple(() =>
        {
            Assert.That(audit.ExecutingQueries, Is.EqualTo(2));
            Assert.That(audit.Queries, Is.EqualTo(2));
        });

        audit.EndOne(2);
        audit.EndOne(5);

        Assert.Multiple(() =>
        {
            Assert.That(audit.ExecutingQueries, Is.EqualTo(0));
            Assert.That(audit.Queries, Is.EqualTo(2));
            Assert.That(audit.AffectedRows, Is.EqualTo(7));
        });
    }
    [Test]
    public void TestManyQueriesAtOnce_MultiThreaded()
    {
        var audit = new UpdateTableAudit(null);

        Assert.That(audit.ExecutingQueries, Is.EqualTo(0));

        List<Task> tasks = [];

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                audit.StartOne();
                Task.Delay(TimeSpan.FromSeconds(5));
                audit.EndOne(1);
            }));
        }

        Task.WaitAll([.. tasks]);

        Assert.Multiple(() =>
        {
            Assert.That(audit.ExecutingQueries, Is.EqualTo(0));
            Assert.That(audit.Queries, Is.EqualTo(50));
            Assert.That(audit.Stopwatch.IsRunning, Is.False);
            Assert.That(audit.Stopwatch.ElapsedMilliseconds, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(10).TotalMilliseconds));
        });
    }
}
