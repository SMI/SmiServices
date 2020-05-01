
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.CohortExtractor.Messaging;
using NLog;
using RabbitMQ.Client.Exceptions;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using ReusableLibraryCode.Checks;
using Smi.Common.Execution;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Microservices.CohortExtractor.Execution
{

    /// <summary>
    /// Microservice for handling requests to extract images for specific UIDs.  UIDs arrive as <see cref="ExtractionRequestMessage"/> instances.  These
    /// requests are fed to an <see cref="IExtractionRequestFulfiller"/> for database lookup and the resulting file matches sent for extraction in the
    /// form of <see cref="ExtractFileMessage"/>.
    ///
    /// <para>This microservice may filter which images are sent for extraction e.g. only PRIMARY/ORIGINAL images (depending on system configuration).</para>
    /// </summary>
    public class CohortExtractorHost : MicroserviceHost
    {
        /// <summary>
        /// RabbitMQ queue subscriber responsible for dequeuing messages and feeding them to the <see cref="IExtractionRequestFulfiller"/>
        /// </summary>
        public ExtractionRequestQueueConsumer Consumer { get; set; }
        private readonly CohortExtractorOptions _consumerOptions;

        private IAuditExtractions _auditor;
        private IExtractionRequestFulfiller _fulfiller;
        private IProjectPathResolver _pathResolver;
        private IProducerModel _fileMessageProducer;

        /// <summary>
        /// Creates a new instance of the host with the given 
        /// </summary>
        /// <param name="options">Settings for the microservice (location of rabbit, queue names etc)</param>
        /// <param name="auditor">Optional override for the value specified in <see cref="GlobalOptions.CohortExtractorOptions"/></param>
        /// <param name="fulfiller">Optional override for the value specified in <see cref="GlobalOptions.CohortExtractorOptions"/></param>
        /// <param name="loadSmiLogConfig">True to replace any existing <see cref="LogManager.Configuration"/> with the SMI logging configuration (which must exist in the file "Microservices.NLog.config" of the current directory)</param>
        public CohortExtractorHost(GlobalOptions options, IAuditExtractions auditor, IExtractionRequestFulfiller fulfiller, bool loadSmiLogConfig = true)
            : base(options, loadSmiLogConfig: loadSmiLogConfig)
        {
            _consumerOptions = options.CohortExtractorOptions;
            _consumerOptions.Validate();

            _auditor = auditor;
            _fulfiller = fulfiller;
        }

        /// <summary>
        /// Starts up service and begins listening with the <see cref="Consumer"/>
        /// </summary>
        public override void Start()
        {
            IRDMPPlatformRepositoryServiceLocator repositoryLocator = Globals.RDMPOptions.GetRepositoryProvider();

            var startup = new Startup(new EnvironmentInfo("netcoreapp2.2"), repositoryLocator);

            var toMemory = new ToMemoryCheckNotifier();
            startup.DoStartup(toMemory);

            foreach (CheckEventArgs args in toMemory.Messages.Where(m => m.Result == CheckResult.Fail))
                Logger.Log(LogLevel.Warn, args.Ex, args.Message);

            _fileMessageProducer = RabbitMqAdapter.SetupProducer(Globals.CohortExtractorOptions.ExtractFilesProducerOptions, isBatch: true);
            IProducerModel fileMessageInfoProducer = RabbitMqAdapter.SetupProducer(Globals.CohortExtractorOptions.ExtractFilesInfoProducerOptions, isBatch: false);

            InitializeExtractionSources(repositoryLocator);

            Consumer = new ExtractionRequestQueueConsumer(_fulfiller, _auditor, _pathResolver, _fileMessageProducer, fileMessageInfoProducer);

            RabbitMqAdapter.StartConsumer(_consumerOptions, Consumer, isSolo: false);
        }

        public override void Stop(string reason)
        {
            if (_fileMessageProducer != null)
                try
                {
                    _fileMessageProducer.WaitForConfirms();
                }
                catch (AlreadyClosedException) { /* Ignored */ }

            base.Stop(reason);
        }

        /// <summary>
        /// Connects to RDMP platform databases to retrieve extractable catalogues and initializes the <see cref="IExtractionRequestFulfiller"/> (if none
        /// was specified in the constructor override).
        /// </summary>
        /// <param name="repositoryLocator"></param>
        private void InitializeExtractionSources(IRDMPPlatformRepositoryServiceLocator repositoryLocator)
        {
            // Get all extractable catalogues
            ICatalogue[] catalogues = repositoryLocator
                .DataExportRepository
                .GetAllObjects<ExtractableDataSet>()
                .Select(eds => eds.Catalogue)
                .ToArray();

            if (_auditor == null)
                _auditor = ObjectFactory.CreateInstance<IAuditExtractions>(_consumerOptions.AuditorType, typeof(IAuditExtractions).Assembly,
                    repositoryLocator);

            if (_auditor == null)
                throw new Exception("No IAuditExtractions set");

            if (!_consumerOptions.AllCatalogues)
                catalogues = catalogues.Where(c => _consumerOptions.OnlyCatalogues.Contains(c.ID)).ToArray();

            if (_fulfiller == null)
                _fulfiller = ObjectFactory.CreateInstance<IExtractionRequestFulfiller>(_consumerOptions.RequestFulfillerType,
                    typeof(IExtractionRequestFulfiller).Assembly, new object[] { catalogues });

            if (_fulfiller == null)
                throw new Exception("No IExtractionRequestFulfiller set");

            if(!string.IsNullOrWhiteSpace(_consumerOptions.ModalityRoutingRegex))
                _fulfiller.ModalityRoutingRegex = new Regex(_consumerOptions.ModalityRoutingRegex);

            if(!string.IsNullOrWhiteSpace(_consumerOptions.RejectorType))
                _fulfiller.Rejectors.Add(ObjectFactory.CreateInstance<IRejector>(_consumerOptions.RejectorType,typeof(IRejector).Assembly));

            if(_consumerOptions.Blacklists != null)
                foreach (int id in _consumerOptions.Blacklists)
                {
                    var cata = repositoryLocator.CatalogueRepository.GetObjectByID<Catalogue>(id);
                    var rejector = new BlacklistRejector(cata);
                    _fulfiller.Rejectors.Add(rejector);
                }

            if(!string.IsNullOrWhiteSpace(_consumerOptions.ProjectPathResolverType))
                _pathResolver = ObjectFactory.CreateInstance<IProjectPathResolver>(_consumerOptions.ProjectPathResolverType, typeof(IProjectPathResolver).Assembly,repositoryLocator);
            else
                _pathResolver = new DefaultProjectPathResolver();
        }
    }
}