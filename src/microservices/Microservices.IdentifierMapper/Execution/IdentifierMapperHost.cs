
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
            : base(options, loadSmiLogConfig)
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

            Logger.Info("Calling Setup on swapper");

            try
            {
                _swapper.Setup(_consumerOptions);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Failed to setup swapper");
                throw;
            }

            //TODO Probably want to run this in one of two modes:
            //TODO 1) "Batch" -> Preload whole mapping table, process messages in batches (with batch consumer). Can't scale this horizontally (more services running)
            //TODO 2) "Stream" -> Query the database for a swap value for each message as it comes in (with a small cache), produce single messages
            _producerModel = RabbitMqAdapter.SetupProducer(options.IdentifierMapperOptions.AnonImagesProducerOptions, isBatch: false);

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

            _swapper?.LogProgress(Logger);

            base.Stop(reason);
        }
    }
}
