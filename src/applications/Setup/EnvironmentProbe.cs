using Microservices.CohortExtractor.Execution;
using Microservices.CohortPackager.Execution;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.DicomAnonymiser;
using Microservices.DicomRelationalMapper.Execution;
using Microservices.DicomTagReader.Execution;
using Microservices.IdentifierMapper.Execution;
using Microservices.IsIdentifiable.Service;
using Microservices.MongoDBPopulator.Execution;
using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Setup
{

    internal class EnvironmentProbe
    {        
        public CheckEventArgs DeserializeYaml { get; }

        public CheckEventArgs? RabbitMq { get; private set; }
        public  GlobalOptions? Options { get; }
        public CheckEventArgs? Rdmp { get; private set; }

        public CheckEventArgs? CohortExtractor { get; private set; }
        public CheckEventArgs? MongoDb { get; private set; }
        public CheckEventArgs? DicomTagReader { get; private set; }
        public CheckEventArgs? MongoDbPopulator { get; private set; }

        public const string CheckInfrastructureTaskName = "Checking Infrastructure";
        public const string CheckMicroservicesTaskName = "Checking Microservices";

        internal int GetExitCode()
        {
            // get all things we can check
            foreach(var prop in typeof(EnvironmentProbe).GetProperties())
            {
                var val = prop.GetValue(this);
                
                // did any checks run
                if(val is CheckEventArgs cea)
                {
                    // that failed
                    if (cea.Result == CheckResult.Fail)
                    {
                        // something failed so exit code is failure (non zero)
                        return 100;
                    }   
                }
            }

            return 0;
        }

        public CheckEventArgs? IdentifierMapper { get; private set; }
        public CheckEventArgs? DicomRelationalMapper { get; private set; }
        public CheckEventArgs? DicomAnonymiser { get; private set; }
        public CheckEventArgs? IsIdentifiable { get; private set; }
        public CheckEventArgs? CohortPackager { get; private set; }

        public EnvironmentProbe(string? yamlFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(yamlFile))
                    throw new Exception("You have not yet entered a path for yaml file");

                Options = new GlobalOptionsFactory().Load("Setup", yamlFile);
                DeserializeYaml = new CheckEventArgs("Deserialized Yaml File",CheckResult.Success);
            }
            catch (Exception ex)
            {
                DeserializeYaml = new CheckEventArgs("Failed to Deserialize Yaml File",CheckResult.Fail, ex);
            }
        }

        internal void CheckInfrastructure(IDataLoadEventListener? listener = null)
        {
            var sw = Stopwatch.StartNew();
            int max = 3;
            int current = 0;
            var task = CheckInfrastructureTaskName;

            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(current, ProgressType.Records, max), sw.Elapsed));

            ProbeRabbitMq();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeMongoDb();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeRdmp();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));
        }
        internal void CheckMicroservices(IDataLoadEventListener? listener = null)
        {
            var sw = Stopwatch.StartNew();
            int max = 8;
            int current = 0;
            string task = CheckMicroservicesTaskName;

            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(current, ProgressType.Records, max), sw.Elapsed));

            ProbeDicomTagReader();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeMongoDbPopulator();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeIdentifierMapper();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeDicomRelationalMapper();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeCohortExtractor();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeDicomAnonymiser();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeIsIdentifiable();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));

            ProbeCohortPackager();
            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));
        }

        public void ProbeRdmp()
        {
            // clear any old records
            Rdmp = null;

            try
            {
                if (Options == null)
                    return;

                if (Options.RDMPOptions == null || string.IsNullOrEmpty(Options.RDMPOptions.CatalogueConnectionString))
                {
                    throw new Exception("No RDMP connection settings specified");
                }

                var provider = new LinkedRepositoryProvider(Options.RDMPOptions.CatalogueConnectionString,
                    Options.RDMPOptions.DataExportConnectionString);

                var startup = new Startup(new EnvironmentInfo(), provider);
                
                bool failed = false;
                var sb = new StringBuilder();
                var exceptions = new List<Exception>();

                startup.DatabaseFound += (s, e) => {
                    failed = !failed && e.Status != RDMPPlatformDatabaseStatus.Healthy || e.Exception != null;
                    sb.AppendLine(e.Patcher.Name + " " + e.Status);

                    if (e.Exception != null)
                    {
                        sb.AppendLine(ExceptionHelper.ExceptionToListOfInnerMessages(e.Exception));
                        exceptions.Add(e.Exception);
                    }
                };

                startup.DoStartup(new ThrowImmediatelyCheckNotifier() { WriteToConsole = false }) ;

                Rdmp = new CheckEventArgs(sb.ToString(), failed ? CheckResult.Fail : CheckResult.Success);
            }
            catch (Exception ex)
            {
                Rdmp = new CheckEventArgs("Failed to connect to RDMP", CheckResult.Fail, ex);
            }
        }

        public void ProbeRabbitMq()
        {
            // clear any old records
            RabbitMq = null;

            if (Options == null)
                return;

            try
            {
                var factory = ConnectionFactoryExtensions.CreateConnectionFactory(Options.RabbitOptions);
                var adapter = new RabbitMqAdapter(factory, "setup");

                RabbitMq = new CheckEventArgs("Connected to RabbitMq", CheckResult.Success);
            }
            catch (Exception ex)
            {
                RabbitMq = new CheckEventArgs("Failed to connect to RabbitMq", CheckResult.Fail, ex);
            }
        }

        public void ProbeMongoDb()
        {
            MongoDb = null;
            if (Options == null)
                return;

            try
            {
                if (Options.MongoDatabases == null)
                    return;

                // this opens connection to the server and tests for collection existing                
                new MongoDbAdapter("Setup", Options.MongoDatabases.DicomStoreOptions,
                         Options.MongoDbPopulatorOptions.ImageCollection);


                MongoDbOptions mongoDbOptions = Options.MongoDatabases.ExtractionStoreOptions;
                var jobStore = new MongoExtractJobStore(
                    MongoClientHelpers.GetMongoClient(mongoDbOptions, "Setup"),
                    mongoDbOptions.DatabaseName,new Smi.Common.Helpers.DateTimeProvider()                    
                );

                MongoDb = new CheckEventArgs("MongoDb Checking Succeeded", CheckResult.Success);
            }
            catch (Exception ex)
            {
                MongoDb = new CheckEventArgs("MongoDb Checking Failed", CheckResult.Fail, ex);
            }
        }

        public void ProbeCohortExtractor()
        {
            Probe(nameof(CohortExtractorHost),(o) => new CohortExtractorHost(o, null, null),(p)=>CohortExtractor = p);
        }

        private void ProbeDicomAnonymiser()
        {
            Probe(nameof(DicomAnonymiserHost), (o) => new DicomAnonymiserHost(o), (p) => DicomAnonymiser = p);
        }
        private void ProbeIsIdentifiable()
        {
            Probe(nameof(IsIdentifiableHost), (o) => new IsIdentifiableHost(o), (p) => IsIdentifiable = p);
        }

        private void ProbeCohortPackager()
        {
            Probe(nameof(CohortPackagerHost), (o) => new CohortPackagerHost(o), (p) => CohortPackager = p);
        }

        public void ProbeDicomRelationalMapper()
        {
            Probe(nameof(DicomRelationalMapperHost), (o) => new DicomRelationalMapperHost(o), (p) => DicomRelationalMapper = p);
        }

        private void ProbeIdentifierMapper()
        {
            Probe(nameof(IdentifierMapperHost), (o) => new IdentifierMapperHost(o), (p) => IdentifierMapper = p);
        }

        private void ProbeMongoDbPopulator()
        {
            Probe(nameof(MongoDbPopulatorHost), (o) => new MongoDbPopulatorHost(o), (p) => MongoDbPopulator = p);
        }

        private void ProbeDicomTagReader()
        {
            Probe(nameof(DicomTagReaderHost), (o) => new DicomTagReaderHost(o), (p) => DicomTagReader = p);
        }
        private void Probe(string probeName, Func<GlobalOptions,MicroserviceHost> hostConstructor, Action<CheckEventArgs?> storeResult)
        {
            // clear old results
            storeResult(null);

            if (Options == null)
                return;

            try
            {
                var host = hostConstructor(Options);

                host.StartAuxConnections();
                host.Start();

                host.Stop("Finished Testing");

                storeResult(new CheckEventArgs($"{probeName} Succeeded", CheckResult.Success));
            }
            catch (Exception ex)
            {
                storeResult(new CheckEventArgs($"{probeName} Failed", CheckResult.Fail, ex));
            }
        }

    }
}
