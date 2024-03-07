using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;


namespace Applications.TriggerUpdates.Execution
{
    public class TriggerUpdatesHost : MicroserviceHost
    {
        private ITriggerUpdatesSource _source;
        private IProducerModel _producer;

        public TriggerUpdatesHost(GlobalOptions options,ITriggerUpdatesSource source,IMessageBroker? messageBroker = null)
            : base(options, messageBroker)
        {
            _source = source;
            _producer =  MessageBroker.SetupProducer(options.TriggerUpdatesOptions!, isBatch: false);
        }
        
        public override void Start()
        {
            foreach(var upd in _source.GetUpdates())
            {
                _producer.SendMessage(upd, isInResponseTo: null, routingKey: null);
            }
            
            Stop("Update detection process finished");
        }
    }
}
