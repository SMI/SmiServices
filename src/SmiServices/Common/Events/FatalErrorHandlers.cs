
using RabbitMQ.Client.Events;

namespace SmiServices.Common.Events;

public delegate void HostFatalHandler(object sender, FatalErrorEventArgs e);

public delegate void ConsumerFatalHandler(object sender, FatalErrorEventArgs e);

public delegate void ProducerFatalHandler(object sender, BasicReturnEventArgs e);
