
package org.smi.common.messaging;

import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

import org.smi.common.messages.IMessageHeader;
import org.smi.common.messages.MessageHeaderFactory;

import com.google.gson.Gson;
import com.rabbitmq.client.AMQP;
import com.rabbitmq.client.Channel;

public class ProducerModel implements IProducerModel {

	// private final static Logger log = Logger.getLogger(ProducerModel.class);

	/** Channel through which to publish */
	public Channel _channel;

	/** Exchange name to publish to */
	public String _exchangeName;

	/** Our Gson instance for converting objects to json */
	private Gson _gson;

	/** Default properties to use for sending new messages */
	private AMQP.BasicProperties _properties;

	/** Factory to build new MessageHeaders with */
	private MessageHeaderFactory _headerFactory;

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
	public void SendMessage(Object message, String routingKey, IMessageHeader inResponseTo) {

		if (routingKey == null)
			routingKey = "";

		byte[] body = null;

		try {

			body = _gson.toJson(message, message.getClass()).getBytes("UTF-8");

		} catch (UnsupportedEncodingException e) {

			e.printStackTrace();
		}

		Map<String, Object> headers = new HashMap<String, Object>();

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

		try {

			_channel.basicPublish(_exchangeName, routingKey, properties, body);

		} catch (IOException e) {

			e.printStackTrace();
		}

		try {

			_channel.waitForConfirms();

		} catch (InterruptedException e) {

			e.printStackTrace();
		}
	}
}
