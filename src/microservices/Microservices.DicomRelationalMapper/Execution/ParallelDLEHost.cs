
using System.Text.RegularExpressions;
using Microservices.DicomRelationalMapper.Execution.Namers;
using FAnsi.Discovery;
using ReusableLibraryCode.Progress;
using System.Collections.Generic;
using Rdmp.Core.Repositories;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.EntityNaming;
using Rdmp.Core.Curation.Data.EntityNaming;
using Rdmp.Core.DataLoad;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;
using Rdmp.Core.DataLoad.Engine.LoadExecution;
using Rdmp.Core.DataLoad.Engine.LoadProcess;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.DataLoad.Engine.LoadExecution.Components;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.LoadExecution.Components.Standard;

namespace Microservices.DicomRelationalMapper.Execution
{
    /// <summary>
    /// Sets up an ad-hoc data load in which the namer is changed to the specified <see cref="INameDatabasesAndTablesDuringLoads"/> and
    /// an ExplicitListDicomProcessListProvider is injected into the loads AutoRoutingAttacher component (Payload field)
    /// </summary>
    public class ParallelDLEHost
    {
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private readonly INameDatabasesAndTablesDuringLoads _namer;
        private readonly bool _useInsertIntoForRawMigration;
        private HICDatabaseConfiguration _configuration;

        public ParallelDLEHost(IRDMPPlatformRepositoryServiceLocator repositoryLocator, INameDatabasesAndTablesDuringLoads namer, bool useInsertIntoForRawMigration)
        {
            _repositoryLocator = repositoryLocator;
            _namer = namer;
            _useInsertIntoForRawMigration = useInsertIntoForRawMigration;
        }

        public ExitCodeType RunDLE(LoadMetadata lmd, IDicomFileWorklist payload)
        {
            return RunDLE(lmd, (object)payload);
        }
        public ExitCodeType RunDLE(LoadMetadata lmd, IDicomDatasetWorklist payload)
        {
            return RunDLE(lmd, (object)payload);
        }

        /// <summary>
        /// Runs the DLE using a custom names for RAW/STAGING.  Pass in the load to execute and the files/directories to process
        /// in the batch.
        /// </summary>
        /// <param name="lmd"></param>
        /// <param name="payload"></param>
        /// <returns>The exit code of the data load after it completes</returns>
        private ExitCodeType RunDLE(LoadMetadata lmd, object payload)
        {
            var catalogueRepository = lmd.CatalogueRepository;

            //ensures that RAW/STAGING always have unique names
            _configuration = new HICDatabaseConfiguration(lmd, _namer);
            _configuration.UpdateButDoNotDiff = new Regex("^MessageGuid");

            var logManager = catalogueRepository.GetDefaultLogManager();

            logManager.CreateNewLoggingTaskIfNotExists(lmd.GetDistinctLoggingTask());

            // Create the pipeline to pass into the DataLoadProcess object
            var dataLoadFactory = new HICDataLoadFactory(lmd, _configuration, new HICLoadConfigurationFlags(),
                catalogueRepository, logManager);

            if (_namer is ICreateAndDestroyStagingDuringLoads stagingCreator)
                stagingCreator.CreateStaging(lmd.GetDistinctLiveDatabaseServer());

            var listener = new NLogThrowerDataLoadEventListener(NLog.LogManager.GetCurrentClassLogger());

            IDataLoadExecution execution = dataLoadFactory.Create(listener);

            IExternalDatabaseServer raw = catalogueRepository.GetDefaultFor(PermissableDefaults.RAWDataLoadServer);

            DiscoveredServer liveDb = lmd.GetDistinctLiveDatabaseServer();

            //do we want to try to cut down the time it takes to do RAW=>STAGING by using INSERT INTO  instead of running anonymisation/migration pipeline
            if (_useInsertIntoForRawMigration)
                //if it is on the same server swap out the migration engine for INSERT INTO
                if (raw == null || (raw.Server != null && raw.Server.Equals(liveDb.Name) && raw.DatabaseType == liveDb.DatabaseType))
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "SWAPPING RAW=>STAGING migration strategy to INSERT INTO"));
                    SwapMigrateRAWToStagingComponent(execution.Components);
                }
                else
                {
                    //Cannot use because different servers / DatabaseTypes.
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "CANNOT SWAP RAW=>STAGING migration strategy to INSERT INTO because RAW is on '" + raw.Server + "' (" + raw.DatabaseType + ") and STAGING is on '" + liveDb.Name + "' (" + liveDb.DatabaseType + ")"));
                }
            else
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Flag is false for SWAP RAW=>STAGING migration strategy to INSERT INTO So won't do it"));

            var procedure = new DataLoadProcess(_repositoryLocator, lmd, null, logManager, listener, execution,_configuration);

            ExitCodeType exitCode = procedure.Run(new GracefulCancellationToken(), payload);

            return exitCode;
        }

        private void SwapMigrateRAWToStagingComponent(IList<IDataLoadComponent> components)
        {
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] is CompositeDataLoadComponent composite)
                    SwapMigrateRAWToStagingComponent(composite.Components);

                if (components[i] is MigrateRAWToStaging)
                    components[i] = new MigrateRawToStagingWithSelectIntoStatements();
            }


        }
    }
}