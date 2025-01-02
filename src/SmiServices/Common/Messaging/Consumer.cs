
using NLog;
using RabbitMQ.Client.Events;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmiServices.Common.Messaging;

public abstract class Consumer<T> : IConsumer<T> where T : IMessage
{
    /// <summary>
    /// Count of the messages Acknowledged by this Consumer, use <see cref="Ack(IMessageHeader, ulong)"/> to increment this
    /// </summary>
    public int AckCount { get; private set; }

    /// <summary>
    /// Count of the messages Rejected by this Consumer, use <see cref="ErrorAndNack"/> to increment this
    /// </summary>
    public int NackCount { get; private set; }

    /// <inheritdoc/>
    public bool HoldUnprocessableMessages { get; set; } = false;

    protected int _heldMessages = 0;

    /// <inheritdoc/>
    public int QoSPrefetchCount { get; set; }

    /// <summary>
    /// Event raised when Fatal method called
    /// </summary>
    public event ConsumerFatalHandler? OnFatal;
    public event AckEventHandler? OnAck;
    public event NackEventHandler? OnNack;

    protected readonly ILogger Logger;

    private readonly object _oConsumeLock = new();
    private bool _exiting;

    public virtual void Shutdown()
    {

    }

    protected Consumer()
    {
        string loggerName;
        Type consumerType = GetType();

        if (consumerType.IsGenericType)
        {
            string namePrefix = consumerType.Name.Split(new[] { '`' }, StringSplitOptions.RemoveEmptyEntries)[0];
            IEnumerable<string> genericParameters = consumerType.GetGenericArguments().Select(x => x.Name);
            loggerName = $"{namePrefix}<{string.Join(",", genericParameters)}>";
        }
        else
        {
            loggerName = consumerType.Name;
        }

        Logger = LogManager.GetLogger(loggerName);
    }

    public void ProcessMessage(IMessageHeader header, T message, ulong tag)
    {
        lock (_oConsumeLock)
        {
            if (_exiting)
                return;
        }

        try
        {
            ProcessMessageImpl(header, message, tag);
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Unhandled exception when processing message {header.MessageGuid}");

            if (HoldUnprocessableMessages)
            {
                ++_heldMessages;
                string msg = $"Holding an unprocessable message ({_heldMessages} total message(s) currently held";
                if (_heldMessages >= QoSPrefetchCount)
                    msg += $". Have now exceeded the configured BasicQos value of {QoSPrefetchCount}. No further messages will be delivered to this consumer!";
                Logger.Warn(msg);
            }
            else
            {
                Fatal("ProcessMessageImpl threw unhandled exception", e);
            }
        }
    }

    protected abstract void ProcessMessageImpl(IMessageHeader header, T message, ulong tag);

    /// <summary>
    /// Instructs RabbitMQ to discard a single message and not requeue it
    /// </summary>
    /// <param name="tag"></param>
    private void DiscardSingleMessage(ulong tag)
    {
        OnNack?.Invoke(this, new BasicNackEventArgs { DeliveryTag = tag, Multiple = false, Requeue = false });
        NackCount++;
    }

    protected virtual void ErrorAndNack(IMessageHeader header, ulong tag, string message, Exception exception)
    {
        header.Log(Logger, LogLevel.Error, message, exception);
        DiscardSingleMessage(tag);
    }

    protected void Ack(IMessageHeader header, ulong deliveryTag)
    {
        OnAck?.Invoke(this, new BasicAckEventArgs { DeliveryTag = deliveryTag, Multiple = false });
        header.Log(Logger, LogLevel.Trace, $"Acknowledged {header.MessageGuid}");
        AckCount++;
    }

    /// <summary>
    /// Acknowledges all in batch, this uses multiple which means you are accepting all up to the last message in the batch (including any not in your list
    /// for any reason)
    /// </summary>
    /// <param name="batchHeaders"></param>
    /// <param name="latestDeliveryTag"></param>
    protected void Ack(IList<IMessageHeader> batchHeaders, ulong latestDeliveryTag)
    {
        foreach (IMessageHeader header in batchHeaders)
            header.Log(Logger, LogLevel.Trace, "Acknowledged");

        AckCount += batchHeaders.Count;

        OnAck?.Invoke(this, new BasicAckEventArgs { DeliveryTag = latestDeliveryTag, Multiple = true });
    }

    /// <summary>
    /// Logs a Fatal in the Logger and triggers the FatalError event which should shutdown the MessageBroker
    /// <para>Do not do any further processing after triggering this method</para>
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="exception"></param>
    protected void Fatal(string msg, Exception exception)
    {
        lock (_oConsumeLock)
        {
            if (_exiting)
                return;

            _exiting = true;

            Logger.Fatal(exception, msg);

            ConsumerFatalHandler? onFatal = OnFatal;

            if (onFatal != null)
            {
                Task.Run(() => onFatal.Invoke(this, new FatalErrorEventArgs(msg, exception)));
            }
            else
            {
                throw new Exception("No handlers when attempting to raise OnFatal for this exception", exception);
            }
        }
    }
}
