
using FAnsi.Discovery;
using FellowOakDicom;
using IsIdentifiable.Options;
using Microsoft.Data.SqlClient;
using RabbitMQ.Client;
using Rdmp.Core.DataLoad.Engine.Checks.Checkers;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Messaging;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using DatabaseType = FAnsi.DatabaseType;

namespace SmiServices.Common.Options;

public interface IOptions
{

}

public class GlobalOptions : IOptions
{
    #region AllOptions

    private string? _hostProcessName;

    public string HostProcessName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_hostProcessName))
                throw new ArgumentException("HostProcessName not set");
            return _hostProcessName;
        }
        set
        {
            if (_hostProcessName != null)
                throw new ArgumentException("HostProcessName already set");
            _hostProcessName = value;
        }
    }

    public MessageBrokerType? MessageBrokerType { get; set; } = Messaging.MessageBrokerType.RabbitMQ;

    public LoggingOptions? LoggingOptions { get; set; } = new LoggingOptions();
    public RabbitOptions? RabbitOptions { get; set; } = new RabbitOptions();
    public FileSystemOptions? FileSystemOptions { get; set; } = new FileSystemOptions();
    public RDMPOptions? RDMPOptions { get; set; } = new RDMPOptions();
    public MongoDatabases? MongoDatabases { get; set; } = new MongoDatabases();
    public DicomRelationalMapperOptions? DicomRelationalMapperOptions { get; set; } = new DicomRelationalMapperOptions();
    public UpdateValuesOptions? UpdateValuesOptions { get; set; } = new UpdateValuesOptions();
    public CohortExtractorOptions? CohortExtractorOptions { get; set; } = new CohortExtractorOptions();
    public CohortPackagerOptions? CohortPackagerOptions { get; set; } = new CohortPackagerOptions();
    public DicomReprocessorOptions? DicomReprocessorOptions { get; set; } = new DicomReprocessorOptions();
    public DicomTagReaderOptions? DicomTagReaderOptions { get; set; } = new DicomTagReaderOptions();
    public FileCopierOptions? FileCopierOptions { get; set; } = new FileCopierOptions();
    public IdentifierMapperOptions? IdentifierMapperOptions { get; set; } = new IdentifierMapperOptions();
    public MongoDbPopulatorOptions? MongoDbPopulatorOptions { get; set; } = new MongoDbPopulatorOptions();
    public ProcessDirectoryOptions? ProcessDirectoryOptions { get; set; } = new ProcessDirectoryOptions();

    public TriggerUpdatesOptions? TriggerUpdatesOptions { get; set; } = new TriggerUpdatesOptions();

    public IsIdentifiableServiceOptions? IsIdentifiableServiceOptions { get; set; } = new IsIdentifiableServiceOptions();
    public IsIdentifiableDicomFileOptions? IsIdentifiableOptions { get; set; } = new IsIdentifiableDicomFileOptions();

    public ExtractImagesOptions? ExtractImagesOptions { get; set; } = new ExtractImagesOptions();
    public DicomAnonymiserOptions? DicomAnonymiserOptions { get; set; } = new DicomAnonymiserOptions();

    #endregion

    public static string GenerateToString(object o)
    {
        var sb = new StringBuilder();

        foreach (var prop in o.GetType().GetProperties().Where(
            static prop =>
                     !prop.Name.Contains("password", StringComparison.OrdinalIgnoreCase) &&
                     !Attribute.IsDefined(prop, typeof(YamlIgnoreAttribute))
            )
        )
        {
            sb.Append($"{prop.Name}: {prop.GetValue(o)}, ");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        return GenerateToString(this);
    }
}

public class LoggingOptions
{
    public string? LogConfigFile { get; set; }
    public string? LogsRoot { get; set; }
    public bool TraceLogging { get; set; } = true;

    public override string ToString() => GlobalOptions.GenerateToString(this);
}

public class IsIdentifiableServiceOptions : ConsumerOptions
{
    /// <summary>
    /// The full name of the classifier you want to run
    /// </summary>
    public string? ClassifierType { get; set; }

    public ProducerOptions? IsIdentifiableProducerOptions { get; set; }

    public string? DataDirectory { get; set; }
}

public class ProcessDirectoryOptions : IOptions
{
    public ProducerOptions? AccessionDirectoryProducerOptions { get; set; }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

public class MongoDbPopulatorOptions : IOptions
{
    public ConsumerOptions? SeriesQueueConsumerOptions { get; set; }
    public ConsumerOptions? ImageQueueConsumerOptions { get; set; }
    public string? SeriesCollection { get; set; } = "series";
    public string? ImageCollection { get; set; } = "image";

    /// <summary>
    /// Seconds
    /// </summary>
    public int MongoDbFlushTime { get; set; }
    public int FailedWriteLimit { get; set; }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

public class IdentifierMapperOptions : ConsumerOptions, IMappingTableOptions
{
    public ProducerOptions? AnonImagesProducerOptions { get; set; }
    public string? MappingConnectionString { get; set; }
    public DatabaseType MappingDatabaseType { get; set; }
    public int TimeoutInSeconds { get; set; }
    public string? MappingTableSchema { get; set; }
    public string? MappingTableName { get; set; }
    public string? SwapColumnName { get; set; }
    public string? ReplacementColumnName { get; set; }
    public string? SwapperType { get; set; }

    /// <summary>
    /// True - Changes behaviour of swapper host to pick up the PatientID tag using regex from the JSON string directly
    /// rather than deserializing it to <see cref="DicomDataset"/> first.
    /// </summary>
    public bool AllowRegexMatching { get; set; }

    /// <summary>
    /// Optional, if set then your <see cref="SwapperType"/> will be wrapped and it's answers cached in this Redis database.
    /// The Redis database will always be consulted for a known answer first and <see cref="SwapperType"/> used
    /// as a fallback.
    /// See https://stackexchange.github.io/StackExchange.Redis/Configuration.html#basic-configuration-strings for the format.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }

    public DiscoveredTable Discover()
    {
        var server = new DiscoveredServer(MappingConnectionString, MappingDatabaseType);

        if (string.IsNullOrWhiteSpace(MappingTableName))
            throw new ArgumentException($"MappingTableName must be set");

        var database = server.GetCurrentDatabase();
        if (database == null || !database.Exists())
            throw new ArgumentException("Database must be set in cnonection string");

        return database.ExpectTable(MappingTableName, schema: MappingTableSchema);
    }

    public IMappingTableOptions Clone()
    {
        return (IMappingTableOptions)this.MemberwiseClone();
    }
}

public interface IMappingTableOptions : IOptions
{
    string? MappingConnectionString { get; }
    string? MappingTableSchema { get; set; }
    string? MappingTableName { get; set; }
    string? SwapColumnName { get; set; }
    string? ReplacementColumnName { get; set; }
    DatabaseType MappingDatabaseType { get; }
    int TimeoutInSeconds { get; }

    DiscoveredTable Discover();
    IMappingTableOptions Clone();
}

/// <summary>
/// Contains names of the series and image exchanges that serialized image tag data will be written to
/// </summary>
public class DicomTagReaderOptions : ConsumerOptions
{
    /// <summary>
    /// If true, any errors processing a file will cause the entire <see cref="AccessionDirectoryMessage"/> to be NACK'd,
    /// and no messages will be sent related to that directory. If false, file errors will be logged but any valid files
    /// found will be processed as normal
    /// </summary>
    public bool NackIfAnyFileErrors { get; set; }
    public ProducerOptions? ImageProducerOptions { get; set; }
    public ProducerOptions? SeriesProducerOptions { get; set; }
    public string? FileReadOption { get; set; }
    public TagProcessorMode TagProcessorMode { get; set; }
    public int MaxIoThreads { get; set; } = 1;

    public FileReadOption GetReadOption()
    {
        try
        {
            var opt = (FileReadOption)Enum.Parse(typeof(FileReadOption), FileReadOption!);

            if (opt == FellowOakDicom.FileReadOption.SkipLargeTags)
                throw new ApplicationException("SkipLargeTags is disallowed here to ensure data consistency");

            return opt;
        }
        catch (ArgumentNullException ex)
        {
            throw new ArgumentNullException("DicomTagReaderOptions.FileReadOption is not set in the config file", ex);
        }
    }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

public class FileCopierOptions : ConsumerOptions
{
    public ProducerOptions? CopyStatusProducerOptions { get; set; }
    public string? NoVerifyRoutingKey { get; set; }

    public override string ToString() => GlobalOptions.GenerateToString(this);
}

public enum TagProcessorMode
{
    Serial,
    Parallel
}

public class DicomReprocessorOptions : IOptions
{
    public ProcessingMode? ProcessingMode { get; set; }

    public ProducerOptions? ReprocessingProducerOptions { get; set; }

    public TimeSpan SleepTime { get; set; }

    public override string ToString() => GlobalOptions.GenerateToString(this);
}

/// <summary>
/// Represents the different modes of operation of the reprocessor
/// </summary>
public enum ProcessingMode
{
    /// <summary>
    /// Unknown / Undefined. Used for null-checking
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Reprocessing of entire image documents
    /// </summary>
    ImageReprocessing,

    /// <summary>
    /// Promotion of one or more tags
    /// </summary>
    TagPromotion
}

public class CohortPackagerOptions : IOptions
{
    public ConsumerOptions? ExtractRequestInfoOptions { get; set; }
    public ConsumerOptions? FileCollectionInfoOptions { get; set; }
    public ConsumerOptions? NoVerifyStatusOptions { get; set; }
    public ConsumerOptions? VerificationStatusOptions { get; set; }
    public uint JobWatcherTimeoutInSeconds { get; set; }
    public string? NotifierType { get; set; }

    public bool VerificationMessageQueueProcessBatches { get; set; }
    public int? VerificationMessageQueueFlushTimeSeconds { get; set; }

    public static readonly TimeSpan DefaultVerificationMessageQueueFlushTime = TimeSpan.FromSeconds(5);


    /// <summary>
    /// The newline to use when writing extraction report files. Note that a "\r\n" string
    /// in the YAML config will bee automatically escaped to "\\r\\n" in this string.
    /// </summary>
    public string? ReportNewLine { get; set; }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

public class CohortExtractorOptions : ConsumerOptions
{
    private string? _auditorType;

    /// <summary>
    /// The Type of a class implementing IAuditExtractions which is responsible for auditing the extraction process.  If null then no auditing happens
    /// </summary>
    public string AuditorType
    {
        get => string.IsNullOrWhiteSpace(_auditorType)
            ? "SmiServices.Microservices.CohortExtractor.Audit.NullAuditExtractions"
            : _auditorType;
        set => _auditorType = value;
    }

    /// <summary>
    /// The Type of a class implementing IExtractionRequestFulfiller which is responsible for mapping requested image identifiers to image file paths.  Mandatory
    /// </summary>
    public string? RequestFulfillerType { get; set; }

    /// <summary>
    /// The Type of a class implementing IProjectPathResolver which is responsible for deciding the folder hierarchy to output into
    /// </summary>
    public string? ProjectPathResolverType { get; set; }

    /// <summary>
    /// Controls how modalities are matched to Catalogues.  Must contain a single capture group which
    /// returns a modality code (e.g. CT) when applies to a Catalogue name.  E.g. ^([A-Z]+)_.*$ would result
    /// in Modalities being routed based on the start of the table name e.g. CT => CT_MyTable and MR=> MR_MyTable
    /// </summary>
    public string? ModalityRoutingRegex { get; set; }

    /// <summary>
    /// The Type of a class implementing IRejector which is responsible for deciding individual records/images are not extractable (after fetching from database)
    /// </summary>
    public string? RejectorType { get; set; }

    /// <summary>
    /// Modality specific rejection rules that can either override the <see cref="RejectorType"/> for specific Modalities or be applied in addition
    /// </summary>
    public ModalitySpecificRejectorOptions[]? ModalitySpecificRejectors { get; set; }

    /// <summary>
    /// Path to the rules file to use for the <see cref="DynamicRejector"/>
    /// </summary>
    public string? DynamicRulesPath { get; set; } = "DynamicRules.txt";

    public bool AllCatalogues { get; private set; }
    public List<int> OnlyCatalogues { get; private set; } = null!;

    /// <summary>
    /// Optional list of datasets which contain information about when NOT to extract an image.  This should be a manually curated blacklist - not just general rules (for those use <see cref="RejectorType"/>). Referenced datasets must include one or more of the UID columns (StudyInstanceUID, SeriesInstanceUID or SOPInstanceUID)
    /// </summary>
    public List<int>? Blacklists { get; set; }

    public string? ExtractAnonRoutingKey { get; set; }
    public string? ExtractIdentRoutingKey { get; set; }

    public ProducerOptions? ExtractFilesProducerOptions { get; set; }
    public ProducerOptions? ExtractFilesInfoProducerOptions { get; set; }

    /// <summary>
    /// ID(s) of ColumnInfo that contains a list of values which should not have data extracted for them.  e.g. opt out.  The name of the column referenced must match a column in the extraction table
    /// </summary>
    public List<int>? RejectColumnInfos { get; set; }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }

    public void Validate()
    {
        if (ModalitySpecificRejectors != null && ModalitySpecificRejectors.Length != 0 && string.IsNullOrWhiteSpace(ModalityRoutingRegex))
        {
            throw new Exception("ModalitySpecificRejectors requires providing a ModalityRoutingRegex");
        }

        if (string.IsNullOrEmpty(RequestFulfillerType))
            throw new Exception("No RequestFulfillerType set on CohortExtractorOptions.  This must be set to a class implementing IExtractionRequestFulfiller");

    }
}

public class UpdateValuesOptions : ConsumerOptions
{
    /// <summary>
    /// Number of seconds the updater will wait when running a single value UPDATE on the live table e.g. ECHI A needs to be replaced with ECHI B
    /// </summary>
    public int UpdateTimeout { get; set; } = 5000;

    /// <summary>
    /// IDs of TableInfos that should be updated
    /// </summary>
    public int[] TableInfosToUpdate { get; set; } = [];

}

public class TriggerUpdatesOptions : ProducerOptions
{
    /// <summary>
    /// The number of seconds database commands should be allowed to execute for before timing out.
    /// </summary>
    public int CommandTimeoutInSeconds = 500;
}

public class DicomRelationalMapperOptions : ConsumerOptions
{
    /// <summary>
    /// The ID of the LoadMetadata load configuration to run.  A load configuration is a sequence of steps to modify/clean data such that it is loadable into the final live
    /// tables.  The LoadMetadata is designed to be modified through the RMDP user interface and is persisted in the LoadMetadata table (and other related tables) of the 
    /// RDMP platform database.
    /// </summary>
    public int LoadMetadataId { get; set; }
    public Guid Guid { get; set; }
    public string? DatabaseNamerType { get; set; }
    public int MinimumBatchSize { get; set; }
    public bool UseInsertIntoForRAWMigration { get; set; }
    public int RetryOnFailureCount { get; set; }
    public int RetryDelayInSeconds { get; set; }
    public int MaximumRunDelayInSeconds { get; set; }

    /// <summary>
    /// True to run <see cref="PreExecutionChecker"/> before the data load accepting all proposed fixes (e.g. dropping RAW)
    /// <para>Default is false</para>
    /// </summary>
    public bool RunChecks { get; set; }


    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

public class ExtractImagesOptions : IOptions
{
    public const int MaxIdentifiersPerMessageDefault = 1000;

    /// <summary>
    /// The maximum number of identifiers in each <see cref="ExtractionRequestMessage"/>
    /// </summary>
    public int MaxIdentifiersPerMessage { get; set; } = MaxIdentifiersPerMessageDefault;

    /// <summary>
    /// Options for publishing <see cref="ExtractionRequestMessage"/>s
    /// </summary>
    public ProducerOptions? ExtractionRequestProducerOptions { get; set; }

    /// <summary>
    /// Options for publishing <see cref="ExtractionRequestInfoMessage"/>s
    /// </summary>
    public ProducerOptions? ExtractionRequestInfoProducerOptions { get; set; }

    /// <summary>
    /// The list of possible <see cref="ExtractionKey"/>s that are allowed for use
    /// </summary>
    public ExtractionKey[]? AllowedExtractionKeys { get; set; }

    public override string ToString() => GlobalOptions.GenerateToString(this);
}

public class DicomAnonymiserOptions : IOptions
{
    public string? AnonymiserType { get; set; }
    public ConsumerOptions? AnonFileConsumerOptions { get; set; }
    public ProducerOptions? ExtractFileStatusProducerOptions { get; set; }
    public string? RoutingKeySuccess { get; set; }
    public string? RoutingKeyFailure { get; set; }
    public bool FailIfSourceWriteable { get; set; } = true;
    public string? VirtualEnvPath { get; set; }
    public string? DicomPixelAnonPath { get; set; }
    public string? SmiServicesPath { get; set; }
    public string? CtpAnonCliJar { get; set; }
    public string? CtpAllowlistScript { get; set; }
    public string? SRAnonymiserToolPath { get; set; }

    public override string ToString() => GlobalOptions.GenerateToString(this);
}

public class MongoDatabases : IOptions
{
    public MongoDbOptions? DicomStoreOptions { get; set; }

    public MongoDbOptions? ExtractionStoreOptions { get; set; }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

public class MongoDbOptions : IOptions
{
    public string? HostName { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 27017;
    /// <summary>
    /// UserName for authentication. If empty, authentication will be skipped.
    /// </summary>
    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? DatabaseName { get; set; }

    public bool AreValid(bool skipAuthentication)
    {
        return (skipAuthentication || UserName != null)
               && Port > 0
               && !string.IsNullOrWhiteSpace(HostName)
               && !string.IsNullOrWhiteSpace(DatabaseName);
    }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

/// <summary>
/// Describes the location of the Microsoft Sql Server RDMP platform databases which keep track of load configurations, available datasets (tables) etc
/// </summary>
public class RDMPOptions : IOptions
{
    public string? CatalogueConnectionString { get; set; }
    public string? DataExportConnectionString { get; set; }

    /// <summary>
    /// Alternative to connection strings for if you have RDMP running with a YAML file system backend.
    /// If specified then this will override the connection strings
    /// </summary>
    public string? YamlDir { get; set; }

    public IRDMPPlatformRepositoryServiceLocator GetRepositoryProvider()
    {
        CatalogueRepository.SuppressHelpLoading = true;

        // if using file system backend for RDMP create that repo instead
        if (!string.IsNullOrWhiteSpace(YamlDir))
        {
            return new RepositoryProvider(new YamlRepository(new System.IO.DirectoryInfo(YamlDir)));
        }

        // We are using database backend for RDMP (i.e. Sql Server)
        var cata = new SqlConnectionStringBuilder(CatalogueConnectionString);
        var dx = new SqlConnectionStringBuilder(DataExportConnectionString);

        return new LinkedRepositoryProvider(cata.ConnectionString, dx.ConnectionString);
    }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

/// <summary>
/// Describes the root location of all images, file names should be expressed as relative paths (relative to this root).
/// </summary>
public class FileSystemOptions : IOptions
{
    public string? DicomSearchPattern { get; set; } = "*.dcm";

    private string? _fileSystemRoot;
    private string? _extractRoot;
    private string? _extractionPoolRoot;

    public string? FileSystemRoot
    {
        get => _fileSystemRoot;
        set => _fileSystemRoot = value?.Length > 1 ? value.TrimEnd('/', '\\') : value;
    }

    public string? ExtractRoot
    {
        get => _extractRoot;
        set => _extractRoot = value?.Length > 1 ? value.TrimEnd('/', '\\') : value;
    }

    public string? ExtractionPoolRoot
    {
        get => _extractionPoolRoot;
        set => _extractionPoolRoot = value?.Length > 1 ? value.TrimEnd('/', '\\') : value;
    }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}

/// <summary>
/// Describes the location of the rabbit server for sending messages to
/// </summary>
public class RabbitOptions : IOptions
{
    private IConnection CreateConnection() =>
        new ConnectionFactory
        {
            HostName = RabbitMqHostName,
            Port = RabbitMqHostPort,
            VirtualHost = RabbitMqVirtualHost,
            UserName = RabbitMqUserName,
            Password = RabbitMqPassword
        }.CreateConnection();

    private readonly Lazy<IConnection> _connectionCache;

    public RabbitOptions()
    {
        _connectionCache = new Lazy<IConnection>(CreateConnection);
    }

    [YamlIgnore]
    public IConnection Connection => _connectionCache.Value;
    public string RabbitMqHostName { get; set; } = "localhost";
    public int RabbitMqHostPort { get; set; } = 5672;
    public string? RabbitMqVirtualHost { get; set; } = "/";
    public string? RabbitMqUserName { get; set; } = "guest";
    public string? RabbitMqPassword { get; set; } = "guest";
    public string? FatalLoggingExchange { get; set; }
    public string? RabbitMqControlExchangeName { get; set; }

    public override string ToString()
    {
        return GlobalOptions.GenerateToString(this);
    }
}
