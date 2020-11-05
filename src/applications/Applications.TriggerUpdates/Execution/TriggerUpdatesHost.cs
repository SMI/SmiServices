using Smi.Common.Execution;
using Smi.Common.Messages.Updating;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace TriggerUpdates.Execution
{
    public class TriggerUpdatesHost : MicroserviceHost
    {
        private ITriggerUpdatesSource _source;
        private IProducerModel _producer;

        public TriggerUpdatesHost(GlobalOptions options,ITriggerUpdatesSource source):base(options)
        {
            this._source = source;
            _producer =  RabbitMqAdapter.SetupProducer(options.TriggerUpdatesOptions, isBatch: false);
        }
        
        public override void Start()
        {
            UpdateValuesMessage msg;

            while((msg = _source.Next()) != null)
            {
                _producer.SendMessage(msg,null);
            }
            
            Stop("Directory scan completed");
        }
    }
}