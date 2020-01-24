using System.Collections.Generic;
using System.Text;
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableHost : MicroserviceHost
    {
        private ConsumerOptions _consumerOptions;
        private IConsumer _consumer;

        public IsIdentifiableHost(GlobalOptions globals, bool loadSmiLogConfig = true) : base(globals, loadSmiLogConfig)
        {
            _consumerOptions = globals.IsIdentifiableOptions;
            _consumer = new IsIdentifiableQueueConsumer(globals.FileSystemOptions.FileSystemRoot);
        }

        public override void Start()
        {
            RabbitMqAdapter.StartConsumer(_consumerOptions, _consumer);
        }
    }
}
