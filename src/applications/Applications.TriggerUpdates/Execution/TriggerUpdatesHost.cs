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

            foreach(var upd in _source.GetUpdates())
            {
                _producer.SendMessage(upd,null);
            }
            
            Stop("Directory scan completed");
        }
    }
}