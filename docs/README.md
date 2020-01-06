
# SMI Services Documentation

This is the documentation for the SMI Servies platform. It should (hopefully) contain enough information to run your own instance of the service.

The platform is currently deployed in the National Safe Haven, so some documentation may specifically refer to that environment. The software should be deployable in any environment though, so please open an [issue](https://github.com/SMI/SmiServices/issues) if anything isn't clear.


### Contents

- [Controlling the services](#controlling-the-services)
- [TODO](#todo)


## Controlling the services

The services can be controlled by sending messages to the RabbitMQ control exchange with specific routing keys. See the [main doc](control-queues.md)


## TODO

- Figure out what documentation can be (safely) imported from the old private repo
