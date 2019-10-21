
package org.smi.common.messaging;

import org.smi.common.messages.IMessageHeader;

public interface IProducerModel {
    /**
     * Sends a message to a RabbitMQ exchange
     * 
     * @param message Message object to serialize and send
     * @param routingKey Routing key for the exchange to direct the message
     */
    void SendMessage(Object message, String routingKey, IMessageHeader inResponseTo);    
}
