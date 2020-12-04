using Rdmp.Core.Repositories;
using Smi.Common.Messages;
using Smi.Common.Messages.Updating;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Microservices.UpdateValues.Execution
{
    public class UpdateValuesQueueConsumer : Consumer<UpdateValuesMessage>
    {
        private Updater _updater;

        public UpdateValuesQueueConsumer(UpdateValuesOptions opts,ICatalogueRepository repo)
        {
            _updater = new Updater(repo);
            _updater.UpdateTimeout = opts.UpdateTimeout;
            _updater.TableInfosToUpdate = opts.TableInfosToUpdate;
        }
        protected override void ProcessMessageImpl(IMessageHeader header, UpdateValuesMessage message, ulong tag)
        {
            _updater.HandleUpdate(message);

            Ack(header,tag);
        }
    }
}