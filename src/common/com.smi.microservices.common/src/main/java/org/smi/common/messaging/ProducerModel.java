
package org.smi.common.messaging;

import com.google.gson.Gson;
import com.rabbitmq.client.AMQP;
import com.rabbitmq.client.Channel;
import org.smi.common.messages.IMessageHeader;
import org.smi.common.messages.MessageHeaderFactory;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

public class ProducerModel implements IProducerModel {

	// private final static Logger log = Logger.getLogger(ProducerModel.class);

	/** Channel through which to publish */
	public Channel _channel;

	/** Exchange name to publish to */
	public String _exchangeName;

	/** Our Gson instance for converting objects to json */
	private final Gson _gson;

	/** Default properties to use for sending new messages */
	private final AMQP.BasicProperties _properties;

	/** Factory to build new MessageHeaders with */
	private final MessageHeaderFactory _headerFactory;

	/**
	 * Constructor
	 * 
	 * @param exchangeName
	 *            The exchange to which this producer will send messages to
	 * @param channel
	 *            The connection to the server
	 */
	public ProducerModel(String exchangeName, Channel channel, AMQP.BasicProperties properties, MessageHeaderFactory headerFactory) {

		// log.debug("Creating a producer: exchange name: " + exchangeName);

		_channel = channel;
		_exchangeName = exchangeName;
		_gson = new Gson();
		_properties = properties;
		_headerFactory = headerFactory;
	}

	/*
	 * (non-Javadoc)
	 * 
	 * @see
	 * SMIPlugin.Microservices.Common.IProducerModel#SendMessage(java.lang.Object,
	 * java.lang.String)
	 */
	public void SendMessage(Object message, String routingKey, IMessageHeader inResponseTo) throws IOException, InterruptedException {

		if (routingKey == null)
			routingKey = "";

		byte[] body = _gson.toJson(message, message.getClass()).getBytes(StandardCharsets.UTF_8);

		Map<String, Object> headers = new HashMap<>();

		IMessageHeader header = _headerFactory.getHeader(inResponseTo);
		header.Populate(headers);

		AMQP.BasicProperties properties = new AMQP.BasicProperties()
				.builder()
				.contentEncoding(_properties.getContentEncoding())
				.contentType(_properties.getContentType())
				.deliveryMode(_properties.getDeliveryMode())
				.headers(headers)
				.timestamp(new Date(System.currentTimeMillis() / 1000L))
				.build();

		_channel.basicPublish(_exchangeName, routingKey, properties, body);
		_channel.waitForConfirms();
	}
}
