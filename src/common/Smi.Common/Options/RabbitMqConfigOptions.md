FIXME: This is probably obsolete

## RabbitMQ Config Options

All the configuration settings are stored in a .yaml file. The default set of options is stored in the default.yaml file.

```yaml
RabbitOptions:
    RabbitMqHostName: "localhost"
    RabbitMqHostPort: 5672
    RabbitMqVirtualHost: "/"
    RabbitMqUserName: "guest"
    RabbitMqPassword: "guest"
    RabbitMqControlExchangeName: "ControlExchange"

    FatalLoggingExchange: "FatalLoggingExchange"

FileSystemOptions:
    FileSystemRoot: 'C:\temp'
    ExtractRoot: 'C:\temp'

RDMPOptions:
    Server: 'localhost\sqlexpress'
    CatalogueDb: "RDMP_Catalogue"
    ExportDb: "RDMP_DataExport"

MongoDatabases:
    DicomStoreOptions:
        HostName: "localhost"
        Port: 27017
        DatabaseName: "dicom"
        SeriesCollection: "series"
        ImageCollection: "image"
    ExtractionStoreOptions:
        HostName: "localhost"
        Port: 27017
        DatabaseName: "extraction"

DicomRelationalMapperOptions:
    Guid: "6ff062af-5538-473f-801c-ed2b751c7897"
    QueueName: "AnonymousImageQueue"
    ConsumerTag: "DicomRelationalMapper"
    QoSPrefetchCount: 10000
    AutoAck: false
    LoadMetadataId: 1
    DatabaseNamerType: "GuidDatabaseNamer"

CohortExtractorOptions:
    QueueName: "RequestQueue"
    ConsumerTag: "CohortExtractor"
    QoSPrefetchCount: 10000
    AutoAck: false
    AllCatalogues: true
    OnlyCatalogues: [1, 2, 3]
    # List of IDs of Catalogues to extract from (in ascending order).
    # Ignored if "AllCatalogues == true"
    #    - 2
    #    - 4
    #    - 5
    # also doable on a single line with [2,4,5] :)
    AuditorType: "Smi.CohortExtractor.Audit.NullAuditExtractions"
    RequestFulfillerType: "Smi.CohortExtractor.Execution.RequestFulfillers.FromCataloguesExtractionRequestFulfiller"
    # Writes (Producer) to this exchange
    ExtractFilesProducerOptions:
        Label: "ExtractFiles"
        ExchangeName: "ExtractFileExchange"
    # And audits this too
    ExtractFilesInfoProducerOptions:
        Label: "ExtractFilesInfos"
        ExchangeName: "FileCollectionInfoExchange"

CohortPackagerOptions:
    JobWatcherTickrate: 30
    ExtractRequestInfoOptions:
        QueueName: "RequestInfoQueue"
        ConsumerTag: "CohortPackager"
        QoSPrefetchCount: 1
        AutoAck: false
    ExtractFilesInfoOptions:
        QueueName: "FileCollectionInfoQueue"
        ConsumerTag: "CohortPackager"
        QoSPrefetchCount: 1
        AutoAck: false
    AnonImageStatusOptions:
        QueueName: "FileStatusQueue"
        ConsumerTag: "CohortPackager"
        QoSPrefetchCount: 1
        AutoAck: false

DicomReprocessorOptions:
    ProcessingMode: "ImageReprocessing"
    QueryScriptPath: '.\Scripts\abcd.txt'
    IdentImageProducerOptions:
        Label: "Image"
        ExchangeName: "IdentifiableImageExchange"
    TagPromotionProducerOptions:
        Label: "TagPromotion"
        ExchangeName: "TagPromotionExchange"

DicomTagReaderOptions:
    QueueName: "AccessionDirectoryQueue"
    ConsumerTag: "TagReader"
    QoSPrefetchCount: 1
    AutoAck: false
    NackIfAnyFileErrors: true
    ImageProducerOptions:
        Label: "ImageProducer"
        ExchangeName: "ImageExchange"
    SeriesProducerOptions:
        Label: "SeriesProducer"
        ExchangeName: "SeriesExchange"

IdentifierMapperOptions:
    QueueName: "IdentifiableImageQueue"
    ConsumerTag: "IdentifierMapper"
    QoSPrefetchCount: 1000
    AutoAck: false
    AnonImagesProducerOptions:
        Label: "AnonImages"
        ExchangeName: "AnonymousImageExchange"
    MappingConnectionString: 'Server=localhost\sqlexpress;Integrated Security=true;Initial Catalog=MappingDatabase;'
    MappingDatabaseType: "MicrosoftSQLServer"
    MappingTableName: "MappingTable"
    SwapColumnName: "CHI"
    ReplacementColumnName: "ECHI"
    SwapperType: "Smi.IdentifierMapper.Execution.ForGuidIdentifierSwapper"

MongoDbPopulatorOptions:
    SeriesQueueConsumerOptions:
        QueueName: "MongoSeriesQueue"
        ConsumerTag: "MongoDBPopulator"
        QoSPrefetchCount: 1000
        AutoAck: false
    ImageQueueConsumerOptions:
        QueueName: "MongoImageQueue"
        ConsumerTag: "MongoDBPopulator"
        QoSPrefetchCount: 10000
        AutoAck: false
    MongoDbFlushTime: 1000
    FailedWriteLimit: 5

ProcessDirectoryOptions:
    AccessionDirectoryProducerOptions:
        Label: "AnonImages"
        ExchangeName: "AccessionDirectoryExchange"
```

## How to use it

### C\#

Simply use the static `Load()` method on the `GlobalOptions` class and you will receive a strongly typed object with all
of the above settings.

By default this will load the `default.yaml` file, but the `Load()` method has an overload which will accept a filename as a string (with or without the `.yaml` extension)
and will load it for you if it exists). THis is useful if you want to override the settings from the command line arguments.

You can then pass the GlobalOptions instance to your specific host in the bootstrapper sequence:

```csharp
    private static int Main(string[] args)
    {
        var options = new GlobalOptionsFactory().Load();
        Parser.Default.ParseArguments(args, cli);

        var options = new GlobalOptionsFactory().Load(cli.YamlFile);

        var bootstrapper = new MicroserviceHostBootstrapper(
            () => new DicomTagReaderHost(options));
        return bootstrapper.Main();
    }
```

### Java

To be completed...

### Development, test and production

There are multiple yaml files into the folder. During development work and for debugging it is better to simply edit the `default.yaml` to match your development
environment.

During CI builds, the build script will replace the `default.yaml` contents with the `test.yaml` file, so it is important to keep them in sync if you
add or remove properties.

Production ready builds will replace the `default.yaml` with the `production.yaml` contents, although this file can also be edited after deployment and
you can use command line arguments to load other files. Again, it is important to keep them in sync, mainly in relation to added properties to avoid
exception while parsing.

## How to edit the yaml file

YAML syntax reference is abundant online. YAML is basically a sequence of key-value pairs separated by a colon `:`. Indentation is meaningful,
indented lines will be properties of the previous outdented line. Dashes indicate array elements, although you can use square brackets as well (see above).

If you want to add one option to an existing section, you need to create the corresponding property in the `GlobalOptions` class, as
everything is strongly typed and it will error if a property in the YAML file is not matched by the class (the reverse is allowed).

If you create a new microservice, put its specific settings into a new top-level element in the YAML file.
