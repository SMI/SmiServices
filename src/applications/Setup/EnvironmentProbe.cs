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
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Smi.Common.Messaging;

namespace Setup
{


    public class Probeable
    {
        public string Name { get; }
        public Func<CheckEventArgs?> Run { get; }
        public string Category { get; }
        public CheckEventArgs? Result { get; set; }

        public Probeable(string name, Func<CheckEventArgs?> run, string category)
        {
            Name = name;
            this.Run = run;
            Category = category;
        }
    }

    internal class EnvironmentProbe
    {
        public CheckEventArgs DeserializeYaml { get; }
        public GlobalOptions? Options { get; }

        public const string CheckInfrastructureTaskName = "Checking Infrastructure";
        public const string CheckMicroservicesTaskName = "Checking Microservices";

        public const string Infrastructure = "Infrastructure";
        public const string Microservices = "Microservices";

        public Dictionary<string, Probeable> Probes = new();

        internal int GetExitCode()
        {
            // get all things we can check
            foreach (var prop in typeof(EnvironmentProbe).GetProperties())
            {
                var val = prop.GetValue(this);

                // did any checks run
                if (val is CheckEventArgs cea)
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

        public EnvironmentProbe(string? yamlFile)
        {
            Probes = new();
            Add(Infrastructure, "RabbitMq", ProbeRabbitMq);
            Add(Infrastructure, "MongoDb", ProbeMongoDb);
            Add(Infrastructure, "Rdmp", ProbeRdmp);

            Add(Microservices, "CohortExtractor", () => Probe(nameof(CohortExtractorHost), (o) => new CohortExtractorHost(o, null, null)));
            Add(Microservices, "DicomAnonymiser", () => Probe(nameof(DicomAnonymiserHost), (o) => new DicomAnonymiserHost(o)));
            Add(Microservices, "IsIdentifiable", () => Probe(nameof(IsIdentifiableHost), (o) => new IsIdentifiableHost(o)));
            Add(Microservices, "CohortPackager", () => Probe(nameof(CohortPackagerHost), (o) => new CohortPackagerHost(o)));
            Add(Microservices, "DicomRelationalMapper", () => Probe(nameof(DicomRelationalMapperHost), (o) => new DicomRelationalMapperHost(o)));
            Add(Microservices, "IdentifierMapper", () => Probe(nameof(IdentifierMapperHost), (o) => new IdentifierMapperHost(o)));
            Add(Microservices, "MongoDbPopulator", () => Probe(nameof(MongoDbPopulatorHost), (o) => new MongoDbPopulatorHost(o)));
            Add(Microservices, "DicomTagReader", () => Probe(nameof(DicomTagReaderHost), (o) => new DicomTagReaderHost(o)));



            /*
 {
                get
DicomTagReader {
                    get;
                    MongoDbPopulator {
                        ge
                    IdentifierMapper {
                            ge
                    DicomRelationalMapper
                    DicomAnonymiser {
                                get
                    IsIdentifiable {
                                    get;
                                    CohortPackager {
                                        get;

            */
            try
            {
                if (string.IsNullOrWhiteSpace(yamlFile))
                    throw new Exception("You have not yet entered a path for yaml file");

                Options = new GlobalOptionsFactory().Load("Setup", yamlFile);
                DeserializeYaml = new CheckEventArgs("Deserialized Yaml File", CheckResult.Success);
            }
            catch (Exception ex)
            {
                DeserializeYaml = new CheckEventArgs("Failed to Deserialize Yaml File", CheckResult.Fail, ex);
            }
        }

        private void Add(string category, string name, Func<CheckEventArgs?> probeMethod)
        {
            Probes.Add(name, new Probeable(name, probeMethod, category));
        }

        internal void CheckInfrastructure(IDataLoadEventListener? listener = null)
        {
            var probes = Probes.Where(p => p.Value.Category == Infrastructure).ToArray();

            var sw = Stopwatch.StartNew();
            var max = probes.Length;
            var current = 0;
            var task = CheckInfrastructureTaskName;

            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(current, ProgressType.Records, max), sw.Elapsed));

            foreach (var p in probes)
            {
                // clear old result
                p.Value.Result = null;
                p.Value.Result = p.Value.Run();

                listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));
            }
        }
        internal void CheckMicroservices(IDataLoadEventListener? listener = null)
        {
            var probes = Probes.Where(p => p.Value.Category == Microservices).ToArray();

            var sw = Stopwatch.StartNew();
            var max = probes.Length;
            var current = 0;
            var task = CheckMicroservicesTaskName;

            listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(current, ProgressType.Records, max), sw.Elapsed));

            foreach (var p in probes)
            {
                // clear old result
                p.Value.Result = null;
                p.Value.Result = p.Value.Run();

                listener?.OnProgress(this, new ProgressEventArgs(task, new ProgressMeasurement(++current, ProgressType.Records, max), sw.Elapsed));
            }
        }

        public CheckEventArgs? ProbeRdmp()
        {
            try
            {
                if (Options == null)
                    return null;

                if (Options.RDMPOptions == null ||

                    // Must specify either SqlServer or file system backend for RDMP platform metadata
                    (string.IsNullOrEmpty(Options.RDMPOptions.CatalogueConnectionString) &&
                    string.IsNullOrWhiteSpace(Options.RDMPOptions.YamlDir)))
                {
                    throw new Exception("No RDMP connection settings specified");
                }

                var provider = Options.RDMPOptions.GetRepositoryProvider();

                var startup = new Startup(provider);

                var failed = false;
                var sb = new StringBuilder();
                var exceptions = new List<Exception>();

                startup.DatabaseFound += (s, e) =>
                {
                    failed = !failed && e.Status != RDMPPlatformDatabaseStatus.Healthy || e.Exception != null;
                    sb.AppendLine($"{e.Patcher.Name} {e.Status}");

                    if (e.Exception != null)
                    {
                        sb.AppendLine(ExceptionHelper.ExceptionToListOfInnerMessages(e.Exception));
                        exceptions.Add(e.Exception);
                    }
                };

                startup.DoStartup(ThrowImmediatelyCheckNotifier.Quiet);

                return new CheckEventArgs(sb.ToString(), failed ? CheckResult.Fail : CheckResult.Success);
            }
            catch (Exception ex)
            {
                return new CheckEventArgs("Failed to connect to RDMP", CheckResult.Fail, ex);
            }
        }

        public CheckEventArgs? ProbeRabbitMq()
        {
            if (Options?.RabbitOptions == null)
                return null;

            try
            {
                var adapter = new RabbitMQBroker(Options.RabbitOptions, "setup");

                return new CheckEventArgs("Connected to RabbitMq", CheckResult.Success);
            }
            catch (Exception ex)
            {
                return new CheckEventArgs("Failed to connect to RabbitMq", CheckResult.Fail, ex);
            }
        }

        public CheckEventArgs? ProbeMongoDb()
        {
            if (Options?.MongoDatabases?.DicomStoreOptions == null)
                return null;

            try
            {
                // this opens connection to the server and tests for collection existing
                _=new MongoDbAdapter("Setup", Options.MongoDatabases.DicomStoreOptions,
                         Options.MongoDbPopulatorOptions?.ImageCollection ?? throw new InvalidOperationException());


                var mongoDbOptions = Options.MongoDatabases.ExtractionStoreOptions
                                     ?? throw new ArgumentException($"ExtractionStoreOptions was null");

                _ = new MongoExtractJobStore(
                    MongoClientHelpers.GetMongoClient(mongoDbOptions, "Setup"),
                    mongoDbOptions.DatabaseName ?? throw new InvalidOperationException(), new Smi.Common.Helpers.DateTimeProvider()
                );

                return new CheckEventArgs("MongoDb Checking Succeeded", CheckResult.Success);
            }
            catch (Exception ex)
            {
                return new CheckEventArgs("MongoDb Checking Failed", CheckResult.Fail, ex);
            }
        }

        private CheckEventArgs? Probe(string probeName, Func<GlobalOptions, MicroserviceHost> hostConstructor)
        {
            if (Options == null)
                return null;

            try
            {
                var host = hostConstructor(Options);

                host.StartAuxConnections();
                host.Start();

                host.Stop("Finished Testing");

                return new CheckEventArgs($"{probeName} Succeeded", CheckResult.Success);
            }
            catch (Exception ex)
            {
                return new CheckEventArgs($"{probeName} Failed", CheckResult.Fail, ex);
            }
        }

    }
}
