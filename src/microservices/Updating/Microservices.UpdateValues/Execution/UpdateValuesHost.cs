using JetBrains.Annotations;
using Rdmp.Core.Repositories;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.UpdateValues.Execution
{
    public class UpdateValuesHost : MicroserviceHost
    {
        public UpdateValuesQueueConsumer Consumer {get;set;}

        public UpdateValuesHost([NotNull] GlobalOptions globals, IRabbitMqAdapter rabbitMqAdapter = null, bool loadSmiLogConfig = true, bool threaded = false) : base(globals, rabbitMqAdapter, loadSmiLogConfig, threaded)
        {
        }

        public override void Start()
        {
            
            IRDMPPlatformRepositoryServiceLocator repositoryLocator = Globals.RDMPOptions.GetRepositoryProvider();
            Consumer = new UpdateValuesQueueConsumer(Globals.UpdateValuesOptions,repositoryLocator.CatalogueRepository);

            RabbitMqAdapter.StartConsumer(Globals.UpdateValuesOptions, Consumer, isSolo: false);
        }
    }
}
