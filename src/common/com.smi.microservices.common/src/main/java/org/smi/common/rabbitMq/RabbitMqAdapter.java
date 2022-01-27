package org.smi.common.rabbitMq;

import com.rabbitmq.client.AMQP;
import com.rabbitmq.client.Channel;
import com.rabbitmq.client.Connection;
import com.rabbitmq.client.ConnectionFactory;
import org.smi.common.messages.MessageHeaderFactory;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.messaging.ProducerModel;
import org.smi.common.messaging.SmiConsumer;
import org.smi.common.options.ConsumerOptions;
import org.smi.common.options.GlobalOptions.RabbitOptions;
import org.smi.common.options.ProducerOptions;

import java.io.IOException;
import java.util.Random;
import java.util.concurrent.TimeoutException;
import java.util.regex.Pattern;

/**
 * Helper class for using RabbitMQ
 */
public class RabbitMqAdapter {

	private final Connection _conn;

	private final MessageHeaderFactory _headerFactory;

	private String _rabbitMqServerVersion;

	public String getRabbitMqServerVersion() {
		return _rabbitMqServerVersion;
	}

	public final String _hostId;

	/**
	 * Constructor for the RabbitMQ adapter helper class
	 *
	 * @param options The options for the microservices
	 * @throws TimeoutException
	 * @throws IOException
	 */
	public RabbitMqAdapter(RabbitOptions options, String microserviceName) throws IOException, TimeoutException {

		// TODO Make sure fatal logging setup properly

		/*
		 * The options for the microservices
		 */

		ConnectionFactory _factory = new ConnectionFactory();
		_factory.setHost(options.RabbitMqHostName);
		_factory.setPort(options.RabbitMqHostPort);
		_factory.setVirtualHost(options.RabbitMqVirtualHost);
		_factory.setUsername(options.RabbitMqUserName);
		_factory.setPassword(options.RabbitMqPassword);
		_conn = _factory.newConnection();

		CheckValidServerSettings();

		// TODO Log this
		int randomPid = new Random().nextInt() & Integer.MAX_VALUE;

		_headerFactory = new MessageHeaderFactory(microserviceName, randomPid);
		_hostId = _headerFactory.getProcessName() + _headerFactory.getProcessId();
	}

	private void CheckValidServerSettings() throws IOException {
		final int MinRabbitServerVersionMajor = 3;
		final int MinRabbitServerVersionMinor = 7;
		final int MinRabbitServerVersionPatch = 0;

		if (!_conn.getServerProperties().containsKey("version"))
			throw new IOException("Could not get RabbitMQ server version");

		_rabbitMqServerVersion = _conn.getServerProperties().get("version").toString();
		String[] split = _rabbitMqServerVersion.split(Pattern.quote("."));

		if (Integer.parseInt(split[0]) < MinRabbitServerVersionMajor
				|| (Integer.parseInt(split[0]) == MinRabbitServerVersionMajor && Integer.parseInt(split[1]) < MinRabbitServerVersionMinor)
				|| (Integer.parseInt(split[0]) == MinRabbitServerVersionMajor && Integer.parseInt(split[1]) == MinRabbitServerVersionMinor && Integer.parseInt(split[2]) < MinRabbitServerVersionPatch))
		throw new IOException("Connected to RabbitMQ server version " + _rabbitMqServerVersion + " but minimum required is "
				+ MinRabbitServerVersionMajor + "." + MinRabbitServerVersionMinor + "."
				+ MinRabbitServerVersionPatch);
	}

	/**
	 * Set up a subscription to the queue to send messages to the consumer
	 *
	 * @param options  The connection options
	 * @param consumer Consumer that will be sent any received messages
	 */
	public void StartConsumer(ConsumerOptions options, SmiConsumer<?> consumer) throws IOException {
		consumer.getChannel().basicQos(0, options.QoSPrefetchCount, true);
		consumer.getChannel().basicConsume(options.QueueName, options.AutoAck, consumer);
	}

	/**
	 * Creates a new channel from the connection
	 *
	 * @return The created connection
	 * @throws IOException 
	 */
	public Channel getChannel() throws IOException {
		return _conn.createChannel();
	}

	/**
	 * Set up a {@link IProducerModel} to send messages with
	 *
	 * @param producerOptions The options for the producer which must include the
	 *                        exchange name
	 * @return Object which can send messages to a RabbitMQ exchange
	 * @throws IOException 
	 */
	public IProducerModel SetupProducer(ProducerOptions producerOptions) throws IOException {

		Channel channel = getChannel();
		channel.confirmSelect();

		AMQP.BasicProperties props = new AMQP.BasicProperties.Builder()
				.contentEncoding("UTF-8")
				.contentType("application/json")
				.deliveryMode(2)
				.build();

		return new ProducerModel(producerOptions.ExchangeName, channel, props, _headerFactory);
	}

	/**
	 * Close all open connections and stop any consumers
	 */
	public void Shutdown() throws IOException {
		_conn.close();
	}
}
