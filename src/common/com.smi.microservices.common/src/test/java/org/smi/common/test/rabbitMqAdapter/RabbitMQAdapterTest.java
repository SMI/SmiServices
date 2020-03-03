
package org.smi.common.test.rabbitMqAdapter;

import java.util.concurrent.TimeUnit;

import org.smi.common.messages.SimpleMessage;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.messaging.SimpleConsumer;
import org.smi.common.messaging.SmiConsumer;
import org.smi.common.options.ConsumerOptions;
import org.smi.common.options.GlobalOptions;
import org.smi.common.options.ProducerOptions;
import org.smi.common.rabbitMq.RabbitMqAdapter;

import com.rabbitmq.client.Channel;
import com.rabbitmq.client.Connection;
import com.rabbitmq.client.ConnectionFactory;

import junit.framework.TestCase;

public class RabbitMQAdapterTest extends TestCase {

	// private final static Logger log =
	// Logger.getLogger(RabbitMQAdapterTest.class);

	private GlobalOptions _options;
	private RabbitMqAdapter _rmqAdapter;
	private SmiConsumer _consumer;
	private ConsumerOptions _consumerOptions;
	private IProducerModel _producer;

	private static final String testExchName = "TEST.Java.RabbitMQAdapterTestExchange";
	private static final String testQueueName = "TEST.Java.RabbitMQAdapterTestQueue";

	private ConnectionFactory _factory;
	private Connection _connection;
	private Channel _channel;
	
	protected void setUp() throws Exception {

		super.setUp();		
		_options = GlobalOptions.Load(true);
		
		_rmqAdapter = new RabbitMqAdapter(_options.RabbitOptions, "RabbitMQAdapterTests");
		_consumer = new SimpleConsumer();

		_consumerOptions = new ConsumerOptions();
		_consumerOptions.QueueName = testQueueName;
		_consumerOptions.QoSPrefetchCount = 1;
		_consumerOptions.AutoAck = false;
		
		ProducerOptions producerOptions = new ProducerOptions();
		producerOptions.ExchangeName = testExchName;

		_producer = _rmqAdapter.SetupProducer(producerOptions);
		
		// Declare exchange & queue for this test
		
        _factory = new ConnectionFactory();
        _factory.setHost(_options.RabbitOptions.RabbitMqHostName);
		_factory.setPort(_options.RabbitOptions.RabbitMqHostPort);
		_factory.setVirtualHost(_options.RabbitOptions.RabbitMqVirtualHost);
		_factory.setUsername(_options.RabbitOptions.RabbitMqUserName);
		_factory.setPassword(_options.RabbitOptions.RabbitMqPassword);

		_connection = _factory.newConnection();
		_channel = _connection.createChannel();
				
		_channel.exchangeDeclare(testExchName, "direct", false);
		_channel.queueDeclare(testQueueName, false, false, true, null);
		_channel.queueBind(testQueueName, testExchName, "");
	}

	protected void tearDown() throws Exception {
		
		// log.info("Tearing down tests ...");
		
		_rmqAdapter.Shutdown();

		_channel.exchangeDelete(testExchName);
		_channel.close();
		_connection.close();		
		
		super.tearDown();
	}

	public void testSendRecv() {
		// log.info("testing send / receive");
		// Set up subscription to receive messages
		// log.info("Starting our test consumer via the adapter");
		_rmqAdapter.StartConsumer(_consumerOptions, _consumer);

		// Send messages
		String testMessage = "PJG Hello world!";
		// log.info("Sending a message via our test producer:" + testMessage);
		SimpleMessage message = new SimpleMessage(testMessage);
		// _producer.SendMessage(testMessage, "");
		_producer.SendMessage(message, "", null);
		
		// Wait
		try {
			// log.info("Waiting...");
			TimeUnit.SECONDS.sleep(1);
		} catch (InterruptedException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

		// Check message received
		// log.info("Checking message was received");
		String receivedMessage = ((SimpleConsumer) _consumer).getMessage();
		// log.info("Received message content:" + receivedMessage);
		assert (receivedMessage.equals(testMessage));

		// Send messages
		testMessage = "This is the second message!";
		message.Message = testMessage;
		// log.info("Sending second message via our test producer:" + testMessage);
		_producer.SendMessage(message, "", null);

		// Wait
		try {
			// log.info("Waiting...");
			TimeUnit.SECONDS.sleep(1);
		} catch (InterruptedException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

		// Check second message received
		// log.info("Checking second message was received");
		receivedMessage = ((SimpleConsumer) _consumer).getMessage();
		// log.info("Received second message content:" + receivedMessage);
		assert (receivedMessage.equals(testMessage));

		// TODO check exception generated for empty message
	}

	/*
	 * public void testStartConsumer() { // fail("Not yet implemented"); // TODO }
	 * 
	 * public void testStartProducer() { // fail("Not yet implemented"); // TODO }
	 * 
	 * public void testShutdown() { // fail("Not yet implemented"); // TODO }
	 */
}
