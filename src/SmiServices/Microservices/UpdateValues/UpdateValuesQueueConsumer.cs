using Rdmp.Core.Repositories;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Updating;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;

namespace SmiServices.Microservices.UpdateValues
{
    public class UpdateValuesQueueConsumer : Consumer<UpdateValuesMessage>
    {
        private readonly Updater _updater;

        public UpdateValuesQueueConsumer(UpdateValuesOptions opts, ICatalogueRepository repo)
        {
            _updater = new Updater(repo)
            {
                UpdateTimeout = opts.UpdateTimeout,
                TableInfosToUpdate = opts.TableInfosToUpdate
            };
        }

        DateTime lastPerformanceAudit = new(2001, 1, 1);
        readonly TimeSpan auditEvery = TimeSpan.FromSeconds(60);

        protected override void ProcessMessageImpl(IMessageHeader header, UpdateValuesMessage message, ulong tag)
        {
            _updater.HandleUpdate(message);

            Ack(header, tag);

            if (DateTime.Now.Subtract(lastPerformanceAudit) > auditEvery)
            {
                _updater.LogProgress(Logger, NLog.LogLevel.Trace);
                lastPerformanceAudit = DateTime.Now;
            }
        }
    }
}
