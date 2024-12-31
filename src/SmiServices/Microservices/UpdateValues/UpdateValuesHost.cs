using Rdmp.Core.Repositories;
using SmiServices.Common;
using SmiServices.Common.Execution;
using SmiServices.Common.Options;

namespace SmiServices.Microservices.UpdateValues
{
    public class UpdateValuesHost : MicroserviceHost
    {
        public UpdateValuesQueueConsumer? Consumer { get; set; }

        public UpdateValuesHost(GlobalOptions globals, IMessageBroker? messageBroker = null)
        : base(globals, messageBroker)
        {
            FansiImplementations.Load();
        }

        public override void Start()
        {

            IRDMPPlatformRepositoryServiceLocator repositoryLocator = Globals.RDMPOptions!.GetRepositoryProvider();
            Consumer = new UpdateValuesQueueConsumer(Globals.UpdateValuesOptions!, repositoryLocator.CatalogueRepository);

            MessageBroker.StartConsumer(Globals.UpdateValuesOptions!, Consumer, isSolo: false);
        }
    }
}
