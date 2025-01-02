using NLog;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.EntityNaming;
using Rdmp.Core.Logging.Listeners.Extensions;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.Startup;
using Rdmp.Core.Startup.Events;
using SmiServices.Common;
using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System;


namespace SmiServices.Microservices.DicomRelationalMapper;

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
        var repositoryLocator = Globals.RDMPOptions!.GetRepositoryProvider();

        Logger.Info("About to run Startup");

        var startup = new Startup(repositoryLocator);
        startup.DatabaseFound += Startup_DatabaseFound;

        var toMemory = new ToMemoryCheckNotifier();
        startup.DoStartup(toMemory);

        foreach (var args in toMemory.Messages)
            Logger.Log(args.ToLogLevel(), args.Ex, args.Message);

        Logger.Info("Startup Completed");

        var lmd = repositoryLocator.CatalogueRepository.GetObjectByID<LoadMetadata>(Globals.DicomRelationalMapperOptions!.LoadMetadataId);

        var databaseNamerType = MEF.GetType(Globals.DicomRelationalMapperOptions.DatabaseNamerType) ?? throw new Exception($"Could not find Type '{Globals.DicomRelationalMapperOptions.DatabaseNamerType}'");

        var liveDatabaseName = lmd.GetDistinctLiveDatabaseServer().GetCurrentDatabase()?.GetRuntimeName() ?? throw new Exception("Unable to find database name");

        var instance = ObjectFactory.CreateInstance<INameDatabasesAndTablesDuringLoads>(databaseNamerType, liveDatabaseName, Globals.DicomRelationalMapperOptions.Guid)
            ?? throw new Exception("Could not create an INameDatabasesAndTablesDuringLoads");

        Consumer = new DicomRelationalMapperQueueConsumer(repositoryLocator,
                                                          lmd,
                                                          instance,
                                                          Globals.DicomRelationalMapperOptions)
        {
            RunChecks = Globals.DicomRelationalMapperOptions.RunChecks
        };

        MessageBroker.StartConsumer(Globals.DicomRelationalMapperOptions, Consumer, isSolo: false);
    }

    private void Startup_DatabaseFound(object sender, PlatformDatabaseFoundEventArgs e)
    {

        var msg = "RDMPPlatformDatabaseStatus is " + e.Status + " for tier " + e.Patcher.Tier +
                  (e.Exception == null
                      ? "No exception"
                      : ExceptionHelper.ExceptionToListOfInnerMessages(e.Exception));

        Logger.Log(e.Status == RDMPPlatformDatabaseStatus.Healthy ? LogLevel.Info : LogLevel.Error, e.Exception, msg);
    }

    public override void Stop(string reason)
    {
        Consumer?.Stop(reason);

        base.Stop(reason);
    }

    public void Dispose()
    {
        Consumer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
