using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using System.Collections.Generic;

namespace SmiServices.UnitTests.Common.Messaging
{
    public sealed class TestProducer<T> : IProducerModel<T> where T : IMessage
    {
        public int TotalSent => Bodies.Count;
        public readonly List<T> Bodies = [];
        public string? LastHeader { get; set; }

        public string? LastRoutingKey { get; set; }

        public T? LastMessage { get; set; }

        public void SendMessage(T message, string inResponseTo, string routingKey)
        {
            LastMessage = message;
            LastHeader = inResponseTo;
            LastRoutingKey = routingKey;
            Bodies.Add(message);
        }

        public IMessageHeader SendMessage(T message, IMessageHeader? isInResponseTo, string? routingKey) => throw new System.NotImplementedException();

        public void WaitForConfirms()
        {
        }

        public event ProducerFatalHandler? OnFatal;
    }
}
