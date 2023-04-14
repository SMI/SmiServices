using JetBrains.Annotations;
using Smi.Common.Execution;
using Smi.Common.Helpers;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.IO.Abstractions;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableHost : MicroserviceHost
    {
        private readonly IFileSystem _fileSystem;

        private ConsumerOptions _consumerOptions;
        public IsIdentifiableQueueConsumer Consumer { get; }

        private IProducerModel _producerModel;

        public IsIdentifiableHost(
            [NotNull] GlobalOptions globals,
            [NotNull] IFileSystem fileSystem
        )
            : base(globals)
        {
            _fileSystem = fileSystem;

            _consumerOptions = globals.IsIdentifiableServiceOptions;

            string classifierTypename = globals.IsIdentifiableServiceOptions.ClassifierType;
            string dataDirectory = globals.IsIdentifiableServiceOptions.DataDirectory;

            if (string.IsNullOrWhiteSpace(classifierTypename))
                throw new ArgumentException("No IClassifier has been set in options.  Enter a value for ClassifierType", nameof(globals));
            if (string.IsNullOrWhiteSpace(dataDirectory))
                throw new ArgumentException("A DataDirectory must be set", nameof(globals));

            var objectFactory = new MicroserviceObjectFactory();
            var classifier = objectFactory.CreateInstance<IClassifier>(classifierTypename, typeof(IClassifier).Assembly, _fileSystem.DirectoryInfo.New(dataDirectory), globals.IsIdentifiableOptions);

            if (classifier == null)
                throw new TypeLoadException($"Could not find IClassifier Type {classifierTypename}");

            _producerModel = RabbitMqAdapter.SetupProducer(globals.IsIdentifiableServiceOptions.IsIdentifiableProducerOptions, isBatch: false);

            Consumer = new IsIdentifiableQueueConsumer(_producerModel, globals.FileSystemOptions.ExtractRoot, classifier);
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
