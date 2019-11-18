
package org.smi.common.messaging;

import java.io.IOException;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions;
import org.smi.common.messages.MessageHeader;
import org.smi.common.messages.SimpleMessage;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.rabbitmq.client.AMQP.BasicProperties;
import com.rabbitmq.client.Envelope;

public class SimpleConsumer extends SmiConsumer {
	
	private String _message = null;
	
	@Override
	public void handleDeliveryImpl(String consumerTag, Envelope envelope, BasicProperties properties, byte[] body, MessageHeader header)
			throws IOException {

		final Gson gson = new GsonBuilder()
			    .registerTypeAdapter(SimpleMessage.class, new JsonDeserializerWithOptions<SimpleMessage>())
			    .create();


		JsonObject jObj = new JsonParser().parse(new String(body, "UTF-8")).getAsJsonObject();
		SimpleMessage message = gson.fromJson(jObj, SimpleMessage.class);
		
		_message = message.Message;		
		_channel.basicAck(envelope.getDeliveryTag(), false);
	}

	public String getMessage() {
		
		return _message;
	}

}
