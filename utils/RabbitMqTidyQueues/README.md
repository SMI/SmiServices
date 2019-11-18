
# RabbitMQ Tidy Queues Utility

Language: `Go`

Deletes any control queues that have not been properly cleaned by services which have crashed.

## Building

Download and install [Go](https://golang.org/dl/) and its associated build tools.

### Build for your local system

```bash
> go build
```

### Build on Windows for Linux

Run the [WindowsBuildForLinux.bat](./WindowsBuildForLinux.bat) script. This builds an ELF binary for `amd64-linux`.

In general, you need to appropriately set `GOARCH` and `GOOS` to build for another system, and then unset them afterwards to ensure they get returned back to their default values for your system.

## Usage

Run the build output on the same node as the RabbitMQ server.
