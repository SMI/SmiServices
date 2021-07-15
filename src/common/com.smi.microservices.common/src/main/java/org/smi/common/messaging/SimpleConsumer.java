
package org.smi.common.messaging;

import java.io.IOException;

import org.smi.common.messages.MessageHeader;
import org.smi.common.messages.SimpleMessage;

import com.rabbitmq.client.AMQP.BasicProperties;
import com.rabbitmq.client.Channel;
import com.rabbitmq.client.Envelope;

public class SimpleConsumer extends SmiConsumer<SimpleMessage> {
	
	public SimpleConsumer(Channel channel) {
		super(channel, SimpleMessage.class);
	}

	private String _message = null;
	
	@Override
	public void handleDeliveryImpl(String consumerTag, Envelope envelope, BasicProperties properties, SimpleMessage body, MessageHeader header)
			throws IOException {
		_message=body.Message;
		_channel.basicAck(envelope.getDeliveryTag(), false);
	}

	public String getMessage() {		
		return _message;
	}

}
