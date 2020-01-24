using System;
using System.Collections.Generic;
using System.Text;
using Smi.Common.Execution;
using Smi.Common.Helpers;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableHost : MicroserviceHost
    {
        private ConsumerOptions _consumerOptions;
        private IConsumer _consumer;
        private IProducerModel _producerModel;

        public IsIdentifiableHost(GlobalOptions globals, bool loadSmiLogConfig = true) : base(globals, loadSmiLogConfig)
        {
            _consumerOptions = globals.IsIdentifiableOptions;

            string classifierTypename =  globals.IsIdentifiableOptions.ClassifierType;

            if(string.IsNullOrWhiteSpace(classifierTypename))
                throw new ArgumentException("No IClassifier has been set in options.  Enter a value for ClassifierType",nameof(globals));

            var objectFactory = new MicroserviceObjectFactory();
            var classifier = objectFactory.CreateInstance<IClassifier>(classifierTypename,typeof(IClassifier).Assembly);

            if(classifier == null)
                throw new TypeLoadException($"Could not find IClassifier Type { classifierTypename }");

            _producerModel = RabbitMqAdapter.SetupProducer(globals.IsIdentifiableOptions.IsIdentifiableProducerOptions, isBatch: false);

            _consumer = new IsIdentifiableQueueConsumer(_producerModel,globals.FileSystemOptions.FileSystemRoot,classifier);
        }

        public override void Start()
        {
            RabbitMqAdapter.StartConsumer(_consumerOptions, _consumer);
        }
    }
}
