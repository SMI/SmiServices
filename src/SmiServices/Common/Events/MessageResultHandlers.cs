using RabbitMQ.Client.Events;

namespace SmiServices.Common.Events;

public delegate void AckEventHandler(object sender, BasicAckEventArgs args);

public delegate void NackEventHandler(object sender, BasicNackEventArgs args);
