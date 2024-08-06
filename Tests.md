## Tests

SMI Microservices use RabbitMQ, MongoDb, Sql Server and MySql. These tests are run automatically by GitHub Actions.

## Connection Strings

Once you have set up the above dependencies you will need to set the connection strings in the following files (which should not be committed to the repository):

- [TestDatabases.txt](tests/SmiServices.UnitTests/TestDatabases.txt)
- [RelationalDatabases.yaml](tests/SmiServices.UnitTests/RelationalDatabases.yaml)
- [Mongo.yaml](tests/SmiServices.UnitTests/Mongo.yaml)
- [Rabbit.yaml](tests/SmiServices.UnitTests/Rabbit.yaml)

Tests involving [RDMP](https://github.com/HicServices/RDMP/) require the [RDMP databases to be set up](https://github.com/HicServices/RDMP/blob/develop/Documentation/CodeTutorials/Tests.md#database-tests) on the Sql Server. For example:

If running on Linux, the following command will do that (use latest version in url):

```
$ wget https://github.com/HicServices/RDMP/releases/download/v3.2.1/rdmp-cli-linux-x64.zip
$ unzip -d rdmp-cli rdmp-cli-linux-x64.zip || true # Ignore exit code since unzip returns 1 for a warning we don't care about
$ cd rdmp-cli
$ chmod +x rdmp
$ ./rdmp install localhost TEST_ -u sa -p 'YourStrongPassw0rd'
```
