using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microservices.Common.Messages;
using Microservices.Common.Options;

namespace Microservices.Common.Tests
{
    public class TestTimeline
    {
        private readonly MicroserviceTester _tester;
        Queue<Action> Operations = new Queue<Action>();
        
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
            Operations.Enqueue(() => Thread.Sleep(milliseconds));
            return this;
        }

        public TestTimeline SendMessage(ConsumerOptions toConsumer,IMessage message)
        {
            Operations.Enqueue(()=>_tester.SendMessage(toConsumer,message));
            return this;
        }

        public void StartTimeline()
        {
            new Task(() =>
            {
                StartTime = DateTime.Now;

                foreach (Action a in Operations)
                    a();
            }).Start();
        }
    }
}
