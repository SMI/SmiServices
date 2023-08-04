using Smi.Common.Execution;
using Smi.Common.Options;
using Microservices.DicomRelationalMapper.Messaging;
using NLog;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.EntityNaming;
using Rdmp.Core.Logging.Listeners.Extensions;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using System;
using Smi.Common;


namespace Microservices.DicomRelationalMapper.Execution
{
    public class DicomRelationalMapperHost : MicroserviceHost, IDisposable
    {
        public DicomRelationalMapperQueueConsumer? Consumer { get; private set; }

        public DicomRelationalMapperHost(GlobalOptions globals)
            : base(globals)
        {
            FansiImplementations.Load();
        }

        //TODO Should most of this not be in the constructor?
        public override void Start()
        {
            IRDMPPlatformRepositoryServiceLocator repositoryLocator = Globals.RDMPOptions.GetRepositoryProvider();

            Logger.Info("About to run Startup");

            var startup = new Startup(new EnvironmentInfo(PluginFolders.Main), repositoryLocator);
            startup.DatabaseFound += Startup_DatabaseFound;

            var toMemory = new ToMemoryCheckNotifier();
            startup.DoStartup(toMemory);

            foreach (CheckEventArgs args in toMemory.Messages)
                Logger.Log(args.ToLogLevel(), args.Ex, args.Message);

            Logger.Info("Startup Completed");

            var lmd = repositoryLocator.CatalogueRepository.GetObjectByID<LoadMetadata>(Globals.DicomRelationalMapperOptions.LoadMetadataId);

            Type databaseNamerType = repositoryLocator.CatalogueRepository.MEF.GetType(Globals.DicomRelationalMapperOptions.DatabaseNamerType);

            if(databaseNamerType == null)
            {
                throw new Exception($"Could not find Type '{Globals.DicomRelationalMapperOptions.DatabaseNamerType}'");
            }

            string liveDatabaseName = lmd.GetDistinctLiveDatabaseServer().GetCurrentDatabase().GetRuntimeName();

            var instance = ObjectFactory.CreateInstance<INameDatabasesAndTablesDuringLoads>(databaseNamerType, liveDatabaseName, Globals.DicomRelationalMapperOptions.Guid)
                ?? throw new Exception("Could not create an INameDatabasesAndTablesDuringLoads");

            Consumer = new DicomRelationalMapperQueueConsumer(repositoryLocator,
                                                              lmd,
                                                              instance,
                                                              Globals.DicomRelationalMapperOptions)
            {
                RunChecks = Globals.DicomRelationalMapperOptions.RunChecks
            };

            RabbitMqAdapter.StartConsumer(Globals.DicomRelationalMapperOptions, Consumer, isSolo: false);
        }

        private void Startup_DatabaseFound(object sender, PlatformDatabaseFoundEventArgs e)
        {
            
            string msg = "RDMPPlatformDatabaseStatus is " + e.Status + " for tier " + e.Patcher.Tier +
                  (e.Exception == null
                      ? "No exception"
                      : ExceptionHelper.ExceptionToListOfInnerMessages(e.Exception));

            Logger.Log(e.Status == RDMPPlatformDatabaseStatus.Healthy ? LogLevel.Info : LogLevel.Error, e.Exception, msg);
        }

        public override void Stop(string reason)
        {
            if (Consumer != null)
                Consumer.Stop(reason);

            base.Stop(reason);
        }

        public void Dispose()
        {
            if(Consumer != null)
                Consumer.Dispose();
        }
    }
}
