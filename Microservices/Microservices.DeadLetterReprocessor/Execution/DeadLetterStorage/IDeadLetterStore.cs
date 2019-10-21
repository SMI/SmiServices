
using Microservices.Common.Messages;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

namespace Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage
{
    public interface IDeadLetterStore
    {
        /// <summary>
        /// Store all information about a message for later reprocessing
        /// </summary>
        /// <param name="deliverArgs"></param>
        /// <param name="header"></param>
        /// <param name="retryAfter"></param>
        void PersistMessageToStore(BasicDeliverEventArgs deliverArgs, IMessageHeader header, TimeSpan retryAfter);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toStore"></param>
        /// <param name="retryAfter"></param>
        void PersistMessageToStore(IEnumerable<Tuple<BasicDeliverEventArgs, IMessageHeader>> toStore, TimeSpan retryAfter);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<BasicDeliverEventArgs> GetMessagesForReprocessing(string queueFilter, bool forceProcess, Guid messageGuid = new Guid());

        /// <summary>
        /// Send a message which already exists in the store to the graveyard
        /// </summary>>
        /// <param name="messageGuid"></param>
        /// <param name="reason"></param>
        /// <param name="e"></param>
        void SendToGraveyard(Guid messageGuid, string reason, Exception e = null);

        /// <summary>
        /// Send a message which doesn't exist in the store to the graveyard
        /// </summary>
        /// <param name="deliverArgs"></param>
        /// <param name="header"></param>
        /// <param name="reason"></param>
        /// <param name="e"></param>
        void SendToGraveyard(BasicDeliverEventArgs deliverArgs, IMessageHeader header, string reason, Exception e = null);

        /// <summary>
        /// Notify the store that a message has been republished
        /// </summary>
        /// <param name="messageGuid"></param>
        void NotifyMessageRepublished(Guid messageGuid);
    }
}
