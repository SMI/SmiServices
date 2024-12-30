using RabbitMQ.Client.Events;
using SmiServices.Common.Messages;

namespace SmiServices.Common.Events;

public delegate void AckEventHandler(object sender, BasicAckEventArgs args);

public delegate void NackEventHandler(object sender, BasicNackEventArgs args);

public delegate void SmiAckEventHandler(object sender, SmiAckEventArgs args);

/// <summary>
/// Subclass of <see cref="BasicAckEventArgs"/> including the relevant <see cref="IMessageHeader"/>
/// </summary>
/// <param name="header"></param>
public class SmiAckEventArgs(IMessageHeader header) : BasicAckEventArgs
{
    public IMessageHeader Header { get; set; } = header;
}
