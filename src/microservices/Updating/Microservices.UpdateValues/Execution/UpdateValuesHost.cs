using Rdmp.Core.Repositories;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.UpdateValues.Execution
{
    public class UpdateValuesHost : MicroserviceHost
    {
        public UpdateValuesQueueConsumer? Consumer { get; set; }

        public UpdateValuesHost(GlobalOptions globals, IRabbitMqAdapter? rabbitMqAdapter = null, bool threaded = false) 
        : base(globals, rabbitMqAdapter, threaded)
        {
            FansiImplementations.Load();
        }

        public override void Start()
        {

            IRDMPPlatformRepositoryServiceLocator repositoryLocator = Globals.RDMPOptions.GetRepositoryProvider();
            Consumer = new UpdateValuesQueueConsumer(Globals.UpdateValuesOptions, repositoryLocator.CatalogueRepository);

            RabbitMqAdapter.StartConsumer(Globals.UpdateValuesOptions, Consumer, isSolo: false);
        }
    }
}
