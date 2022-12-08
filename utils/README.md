# Utils

## CI build scripts

-   `wait-for.bash`. Repeats the given command until it passes or times-out

## Other utils

-   `docker-compose`. A set of docker-compose files to provide containers for tests
-   RabbitMqTidyQueues. Automatically deletes any Control queues which have no consumers (those that have not been deleted by their host due to a crash).
-   RabbitMQ Dump Queue. Allows dumping of all messages in a RabbitMQ queue to JSON files. It can be downloaded from [here](https://github.com/dubek/rabbitmq-dump-queue).
