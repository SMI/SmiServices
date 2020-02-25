
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Microservices.IdentifierMapper.Execution.Swappers;
using Microservices.IdentifierMapper.Messaging;
using System;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using NLog;
using RabbitMQ.Client.Exceptions;


namespace Microservices.IdentifierMapper.Execution
{
    public class IdentifierMapperHost : MicroserviceHost
    {
        public readonly IdentifierMapperQueueConsumer Consumer;

        private Guid _consumerId;
        private readonly IdentifierMapperOptions _consumerOptions;

        private readonly IProducerModel _producerModel;

        private readonly ISwapIdentifiers _swapper;



        public IdentifierMapperHost(GlobalOptions options, ISwapIdentifiers swapper = null, bool loadSmiLogConfig = true)
            : base(options, loadSmiLogConfig, threaded: true)
        {
            _consumerOptions = options.IdentifierMapperOptions;

            //load all supported implementations
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<OracleImplementation>();
            ImplementationManager.Load<MicrosoftSQLImplementation>();
            ImplementationManager.Load<PostgreSqlImplementation>();

            if (swapper == null)
            {
                Logger.Info("Not passed a swapper, creating one of type " + options.IdentifierMapperOptions.SwapperType);
                _swapper = ObjectFactory.CreateInstance<ISwapIdentifiers>(options.IdentifierMapperOptions.SwapperType, typeof(ISwapIdentifiers).Assembly);
            }
            else
            {
                _swapper = swapper;
            }

            //if we want to use a Redis server to cache answers then wrap the mapper in a Redis caching swapper
            if (!string.IsNullOrWhiteSpace(options.IdentifierMapperOptions.RedisHost))
                _swapper = new RedisSwapper(options.IdentifierMapperOptions.RedisHost, _swapper);

            _swapper.Setup(_consumerOptions);
            Logger.Info($"Swapper of type {_swapper.GetType()} created");

            // Batching now handled implicitly as backlog demands
            _producerModel = RabbitMqAdapter.SetupProducer(options.IdentifierMapperOptions.AnonImagesProducerOptions, isBatch: true);

            Consumer = new IdentifierMapperQueueConsumer(_producerModel, _swapper)
            {
                AllowRegexMatching = options.IdentifierMapperOptions.AllowRegexMatching
            };

            // Add our event handler for control messages
            AddControlHandler(new IdentifierMapperControlMessageHandler(_swapper));
        }

        public override void Start()
        {
            _consumerId = RabbitMqAdapter.StartConsumer(_consumerOptions, Consumer);
        }

        public override void Stop(string reason)
        {
            if (_consumerId != Guid.Empty)
                RabbitMqAdapter.StopConsumer(_consumerId);
            try
            {
                // Wait for any unconfirmed messages before calling stop
                _producerModel.WaitForConfirms();
            }
            catch (AlreadyClosedException)
            {
                
            }

            _swapper?.LogProgress(Logger, LogLevel.Info);

            base.Stop(reason);
        }
    }
}
