package org.smi.common.rabbitMq;

import com.rabbitmq.client.*;
import org.smi.common.messages.MessageHeaderFactory;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.messaging.ProducerModel;
import org.smi.common.messaging.SmiConsumer;
import org.smi.common.options.ConsumerOptions;
import org.smi.common.options.GlobalOptions.RabbitOptions;
import org.smi.common.options.ProducerOptions;

import java.io.IOException;
import java.net.ConnectException;
import java.util.*;
import java.util.concurrent.TimeoutException;
import java.util.regex.Pattern;

/**
 * Helper class for using RabbitMQ
 */
public class RabbitMqAdapter {

	private ConnectionFactory _factory;

	/**
	 * List of all the connections
	 */
	private List<Connection> _connections = new ArrayList<Connection>();

	/**
	 * List of all the channels
	 */
	private List<Channel> _channels = new ArrayList<Channel>();

	/**
	 * List of all the thread/consumer pairs
	 */
	private Map<Thread, ConsumeRunnable> _threads = new HashMap<Thread, ConsumeRunnable>();

	/**
	 * The options for the microservices
	 */
	private final RabbitOptions _options;

	private final MessageHeaderFactory _headerFactory;

	private final int MinRabbitServerVersionMajor = 3;
	private final int MinRabbitServerVersionMinor = 7;
	private final int MinRabbitServerVersionPatch = 0;

	private String _rabbitMqServerVersion;

	public String getRabbitMqServerVersion() {
		return _rabbitMqServerVersion;
	}

	private String _hostId;

	/**
	 * Constructor for the RabbitMQ adapter helper class
	 *
	 * @param options The options for the microservices
	 * @throws TimeoutException
	 * @throws IOException
	 */
	public RabbitMqAdapter(RabbitOptions options, String microserviceName) throws IOException, TimeoutException {

		// TODO Make sure fatal logging setup properly

		_options = options;

		_factory = new ConnectionFactory();
		_factory.setHost(_options.RabbitMqHostName);
		_factory.setPort(_options.RabbitMqHostPort);
		_factory.setVirtualHost(_options.RabbitMqVirtualHost);
		_factory.setUsername(_options.RabbitMqUserName);
		_factory.setPassword(_options.RabbitMqPassword);

		CheckValidServerSettings();

		// TODO Log this
		int randomPid = new Random().nextInt() & Integer.MAX_VALUE;

		_headerFactory = new MessageHeaderFactory(microserviceName, randomPid);
		_hostId = _headerFactory.getProcessName() + _headerFactory.getProcessId();
	}

	private void CheckValidServerSettings() throws IOException, TimeoutException {
		try {
			Connection conn = _factory.newConnection("TestConnection");
			if (!conn.getServerProperties().containsKey("version"))
				throw new IOException("Could not get RabbitMQ server version");

			String version = ((LongString) conn.getServerProperties().get("version")).toString();
			String[] split = version.split(Pattern.quote("."));

			if (Integer.parseInt(split[0]) < MinRabbitServerVersionMajor
					|| Integer.parseInt(split[1]) < MinRabbitServerVersionMinor
					|| Integer.parseInt(split[2]) < MinRabbitServerVersionPatch)
				throw new IOException("Connected to RabbitMQ server version " + version + " but minimum required is "
						+ MinRabbitServerVersionMajor + "." + MinRabbitServerVersionMinor + "."
						+ MinRabbitServerVersionPatch);

			_rabbitMqServerVersion = version;
		} catch (ConnectException e) {
			StringBuilder sb = new StringBuilder();
			sb.append("    HostName:                       " + _factory.getHost() + System.lineSeparator());
			sb.append("    Port:                           " + _factory.getPort() + System.lineSeparator());
			sb.append("    UserName:                       " + _factory.getUsername() + System.lineSeparator());
			sb.append("    VirtualHost:                    " + _factory.getVirtualHost() + System.lineSeparator());
			sb.append("    HandshakeContinuationTimeout:   " + _factory.getHandshakeTimeout() + System.lineSeparator());
			String excString = String.format("Could not create a connection to RabbitMQ on startup:%n" + sb);
			throw new RuntimeException(excString, e);
		}
	}

	/**
	 * Set up a subscription to the queue to send messages to the consumer
	 *
	 * @param options  The connection options
	 * @param consumer Consumer that will be sent any received messages
	 */
	public void StartConsumer(ConsumerOptions options, SmiConsumer consumer) {

		Channel channel = getChannel(String.format("%s::Consumer::%s", _hostId, options.QueueName));

		try {

			channel.basicQos(0, options.QoSPrefetchCount, true);
		} catch (IOException e) {

			e.printStackTrace();
		}

		ConsumeRunnable runnable = new ConsumeRunnable(channel, consumer, options);

		Thread thread = new Thread(runnable);
		thread.start();

		_threads.put(thread, runnable);
	}

	/**
	 * Local class to set up the consume operation as a Runnable task
	 */
	private class ConsumeRunnable implements Runnable {

		private Consumer _consumer; /// < The consumer of messages
		private Channel _channel; /// < The channel sending messages
		private ConsumerOptions _options; /// < The options for this consumer

		/**
		 * Finishes the consume operation
		 */
		public void terminate() {
		}

		/**
		 * Constructor
		 *
		 * @param channel  The channel that will be consumed
		 * @param consumer The consumer
		 * @param options  The configuration options for the consumer
		 */
		public ConsumeRunnable(Channel channel, SmiConsumer consumer, ConsumerOptions options) {

			consumer.setChannel(channel);

			_consumer = consumer;
			_channel = channel;
			_options = options;
		}

		/*
		 * (non-Javadoc)
		 *
		 * @see java.lang.Runnable#run()
		 */
		public void run() {

			try {

				_channel.basicConsume(_options.QueueName, _options.AutoAck, _consumer);

			} catch (IOException e) {

				e.printStackTrace();
				System.exit(0);
			}
		}
	}

	/**
	 * Creates a new channel from the connection factory
	 *
	 * @return The created connection
	 */
	private Channel getChannel(String label) {

		// TODO Better error needed overall here

		Connection connection = null;

		try {

			connection = _factory.newConnection(label);

		} catch (IOException e) {

			e.printStackTrace();

		} catch (TimeoutException e) {

			e.printStackTrace();
		}

		Channel channel = null;

		try {

			channel = connection.createChannel();

		} catch (IOException e) {

			e.printStackTrace();
		}

		_connections.add(connection);
		_channels.add(channel);

		return channel;
	}

	/**
	 * Setup a {@link IProducerModel} to send messages with
	 *
	 * @param producerOptions The options for the producer which must include the
	 *                        exchange name
	 * @return Object which can send messages to a RabbitMQ exchange
	 */
	public IProducerModel SetupProducer(ProducerOptions producerOptions) {

		Channel channel = getChannel(String.format("%s::Producer::%s", _hostId, producerOptions.ExchangeName));

		try {

			channel.confirmSelect();

		} catch (IOException e) {

			e.printStackTrace();
		}

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

		for (Map.Entry<Thread, ConsumeRunnable> entry : _threads.entrySet()) {

			entry.getValue().terminate(); // Terminate the consumer
			entry.getKey().interrupt(); // Interrupt the thread
		}

		for (Channel channel : _channels) {

			try {

				channel.close(200, "Goodbye");

			} catch (IOException | TimeoutException e) {

				e.printStackTrace();
			}
		}

		for (Connection connection : _connections) {

			try {

				connection.close();

			} catch (IOException e) {

				e.printStackTrace();
			}
		}
	}
}
