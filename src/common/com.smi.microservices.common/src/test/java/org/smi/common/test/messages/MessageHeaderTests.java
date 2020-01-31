
package org.smi.common.test.messages;

import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.UUID;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

import org.junit.Ignore;
import org.smi.common.messages.MessageHeader;
import org.smi.common.messages.MessageHeaderFactory;
import org.smi.common.messages.SimpleMessage;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.messaging.SmiConsumer;
import org.smi.common.options.ConsumerOptions;
import org.smi.common.options.GlobalOptions;
import org.smi.common.options.ProducerOptions;
import org.smi.common.rabbitMq.RabbitMqAdapter;

import com.rabbitmq.client.AMQP.BasicProperties;
import com.rabbitmq.client.Channel;
import com.rabbitmq.client.Connection;
import com.rabbitmq.client.ConnectionFactory;
import com.rabbitmq.client.Envelope;

import junit.extensions.TestSetup;
import junit.framework.Test;
import junit.framework.TestCase;
import junit.framework.TestSuite;

public class MessageHeaderTests extends TestCase {

	private static MessageHeaderFactory _factory;

	private static Connection _conn;
	private static Channel _ch;

	private static final String ExchangeName = "MessageHeaderTestsExchange";
	private static final String QueueName = "MessageHeaderTestsQueue";

	public static Test suite() {

		TestSetup setup = new TestSetup(new TestSuite(MessageHeaderTests.class)) {

			protected void setUp() throws Exception {

				_factory = new MessageHeaderFactory("MessageHeaderTests", 1234);

				GlobalOptions options = GlobalOptions.Load(true);
				ConnectionFactory conFactory = new ConnectionFactory();
				conFactory.setHost(options.RabbitOptions.RabbitMqHostName);
				conFactory.setPort(options.RabbitOptions.RabbitMqHostPort);
				conFactory.setVirtualHost(options.RabbitOptions.RabbitMqVirtualHost);
				conFactory.setUsername(options.RabbitOptions.RabbitMqUserName);
				conFactory.setPassword(options.RabbitOptions.RabbitMqPassword);

				// Travis debug
				StringBuilder sb = new StringBuilder();
				sb.append("RabbitMQ settings:");
				sb.append("    HostName:                       " + conFactory.getHost() + System.lineSeparator());
				sb.append("    Port:                           " + conFactory.getPort() + System.lineSeparator());
				sb.append("    UserName:                       " + conFactory.getUsername() + System.lineSeparator());
				sb.append("    VirtualHost:                    " + conFactory.getVirtualHost() + System.lineSeparator());
				sb.append("    HandshakeContinuationTimeout:   " + conFactory.getHandshakeTimeout()	+ System.lineSeparator());
				System.out.println(sb);

				_conn = conFactory.newConnection("JavaTestConnection");
				_ch = _conn.createChannel();

				System.out.println("Declaring test exchange / queue pair");

				_ch.exchangeDeclare(ExchangeName, "direct", true);
				_ch.queueDeclare(QueueName, true, false, false, null);
				_ch.queueBind(QueueName, ExchangeName, "");
			}

			protected void tearDown() throws Exception {

				System.out.println("Deleting test exchange / queue pair");

				_ch.exchangeDelete(ExchangeName);
				_ch.queueDelete(QueueName);

				_ch.close();
				_conn.close();
			}
		};

		return setup;
	}

	@Ignore
	public void testGetHeaderNoParents() {

		MessageHeader header = _factory.getHeader(null);

		assertNotNull(header.getMessageGuid());
		assertEquals(header.getProducerExecutableName(), "MessageHeaderTests");
		assertEquals(header.getProducerProcessID(), 1234);
		assertEquals(header.getParents().length, 0);
	}

	@Ignore
	public void testGetHeaderWithParents() {

		MessageHeader parentHeader = _factory.getHeader(null);
		MessageHeader header = _factory.getHeader(parentHeader);

		assertEquals(header.getParents().length, 1);
		assertEquals(header.getParents()[0], parentHeader.getMessageGuid());
	}

	@Ignore
	public void testPopulateHeaders() {

		MessageHeader h1 = _factory.getHeader(null);
		MessageHeader h2 = _factory.getHeader(h1);
		MessageHeader h3 = _factory.getHeader(h2);

		HashMap<String, Object> headerMap = new HashMap<String, Object>();
		h3.Populate(headerMap);

		assertEquals(headerMap.size(), 5);

		assertEquals(headerMap.get("MessageGuid").toString(), h3.getMessageGuid().toString());
		assertEquals(headerMap.get("ProducerExecutableName"), h3.getProducerExecutableName());
		assertEquals(headerMap.get("ProducerProcessID"), h3.getProducerProcessID());

		String expectedParents = h1.getMessageGuid() + "->" + h2.getMessageGuid();
		assertEquals(headerMap.get("Parents"), expectedParents);
	}

	@Ignore
	public void testConstructFromHashMap() {

		UUID testUUID = UUID.randomUUID();

		HashMap<String, Object> headerMap = new HashMap<String, Object>();

		headerMap.put("MessageGuid", testUUID.toString().getBytes(StandardCharsets.UTF_8));
		headerMap.put("ProducerExecutableName", "MessageHeaderTests".getBytes(StandardCharsets.UTF_8));
		headerMap.put("ProducerProcessID", 1234);
		headerMap.put("RetryCount", 5678);
		headerMap.put("Parents", "");

		// TODO Fix this test
		// System.out.println(((byte[]) headerMap.get("MessageGuid")).length);
		// MessageHeader header = _factory.getHeader(headerMap, StandardCharsets.UTF_8);
	}

	public void testHeaderRoundTrip() throws InterruptedException, URISyntaxException, IOException, TimeoutException {

		GlobalOptions options = GlobalOptions.Load(true);

		RabbitMqAdapter adapter = new RabbitMqAdapter(options.RabbitOptions, "MessageHeaderTests");

		ConsumerOptions consumerOptions = new ConsumerOptions();
		consumerOptions.QueueName = QueueName;
		consumerOptions.QoSPrefetchCount = 1;
		consumerOptions.AutoAck = true;

		SimpleConsumer consumer = new SimpleConsumer();
		adapter.StartConsumer(consumerOptions, consumer);

		ProducerOptions producerOptions = new ProducerOptions();
		producerOptions.ExchangeName = ExchangeName;

		IProducerModel producerModel = adapter.SetupProducer(producerOptions);

		SimpleMessage message = new SimpleMessage("testHeaderRoundTrip");

		if (_factory == null) {
			fail();
		}

		MessageHeader header = _factory.getHeader(null);
		producerModel.SendMessage(message, "", header);

		int timeout = 10;

		while (timeout > 0 && consumer.getReceived() == 0) {

			System.out.println("Sleeping!");

			TimeUnit.SECONDS.sleep(1);
			--timeout;
		}

		adapter.Shutdown();
	}

	private class SimpleConsumer extends SmiConsumer {

		public int Received;

		@Override
		public void handleDeliveryImpl(String consumerTag, Envelope envelope, BasicProperties properties, byte[] body,
				MessageHeader header) throws IOException {

			System.out.println("Message received!");
			System.out.println("--- Header ---");
			System.out.println(header);
			System.out.println("--- ---");

			++Received;
		}

		public int getReceived() {
			return Received;
		}

	}
}
