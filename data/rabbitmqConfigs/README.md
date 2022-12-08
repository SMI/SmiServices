# RabbitMQ Configuration

## Reset the server

The following will **completely** reset the state of your RabbitMQ instance, including removing all users, vhosts, exchanges, queues, vhosts, and messages.

Find your installation folder (default appears to be `C:\Program Files\RabbitMQ Server\rabbitmq_server-3.7.3\sbin` on Windows). From there, run the following:

```
rabbitmqctl.bat stop_app

rabbitmqctl.bat reset

rabbitmqctl.bat start_app
```

Can also just run the `ResetRabbitMQ.ps1` script included in this directory.

For Linux, remove the `.bat` extension from the commands above, and run as sudo.

## Uploading broker defnintions

From Linux, the exchange and queue definitions can be uploaded using curl:

```bash
$ curl \
-u guest:guest \
-XPOST -H"content-type:application/json" \
-d"@/full/path/to/defaultExtractConfig.json" \
http://0.0.0.0:15672/api/definitions
```

## Deleting a vhost

```bash
> curl \
    -u guest:guest \
    -XDELETE \
    http://0.0.0.0:15672/api/vhosts/<name>
```

## Filter the default exchanges

RabbitMQ has predefined exchanges which can't be removed from the management UI. To filter these out, tick the `regex` checkbox next to the search box on the `Exchanges` tab, then use this regex:

```regex
^(?!amq\.)(.+)$
```

## Filter the control queues

The microservice control queues can be filtered out from the default display with the following regex:

```regex
^(?!Control\.)(.+)$
```
