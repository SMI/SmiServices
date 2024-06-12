# RabbitMQ Configuration

RabbitMQ definitions can be uploaded via its web management interface, of from a CLI e.g.:

```console
curl \
    -u guest:guest \
    -XPOST -H"content-type:application/json" \
    -d"@/full/path/to/defaultExtractConfig.json" \
    http://0.0.0.0:15672/api/definitions
```

## Tips

### Filter the default exchanges

RabbitMQ has predefined exchanges which can't be removed from the management interface. To filter these out, tick the `regex` checkbox next to the search box on the `Exchanges` tab, then use this regex:

```
^(?!amq\.)(.+)$
```

### Filter the control queues

The microservice control queues can be filtered out from the default display with the following regex:

```
^(?!Control\.)(.+)$
```
