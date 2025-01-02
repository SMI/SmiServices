using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmiServices.UnitTests.Common;

public class TestTimeline : IDisposable
{
    private readonly MicroserviceTester _tester;
    readonly Queue<Action> Operations = new();

    public CancellationTokenSource cts = new();

    /// <summary>
    /// The exact time the TestTimeline was last started
    /// </summary>
    public DateTime StartTime { get; private set; }

    public TestTimeline(MicroserviceTester tester)
    {
        _tester = tester;
    }

    public TestTimeline Wait(int milliseconds)
    {
        Operations.Enqueue(() => Task.Delay(milliseconds, cts.Token));
        return this;
    }

    public TestTimeline SendMessage(ConsumerOptions toConsumer, IMessage message)
    {
        Operations.Enqueue(() => _tester.SendMessage(toConsumer, message));
        return this;
    }

    public void StartTimeline()
    {
        new Task(() =>
        {
            StartTime = DateTime.Now;

            foreach (Action a in Operations)
                if (cts.IsCancellationRequested)
                    break;
                else
                    a();
        }).Start();
    }

    public void Dispose()
    {
        _tester?.Dispose();
        cts.Cancel();
        cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
