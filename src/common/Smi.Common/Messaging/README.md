# Microservice Control Queues

## Contents

1. [Background](#background)
2. [Commands](#control-commands)
3. [Implementing a new control command handler](#implementing-a-new-control-command-handler)
4. [Control Queues and Cleanup](#control-queues-and-cleanup)

### Background

We had two requirements driving this:

- The IdentifierMapper service needed to have its cached swapping dictionary refreshed occasionally, without needing to be restarted
- Generally, we wished to be able to shutdown all/some/a specific microservice easily from RabbitMQ

### Commands

Commands are sent by publishing a blank message to the SMI.ControlExchange, with a specific routing key. This allows you to easily send them from the RabbitMQ web management page. Currently the supported format for routing keys is `smi.control.<who>.<what>`, can expand on this later if needed.

Currently we have 1 general command (`stop`), which you can send by setting the following routing keys:

- `smi.control.all.stop` -- Stop & shutdown all microservices
- `smi.control.dicomtagreader.stop` --  Stop all DicomTagReader microservices
- `smi.control.dicomtagreader.stop1234` -- Stop the DicomTagReader service with ID `1234`

We also have 1 command specific to the Identifier Mapper microservice, `refresh`:

- `smi.control.identifiermapper.refresh` - Refreshes all IdentifierMapper microservices
- `smi.control.identifiermapper.refresh1234` -- Refresh the IdentifierMapper service with ID `1234`

Example:

![test](../../../../docs/images/control-queue-publish.PNG)

Notes:
The name for the microservice (e.g. `identifiermapper`) must match the name of the microservice process.
All routing keys should be lowercase.

### Implementing A New Control Command Handler

Firstly, add this to your App.config to indicate you wish to be passed control events:

```xml
<add key="UsesControlEvents" value="true"/>
```

Next, implement a class & method where you wish to handle the events. The method must have the signature:

```c#
void MyControlHandler(string routingKey)
```

Lastly, add this to your host class:
```c#
var controlClass = new MyControlClass(...);
AddControlHandler(controlClass.MyControlHandler);
```

That's it! Now you will be passed the full routing key for any control message addressed to your specific microservice type (i.e. where the `<who>` part of the routing key matches your microservice name).

### Control Queues and Cleanup

The actual implementation of the control queues works as follows:

- When each service starts up, it creates a new queue named with its service name and process ID
- It then binds this queue to the global `ControlExchange`. Two bindings are created:
  - `smi.control.all.*`: Matches any "send to all" routing keys
  - `smi.control.<process_name>.*`: Matches "all services of my type" routing keys
- On shutdown (when the RMQ connection is closed), the control queue should be automatically delted by the server

The creation of the control queue is performed during a single ad-hoc connection, and is not part of the standard Consumer process (for _reasons_). One consequence of this is that if a microservice crashes _after_ the control queue is created, but _before_ the actual subscription to the queue is started (i.e. at some point during startup before RabbitMQAdapter.StartConsumer is called), then the control queue may not be automatically deleted. This isn't really an issue other than causing visual clutter on the RabbitMQ management interface. These dangling queues can be manually deleted with the [TidyQueues](../../../Utils/RabbitMqTidyQueues) utility tool.

