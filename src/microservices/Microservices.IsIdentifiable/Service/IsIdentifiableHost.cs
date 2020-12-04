using System;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microservices.IsIdentifiable.Options;
using Smi.Common.Execution;
using Smi.Common.Helpers;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableHost : MicroserviceHost
    {
        private ConsumerOptions _consumerOptions;
        public IsIdentifiableQueueConsumer Consumer { get; }

        private IProducerModel _producerModel;

        public IsIdentifiableHost(
            [NotNull] GlobalOptions globals,
            [NotNull] IsIdentifiableServiceOptions serviceOpts,
            bool loadSmiLogConfig = true
        )
            : base(globals, loadSmiLogConfig: loadSmiLogConfig)
        {
            _consumerOptions = globals.IsIdentifiableOptions;

            string classifierTypename =  globals.IsIdentifiableOptions.ClassifierType;
            string dataDirectory = globals.IsIdentifiableOptions.DataDirectory;

            if(string.IsNullOrWhiteSpace(classifierTypename))
                throw new ArgumentException("No IClassifier has been set in options.  Enter a value for ClassifierType",nameof(globals));
            if(string.IsNullOrWhiteSpace(dataDirectory))
                throw new ArgumentException("A DataDirectory must be set",nameof(globals));

            var objectFactory = new MicroserviceObjectFactory();
            var classifier = objectFactory.CreateInstance<IClassifier>(classifierTypename, typeof(IClassifier).Assembly, new DirectoryInfo(dataDirectory), serviceOpts);

            if(classifier == null)
                throw new TypeLoadException($"Could not find IClassifier Type { classifierTypename }");

            _producerModel = RabbitMqAdapter.SetupProducer(globals.IsIdentifiableOptions.IsIdentifiableProducerOptions, isBatch: false);

            Consumer = new IsIdentifiableQueueConsumer(_producerModel, globals.FileSystemOptions.FileSystemRoot, globals.FileSystemOptions.ExtractRoot, classifier);
        }

        public override void Start()
        {
            RabbitMqAdapter.StartConsumer(_consumerOptions, Consumer, isSolo: false);
        }

        public override void Stop(string reason)
        {
            base.Stop(reason);

            Consumer?.Dispose();
        }
    }
}
