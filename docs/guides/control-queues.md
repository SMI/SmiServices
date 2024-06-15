# Microservice Control Queues

This describes how the services can be controlled via RabbitMQ messages.

## Commands

Commands are sent by publishing a message to the ControlExchange (specified in your config by `RabbitOptions.RabbitMqControlExchangeName`) with a specific routing key. This allows you to easily send them from the RabbitMQ web management page, or via a CLI.

RabbitMQ message routing keys are used to control which services receive the message. The current format for routing keys is `smi.control.<who>.<what>`. Where `<who>` is the name of the service, and `<what>` is some defined action. Note that all keys must be specified in lowercase. The currently defined actions are:

### General - any service

- `stop` - Stops the service
- `ping` - Logs a `pong` message. Useful for debugging

### DicomReprocessor

- `set-sleep-time-ms` - Sets the sleep time between batches. This also requires the new value to be set in the message body

### IdentifierMapper

- `refresh` - Refreshes any caches in use

### CohortPackager

- `processjobs` - Checks if any in progress jobs are complete

## Sending a message

Messages can be sent either via the Web UI or via a CLI (see below for details). In either case, the following applies:

- The `<who>` field must exactly match the name of the microservice process (e.g. `identifiermapper`)
- All routing keys should be lowercase
- `all` can be used as the `<who>` keyword to control all services
- A specific service can be messaged by including its `PID` at the end of the routing key. This is currently the only way to control a specific service instance rather than all services of a certain type

Examples of some routing keys:

```text
smi.control.all.stop # Stop all services
smi.control.dicomtagreader.stop # Stop all DicomTagReader services
smi.control.identifiermapper.refresh1234 # Refresh the IdentifierMapper service with PID `1234`
```

Note that some services may take some time to finish their current operation and exit after receiving a `shutdown` command.

### Via the Web UI

On your RabbitMQ Management interface (`http://<rabbit host>:15672`), click `Exchanges` then `Control Exchange`. Expand the `Publish message` box then enter the message info. Any required content should be entered into the `Payload` box in plain text.

## Implementing A New Control Command Handler

Implement a class which contains a method with the following signature:

```c#
void MyControlHandler(string action, string message)
```

Then, instantiate your class and register its event in your host (must be a subclass of `MicroserviceHost`):

```c#
var controlClass = new MyControlClass(...);
AddControlHandler(controlClass.MyControlHandler);
```

That's it! Now you will be passed the full routing key for any control message addressed to your specific microservice type (i.e. where the `<who>` part of the routing key matches your microservice name), and any message content.

## Control Queues and Cleanup

The actual implementation of the control queues works as follows:

- When each service starts up, it creates a new queue named with its service name and process ID
- It then binds this queue to the global `ControlExchange`. Two bindings are created:
  - `smi.control.all.*`: Matches any "send to all" routing keys
  - `smi.control.<process_name>.*`: Matches "all services of my type" routing keys
- On shutdown (when the RMQ connection is closed), the control queue should be automatically deleted by the server

The creation of the control queue is performed during a single ad-hoc connection, and is not part of the standard Consumer process (for _reasons_). One consequence of this is that if a microservice crashes _after_ the control queue is created, but _before_ the actual subscription to the queue is started (i.e. at some point during startup before RabbitMQAdapter.StartConsumer is called), then the control queue may not be automatically deleted. This isn't really an issue other than causing visual clutter on the RabbitMQ management interface. These dangling queues can be manually deleted with the [TidyQueues](/utils/RabbitMqTidyQueues) utility tool.
