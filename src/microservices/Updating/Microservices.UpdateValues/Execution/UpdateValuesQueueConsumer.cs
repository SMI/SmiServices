using Rdmp.Core.Repositories;
using Smi.Common.Messages;
using Smi.Common.Messages.Updating;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;

namespace Microservices.UpdateValues.Execution
{
    public class UpdateValuesQueueConsumer : Consumer<UpdateValuesMessage>
    {
        private Updater _updater;

        public UpdateValuesQueueConsumer(UpdateValuesOptions opts,ICatalogueRepository repo)
        {
            _updater = new Updater(repo);
            _updater.UpdateTimeout = opts.UpdateTimeout;
            _updater.TableInfosToUpdate = opts.TableInfosToUpdate;
        }

        DateTime lastPerformanceAudit = new(2001,1,1);
        TimeSpan auditEvery = TimeSpan.FromSeconds(60);

        protected override void ProcessMessageImpl(IMessageHeader? header, UpdateValuesMessage message, ulong tag)
        {
            _updater.HandleUpdate(message);

            Ack(header,tag);

            if(DateTime.Now.Subtract(lastPerformanceAudit) > auditEvery)
            {
                _updater.LogProgress(Logger,NLog.LogLevel.Trace);
                lastPerformanceAudit = DateTime.Now;
            }
        }
    }
}
