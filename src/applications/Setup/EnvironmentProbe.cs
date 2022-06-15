using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using Smi.Common;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Setup
{

    internal class EnvironmentProbe
    {
        private string? yamlFile;
        
        public CheckEventArgs DeserializeYaml { get; }

        public CheckEventArgs? RabbitMq { get; set; }
        public  GlobalOptions? Options { get; }
        public CheckEventArgs? Rdmp { get; internal set; }

        public EnvironmentProbe(string? yamlFile)
        {
            this.yamlFile = yamlFile;

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

            ProbeRdmp();            
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

                startup.DoStartup(new ThrowImmediatelyCheckNotifier());

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
    }
}
