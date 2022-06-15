using Microservices.CohortExtractor.Execution;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.MongoDBPopulator.Execution;
using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
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

        internal void CheckInfrastructure()
        {
            ProbeRabbitMq();

            ProbeMongoDb();

            ProbeRdmp();
        }
        internal void CheckMicroservices()
        {
            ProbeCohortExtractor();
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
            try
            {
                var host = new CohortExtractorHost(Options, null, null);

                host.StartAuxConnections();
                host.Start();

                host.Stop("Finished Testing");

                CohortExtractor = new CheckEventArgs("Testing CohortExtractor Succeeded", CheckResult.Success);
            }
            catch (Exception ex)
            {
                CohortExtractor = new CheckEventArgs("Testing CohortExtractor Failed", CheckResult.Fail, ex);
            }


        }
    }
}
