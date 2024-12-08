using SmiServices.Common;
using SmiServices.Common.Execution;
using SmiServices.Common.Messages.Updating;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;


namespace SmiServices.Applications.TriggerUpdates
{
    public class TriggerUpdatesHost : MicroserviceHost
    {
        private readonly ITriggerUpdatesSource _source;
        private readonly IProducerModel<UpdateValuesMessage> _producer;

        public TriggerUpdatesHost(GlobalOptions options, ITriggerUpdatesSource source, IMessageBroker? messageBroker = null)
            : base(options, messageBroker)
        {
            _source = source;
            _producer = MessageBroker.SetupProducer<UpdateValuesMessage>(options.TriggerUpdatesOptions!, isBatch: false);
        }

        public override void Start()
        {
            foreach (var upd in _source.GetUpdates())
            {
                _producer.SendMessage(upd, isInResponseTo: null, routingKey: null);
            }

            Stop("Update detection process finished");
        }
    }
}
