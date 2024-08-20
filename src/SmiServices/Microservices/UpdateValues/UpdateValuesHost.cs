using Rdmp.Core.Repositories;
using SmiServices.Common;
using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.IO.Abstractions;

namespace SmiServices.Microservices.UpdateValues
{
    public class UpdateValuesHost : MicroserviceHost
    {
        public UpdateValuesQueueConsumer? Consumer { get; set; }

        public UpdateValuesHost(GlobalOptions globals, IFileSystem? fileSystem = null, IMessageBroker? messageBroker = null, bool threaded = false)
        : base(globals, fileSystem ?? new FileSystem(), messageBroker, threaded)
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
