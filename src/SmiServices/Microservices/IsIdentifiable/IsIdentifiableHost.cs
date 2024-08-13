using SmiServices.Common.Execution;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;
using System.IO;

namespace SmiServices.Microservices.IsIdentifiable
{
    public class IsIdentifiableHost : MicroserviceHost
    {
        private readonly ConsumerOptions _consumerOptions;
        public IsIdentifiableQueueConsumer Consumer { get; }

        private readonly IProducerModel _producerModel;

        public IsIdentifiableHost(
            GlobalOptions globals
        )
            : base(globals)
        {
            _consumerOptions = globals.IsIdentifiableServiceOptions ?? throw new ArgumentNullException(nameof(globals));

            string? classifierTypename = globals.IsIdentifiableServiceOptions.ClassifierType;
            string? dataDirectory = globals.IsIdentifiableServiceOptions.DataDirectory;

            if (string.IsNullOrWhiteSpace(classifierTypename))
                throw new ArgumentException("No IClassifier has been set in options.  Enter a value for ClassifierType", nameof(globals));
            if (string.IsNullOrWhiteSpace(dataDirectory))
                throw new ArgumentException("A DataDirectory must be set", nameof(globals));

            var objectFactory = new MicroserviceObjectFactory();
            var classifier = objectFactory.CreateInstance<IClassifier>(classifierTypename, typeof(IClassifier).Assembly, new DirectoryInfo(dataDirectory), globals.IsIdentifiableOptions!)
                ?? throw new TypeLoadException($"Could not find IClassifier Type {classifierTypename}");
            _producerModel = MessageBroker.SetupProducer(globals.IsIdentifiableServiceOptions.IsIdentifiableProducerOptions!, isBatch: false);

            Consumer = new IsIdentifiableQueueConsumer(_producerModel, globals.FileSystemOptions!.ExtractRoot!, classifier);
        }

        public override void Start()
        {
            MessageBroker.StartConsumer(_consumerOptions, Consumer, isSolo: false);
        }

        public override void Stop(string reason)
        {
            base.Stop(reason);

            Consumer?.Dispose();
        }
    }
}
