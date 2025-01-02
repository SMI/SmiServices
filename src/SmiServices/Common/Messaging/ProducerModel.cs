using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SmiServices.Common.Messaging;

/// <summary>
/// Class to implement sending of messages to a RabbitMQ exchange.
/// </summary>
public class ProducerModel : IProducerModel
{
    public event ProducerFatalHandler? OnFatal;

    private readonly ILogger _logger;

    private readonly IModel _model;
    private readonly IBasicProperties _messageBasicProperties;

    private readonly string _exchangeName;

    private readonly int _maxRetryAttempts;
    private const int ConfirmTimeoutMs = 5000;

    // Used to stop messages being produced if we are in the process of crashing out
    private readonly object _oSendLock = new();

    private readonly IBackoffProvider? _backoffProvider;

    // TODO(rkm 2024-10-02) Make this configurable
    private const int _probeCounterLimit = 1_000;

    private readonly TimeSpan _probeTimeout = TimeSpan.FromMinutes(1);

    // Start at the limit so an initial check is performed
    private int _probeMessageCounter = _probeCounterLimit;
    private readonly string? _probeQueueName;
    private readonly int _probeQueueLimit;

    /// <summary>
    /// 
    /// </summary> 
    /// <param name="exchangeName"></param>
    /// <param name="model"></param>
    /// <param name="properties"></param>
    /// <param name="maxRetryAttempts">Max number of times to retry message confirmations</param>
    /// <param name="backoffProvider"></param>
    /// <param name="probeQueueName"></param>
    /// <param name="probeQueueLimit"></param>
    /// <param name="probeTimeout"></param>
    public ProducerModel(
        string exchangeName, IModel model,
        IBasicProperties properties,
        int maxRetryAttempts = 1,
        IBackoffProvider? backoffProvider = null,
        string? probeQueueName = null,
        int probeQueueLimit = 0,
        TimeSpan? probeTimeout = null
    )
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("exchangeName parameter is invalid: \"" + exchangeName + "\"");

        _exchangeName = exchangeName;

        if (maxRetryAttempts < 0)
            throw new ArgumentException("maxRetryAttempts must be greater than 0. Given: " + maxRetryAttempts);

        _maxRetryAttempts = maxRetryAttempts;

        _logger = LogManager.GetLogger(GetType().Name);

        _model = model;
        _messageBasicProperties = properties;

        //TODO Understand this a bit better and investigate whether this also happens on consumer processes

        // Handle messages 'returned' by RabbitMQ - occurs when a messages published as persistent can't be routed to a queue
        _model.BasicReturn += (s, a) => _logger.Warn("BasicReturn for Exchange '{0}' Routing Key '{1}' ReplyCode '{2}' ({3})", a.Exchange, a.RoutingKey, a.ReplyCode, a.ReplyText);
        _model.BasicReturn += (s, a) => Fatal(a);

        // Handle RabbitMQ putting the queue into flow control mode
        _model.FlowControl += (s, a) => _logger.Warn("FlowControl for " + exchangeName);

        _backoffProvider = backoffProvider;

        _probeQueueName = probeQueueName;
        _probeQueueLimit = probeQueueLimit;
        if (probeTimeout != null)
            _probeTimeout = probeTimeout.Value;

        if (_probeQueueName != null)
        {
            var messageCount = model.MessageCount(_probeQueueName);
            _logger.Debug($"Probe queue has {messageCount} message(s)");
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inResponseTo"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    public virtual IMessageHeader SendMessage(IMessage message, IMessageHeader? inResponseTo = null, string? routingKey = null)
    {
        IMessageHeader header = SendMessageImpl(message, inResponseTo, routingKey);
        WaitForConfirms();
        header.Log(_logger, LogLevel.Trace, "Sent " + header.MessageGuid + " to " + _exchangeName);

        return header;
    }

    public void WaitForConfirms()
    {
        // Attempt to get a publish confirmation from RabbitMQ, with some retry/timeout

        var keepTrying = true;
        var numAttempts = 0;

        while (keepTrying)
        {
            if (_model.WaitForConfirms(TimeSpan.FromMilliseconds(ConfirmTimeoutMs), out var timedOut))
            {
                _backoffProvider?.Reset();
                return;
            }

            if (timedOut)
            {
                keepTrying = (++numAttempts < _maxRetryAttempts);
                _logger.Warn($"RabbitMQ WaitForConfirms timed out. numAttempts: {numAttempts}");

                TimeSpan? backoff = _backoffProvider?.GetNextBackoff();
                if (backoff.HasValue)
                {
                    _logger.Warn($"Backing off for {backoff}");
                    Thread.Sleep(backoff.Value);
                }

                continue;
            }

            throw new ApplicationException("RabbitMQ got a Nack");
        }

        throw new ApplicationException("Could not confirm message published after timeout");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inResponseTo"></param>
    /// <param name="routingKey"></param>
    /// <returns></returns>
    protected IMessageHeader SendMessageImpl(IMessage message, IMessageHeader? inResponseTo = null, string? routingKey = null)
    {
        lock (_oSendLock)
        {
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            _messageBasicProperties.Timestamp = new AmqpTimestamp(MessageHeader.UnixTimeNow());
            _messageBasicProperties.Headers = new Dictionary<string, object>();

            IMessageHeader header = inResponseTo != null ? new MessageHeader(inResponseTo) : new MessageHeader();
            header.Populate(_messageBasicProperties.Headers);

            if (_probeQueueName != null && _probeMessageCounter >= _probeCounterLimit)
            {
                while (_model.MessageCount(_probeQueueName) >= _probeQueueLimit)
                {
                    _logger.Warn($"Probe queue ({_probeQueueName}) over message limit ({_probeCounterLimit}). Sleeping for {_probeTimeout}");
                    Thread.Sleep(_probeTimeout);
                }
                _probeMessageCounter = 0;
            }

            _model.BasicPublish(_exchangeName, routingKey ?? "", true, _messageBasicProperties, body);
            ++_probeMessageCounter;

            return header;
        }
    }

    private void Fatal(BasicReturnEventArgs a)
    {
        lock (_oSendLock)
        {
            if (OnFatal != null)
                OnFatal.Invoke(this, a);
            else
                throw new ApplicationException("No subscribers for fatal error event");
        }
    }
}
