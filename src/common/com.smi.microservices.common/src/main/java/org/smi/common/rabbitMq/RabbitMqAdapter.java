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
import java.util.ArrayList;
import java.util.List;
import java.util.Random;
import java.util.concurrent.TimeoutException;
import java.util.regex.Pattern;

/**
 * Helper class for using RabbitMQ
 */
public class RabbitMqAdapter {

	private final Connection _conn;

	/**
	 * List of all the thread/consumer pairs
	 */
	private final List<ConsumeRunnable> _threads = new ArrayList<ConsumeRunnable>();

	private final MessageHeaderFactory _headerFactory;

	private static final int MinRabbitServerVersionMajor = 3;
	private static final int MinRabbitServerVersionMinor = 7;
	private static final int MinRabbitServerVersionPatch = 0;

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
	public void StartConsumer(ConsumerOptions options, SmiConsumer<?> consumer) {
		try {
			consumer.getChannel().basicQos(0, options.QoSPrefetchCount, true);
		} catch (IOException e) {
			e.printStackTrace();
		}

		ConsumeRunnable runnable = new ConsumeRunnable(consumer, options);
		_threads.add(runnable);
		runnable.start();
	}

	/**
	 * Local class to set up the consume operation as a Runnable task
	 */
	private class ConsumeRunnable extends Thread {

		private SmiConsumer<?> _consumer; /// < The consumer of messages
		private ConsumerOptions _options; /// < The options for this consumer

		/**
		 * Finishes the consume operation
		 */
		public void terminate() {
		}

		/**
		 * Constructor
		 *
		 * @param consumer The consumer
		 * @param options  The configuration options for the consumer
		 */
		public ConsumeRunnable(SmiConsumer<?> consumer, ConsumerOptions options) {
			_consumer = consumer;
			_options = options;
		}

		/*
		 * (non-Javadoc)
		 *
		 * @see java.lang.Runnable#run()
		 */
		public void run() {

			try {
				_consumer.getChannel().basicConsume(_options.QueueName, _options.AutoAck, _consumer);
			} catch (IOException e) {

				e.printStackTrace();
				System.exit(0);
			}
		}
	}

	/**
	 * Creates a new channel from the connection
	 *
	 * @return The created connection
	 * @throws IOException 
	 */
	public Channel getChannel(String label) throws IOException {
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

		Channel channel = getChannel(String.format("%s::Producer::%s", _hostId, producerOptions.ExchangeName));
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
	public void Shutdown() {

		for (ConsumeRunnable entry : _threads) {
			entry.terminate(); // Terminate the consumer
			entry.interrupt(); // Interrupt the thread
		}

		try {
			_conn.close();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}
}
