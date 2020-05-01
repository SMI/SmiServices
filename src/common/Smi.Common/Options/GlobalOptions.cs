
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;
using Dicom;
using FAnsi.Discovery;
using JetBrains.Annotations;
using Rdmp.Core.DataLoad.Engine.Checks.Checkers;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using Smi.Common.Messages;
using YamlDotNet.Serialization;
using DatabaseType = FAnsi.DatabaseType;


namespace Smi.Common.Options
{
    public class GlobalOptions
    {
        public static GlobalOptions Load(string environment = "default", string currentDirectory = null)
        {
            IDeserializer deserializer = new DeserializerBuilder()
                                    .WithObjectFactory(GetGlobalOption)
                                    .IgnoreUnmatchedProperties()
                                    .Build();

            currentDirectory = currentDirectory ?? Environment.CurrentDirectory;

            // Make sure environment ends with yaml 
            if (!(environment.EndsWith(".yaml") || environment.EndsWith(".yml")))
                environment += ".yaml";

            // If the yaml file doesn't exist and the path is relative, try looking in currentDirectory instead
            if (!File.Exists(environment) && !Path.IsPathRooted(environment))
                environment = Path.Combine(currentDirectory, environment);

            string text = File.ReadAllText(environment);

            var globals = deserializer.Deserialize<GlobalOptions>(new StringReader(text));
            globals.CurrentDirectory = currentDirectory;
            globals.MicroserviceOptions = new MicroserviceOptions();

            return globals;
        }

        public static GlobalOptions Load(CliOptions cliOptions)
        {
            GlobalOptions globalOptions = Load(cliOptions.YamlFile);
            globalOptions.MicroserviceOptions = new MicroserviceOptions(cliOptions);

            return globalOptions;
        }


        private static object GetGlobalOption(Type arg)
        {
            return arg == typeof(GlobalOptions) ?
                new GlobalOptions() :
                Activator.CreateInstance(arg);
        }

        private GlobalOptions() { }

        #region AllOptions

        /// <summary>
        /// The directory where local files should be when CopyAlways etc.  
        /// 
        /// <para>Should be either Environment.CurrentDirectory or TestContext.CurrentContext.TestDirectory</para>
        /// </summary>
        public string CurrentDirectory;

        public MicroserviceOptions MicroserviceOptions { get; set; }
        public RabbitOptions RabbitOptions { get; set; }
        public FileSystemOptions FileSystemOptions { get; set; }
        public RDMPOptions RDMPOptions { get; set; }
        public MongoDatabases MongoDatabases { get; set; }
        public DicomRelationalMapperOptions DicomRelationalMapperOptions { get; set; }
        public CohortExtractorOptions CohortExtractorOptions { get; set; }
        public CohortPackagerOptions CohortPackagerOptions { get; set; }
        public DicomReprocessorOptions DicomReprocessorOptions { get; set; }
        public DicomTagReaderOptions DicomTagReaderOptions { get; set; }
        public IdentifierMapperOptions IdentifierMapperOptions { get; set; }
        public MongoDbPopulatorOptions MongoDbPopulatorOptions { get; set; }
        public ProcessDirectoryOptions ProcessDirectoryOptions { get; set; }
        public DeadLetterReprocessorOptions DeadLetterReprocessorOptions { get; set; }

        public IsIdentifiableOptions IsIdentifiableOptions { get; set; }

        #endregion

        public static string GenerateToString(object o)
        {
            var sb = new StringBuilder();

            foreach (PropertyInfo prop in o.GetType().GetProperties())
            {
                if (!prop.Name.ToLower().Contains("password"))
                    sb.Append(string.Format("{0}: {1}, ", prop.Name, prop.GetValue(o)));
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return GenerateToString(this);
        }
    }
    
    [UsedImplicitly]
    public class IsIdentifiableOptions : ConsumerOptions
    {
        /// <summary>
        /// The full name of the classifier you want to run
        /// </summary>
        public string ClassifierType { get; set; }

        /// <summary>
        /// The root location in which subfolders must exist containing all data files required by any classifiers
        /// (typically 1 sub-directory per classifier)
        /// </summary>
        public string DataDirectory { get; set; }

        public ProducerOptions IsIdentifiableProducerOptions {get; set;}
    }

    [UsedImplicitly]
    public class MicroserviceOptions
    {
        public bool TraceLogging { get; set; } = true;

        public MicroserviceOptions() { }

        public MicroserviceOptions(CliOptions cliOptions)
        {
            TraceLogging = cliOptions.TraceLogging;
        }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }
    }

    [UsedImplicitly]
    public class ProcessDirectoryOptions
    {
        public ProducerOptions AccessionDirectoryProducerOptions { get; set; }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }
    }

    [UsedImplicitly]
    public class MongoDbPopulatorOptions
    {
        public ConsumerOptions SeriesQueueConsumerOptions { get; set; }
        public ConsumerOptions ImageQueueConsumerOptions { get; set; }
        public string SeriesCollection { get; set; } = "series";
        public string ImageCollection { get; set; } = "image";

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

    [UsedImplicitly]
    public class IdentifierMapperOptions : ConsumerOptions, IMappingTableOptions
    {
        public ProducerOptions AnonImagesProducerOptions { get; set; }
        public string MappingConnectionString { get; set; }
        public DatabaseType MappingDatabaseType { get; set; }
        public int TimeoutInSeconds { get; set; }
        public string MappingTableName { get; set; }
        public string SwapColumnName { get; set; }
        public string ReplacementColumnName { get; set; }
        public string SwapperType { get; set; }

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
        public string RedisConnectionString { get; set; }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }

        public DiscoveredTable Discover()
        {
            var server = new DiscoveredServer(MappingConnectionString, MappingDatabaseType);

            var idx = MappingTableName.LastIndexOf('.');
            var tableNameUnqualified = MappingTableName.Substring(idx + 1);

            idx = MappingTableName.IndexOf('.');
            if (idx == -1)
                throw new ArgumentException($"MappingTableName did not contain the database/user section:'{MappingTableName}'");

            var databaseName = server.GetQuerySyntaxHelper().GetRuntimeName(MappingTableName.Substring(0, idx));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException($"Could not get database/username from MappingTableName {MappingTableName}");

            return server.ExpectDatabase(databaseName).ExpectTable(tableNameUnqualified);
        }

        public IMappingTableOptions Clone()
        {
            return (IMappingTableOptions)this.MemberwiseClone();
        }
    }

    public interface IMappingTableOptions
    {
        string MappingConnectionString { get; }
        string MappingTableName { get; set; }
        string SwapColumnName { get; set; }
        string ReplacementColumnName { get; set; }
        DatabaseType MappingDatabaseType { get; }
        int TimeoutInSeconds { get; }

        DiscoveredTable Discover();
        IMappingTableOptions Clone();
    }

    /// <summary>
    /// Contains names of the series and image exchanges that serialized image tag data will be written to
    /// </summary>
    [UsedImplicitly]
    public class DicomTagReaderOptions : ConsumerOptions
    {
        /// <summary>
        /// If true, any errors processing a file will cause the entire <see cref="AccessionDirectoryMessage"/> to be NACK'd,
        /// and no messages will be sent related to that directory. If false, file errors will be logged but any valid files
        /// found will be processed as normal
        /// </summary>
        public bool NackIfAnyFileErrors { get; set; }
        public ProducerOptions ImageProducerOptions { get; set; }
        public ProducerOptions SeriesProducerOptions { get; set; }
        public string FileReadOption { get; set; }
        public TagProcessorMode TagProcessorMode { get; set; }
        public int MaxIoThreads { get; set; } = 1;

        public FileReadOption GetReadOption()
        {
            try
            {
                var opt = (FileReadOption)Enum.Parse(typeof(FileReadOption), FileReadOption);

                if (opt == Dicom.FileReadOption.SkipLargeTags)
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

    public enum TagProcessorMode
    {
        Serial,
        Parallel
    }

    [UsedImplicitly]
    public class DicomReprocessorOptions
    {
        public ProcessingMode ProcessingMode { get; set; }

        public ProducerOptions ReprocessingProducerOptions { get; set; }

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

    [UsedImplicitly]
    public class CohortPackagerOptions
    {
        public ConsumerOptions ExtractRequestInfoOptions { get; set; }
        public ConsumerOptions FileCollectionInfoOptions { get; set; }
        public ConsumerOptions AnonFailedOptions { get; set; }
        public ConsumerOptions VerificationStatusOptions { get; set; }
        public uint JobWatcherTimeoutInSeconds { get; set; }
        public string ReporterType { get; set; }
        public string NotifierType { get; set; }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }
    }

    [UsedImplicitly]
    public class CohortExtractorOptions : ConsumerOptions
    {
        private string _auditorType;

        /// <summary>
        /// The Type of a class implementing IAuditExtractions which is responsible for auditing the extraction process.  If null then no auditing happens
        /// </summary>
        public string AuditorType
        {
            get => string.IsNullOrWhiteSpace(_auditorType)
                ? "Microservices.CohortExtractor.Audit.NullAuditExtractions"
                : _auditorType;
            set => _auditorType = value;
        }

        /// <summary>
        /// The Type of a class implementing IExtractionRequestFulfiller which is responsible for mapping requested image identifiers to image file paths.  Mandatory
        /// </summary>
        public string RequestFulfillerType { get; set; }
        
        /// <summary>
        /// The Type of a class implementing IProjectPathResolver which is responsible for deciding the folder hierarchy to output into
        /// </summary>
        public string ProjectPathResolverType { get; set; }

        /// <summary>
        /// Controls how modalities are matched to Catalogues.  Must contain a single capture group which
        /// returns a modality code (e.g. CT) when applies to a Catalogue name.  E.g. ^([A-Z]+)_.*$ would result
        /// in Modalities being routed based on the start of the table name e.g. CT => CT_MyTable and MR=> MR_MyTable
        /// </summary>
        public string ModalityRoutingRegex { get; set; } = "^([A-Z]+)_.*$";

        /// <summary>
        /// The Type of a class implementing IRejector which is responsible for deciding individual records/images are not extractable (after fetching from database)
        /// </summary>
        public string RejectorType { get; set; }

        public bool AllCatalogues { get; private set; }
        public List<int> OnlyCatalogues { get; private set; }

        /// <summary>
        /// Optional list of datasets which contain information about when NOT to extract an image.  This should be a manually curated blacklist - not just general rules (for those use <see cref="RejectorType"/>). Referenced datasets must include one or more of the UID columns (StudyInstanceUID, SeriesInstanceUID or SOPInstanceUID)
        /// </summary>
        public List<int> Blacklists { get; set; }

        public ProducerOptions ExtractFilesProducerOptions { get; set; }
        public ProducerOptions ExtractFilesInfoProducerOptions { get; set; }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(RequestFulfillerType))
                throw new Exception("No RequestFulfillerType set on CohortExtractorOptions.  This must be set to a class implementing IExtractionRequestFulfiller");

        }
    }

    [UsedImplicitly]
    public class DicomRelationalMapperOptions : ConsumerOptions
    {
        /// <summary>
        /// The ID of the LoadMetadata load configuration to run.  A load configuration is a sequence of steps to modify/clean data such that it is loadable into the final live
        /// tables.  The LoadMetadata is designed to be modified through the RMDP user interface and is persisted in the LoadMetadata table (and other related tables) of the 
        /// RDMP platform database.
        /// </summary>
        public int LoadMetadataId { get; set; }
        public Guid Guid { get; set; }
        public string DatabaseNamerType { get; set; }
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

    [UsedImplicitly]
    public class DeadLetterReprocessorOptions
    {
        public ConsumerOptions DeadLetterConsumerOptions { get; set; }

        public int MaxRetryLimit { get; set; }

        public int DefaultRetryAfter { get; set; }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }
    }

    [UsedImplicitly]
    public class MongoDatabases
    {
        public MongoDbOptions DicomStoreOptions { get; set; }

        public MongoDbOptions ExtractionStoreOptions { get; set; }

        public MongoDbOptions DeadLetterStoreOptions { get; set; }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }
    }

    [UsedImplicitly]
    public class MongoDbOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 27017;
        /// <summary>
        /// UserName for authentication. If empty, authentication will be skipped.
        /// </summary>
        public string UserName { get; set; }
        public string DatabaseName { get; set; }

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
    [UsedImplicitly]
    public class RDMPOptions
    {
        public string CatalogueConnectionString { get; set; }
        public string DataExportConnectionString { get; set; }

        public IRDMPPlatformRepositoryServiceLocator GetRepositoryProvider()
        {
            CatalogueRepository.SuppressHelpLoading = true;

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
    [UsedImplicitly]
    public class FileSystemOptions
    {
        /// <summary>
        /// If set, services will require that the "SMI_LOGS_ROOT" environment variable is set and points to a valid directory.
        /// This helps to ensure that we log to a central location on the production system.
        /// </summary>
        public bool ForceSmiLogsRoot { get; set; } = false;

        public string LogConfigFile { get; set; }

        public string DicomSearchPattern { get; set; } = "*.dcm";

        private string _fileSystemRoot;
        private string _extractRoot;

        public string FileSystemRoot
        {
            get { return _fileSystemRoot; }
            set { _fileSystemRoot = value.TrimEnd('/', '\\'); }
        }

        public string ExtractRoot
        {
            get { return _extractRoot; }
            [UsedImplicitly]
            set { _extractRoot = value.TrimEnd('/', '\\'); }
        }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }
    }

    /// <summary>
    /// Describes the location of the rabbit server for sending messages to
    /// </summary>
    public class RabbitOptions
    {
        public string RabbitMqHostName { get; set; }
        public int RabbitMqHostPort { get; set; }
        public string RabbitMqVirtualHost { get; set; }
        public string RabbitMqUserName { get; set; }
        public string RabbitMqPassword { get; set; }
        public string FatalLoggingExchange { get; set; }
        public string RabbitMqControlExchangeName { get; set; }
        public bool ThreadReceivers { get; set; }

        public override string ToString()
        {
            return GlobalOptions.GenerateToString(this);
        }
    }
}
