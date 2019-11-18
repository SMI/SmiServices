package org.smi.ctpanonymiser.test.execution;

import com.rabbitmq.client.Channel;
import com.rabbitmq.client.Connection;
import com.rabbitmq.client.ConnectionFactory;
import junit.framework.TestCase;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.smi.common.logging.SmiLogging;
import org.smi.common.messaging.AnyConsumer;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.options.ConsumerOptions;
import org.smi.common.options.GlobalOptions;
import org.smi.common.options.ProducerOptions;
import org.smi.common.rabbitMq.RabbitMqAdapter;
import org.smi.ctpanonymiser.Program;
import org.smi.ctpanonymiser.execution.CTPAnonymiserHost;
import org.smi.ctpanonymiser.messages.ExtractFileMessage;
import org.smi.ctpanonymiser.messages.ExtractFileStatusMessage;

import java.io.File;
import java.nio.file.Paths;
import java.util.UUID;
import java.util.concurrent.TimeUnit;

public class CTPAnonymiserHostTest extends TestCase {

	private static Logger _logger;

	private static final String _fsRoot = System.getProperty("user.dir") + "/src/test/resources";
	private static final String _extractRoot = System.getProperty("user.dir") + "/src/test/resources";

	private static final String _testFile = "image-000001.dcm";

	private static final String _inputExchName = "TEST.ExtractFileExchange";
	private static final String _outputQueueName = "TEST.FileStatusQueue";

	private static String _producerExchangeName;

	private ProducerOptions _extractFileProducerOptions;
	private ConsumerOptions _extractFileStatusConsumerOptions;
	private IProducerModel _extractFileMessageProducer;
	private AnyConsumer<ExtractFileStatusMessage> _anonFileStatusMessageConsumer;

	private ConnectionFactory _factory;
	private Connection _conn;
	private Channel _channel;

	private GlobalOptions _options;
	private RabbitMqAdapter _testAdapter;
	private CTPAnonymiserHost _ctpHost;

	protected void setUp() throws Exception {

		super.setUp();

		SmiLogging.Setup(0);
		_logger = LoggerFactory.getLogger(Program.class);

		_options = GlobalOptions.Load(true);

		_options.FileSystemOptions.setFileSystemRoot(_fsRoot);
		_options.FileSystemOptions.setExtractRoot(_extractRoot);

		if (!_options.CTPAnonymiserOptions.ExtractFileConsumerOptions.QueueName.startsWith("TEST."))
			_options.CTPAnonymiserOptions.ExtractFileConsumerOptions.QueueName = "TEST."
					+ _options.CTPAnonymiserOptions.ExtractFileConsumerOptions.QueueName;

		if (!_options.CTPAnonymiserOptions.ExtractFileStatusProducerOptions.ExchangeName.startsWith("TEST."))
			_options.CTPAnonymiserOptions.ExtractFileStatusProducerOptions.ExchangeName = "TEST."
					+ _options.CTPAnonymiserOptions.ExtractFileStatusProducerOptions.ExchangeName;

		String _consumerQueueName = _options.CTPAnonymiserOptions.ExtractFileConsumerOptions.QueueName;
		_producerExchangeName = _options.CTPAnonymiserOptions.ExtractFileStatusProducerOptions.ExchangeName;

		// Set up RMQ
		_testAdapter = new RabbitMqAdapter(_options.RabbitOptions, "CTPAnonymiserHostTest");

		// Set up test producers, consumers

		_extractFileStatusConsumerOptions = new ConsumerOptions();
		_extractFileStatusConsumerOptions.QueueName = _outputQueueName;
		_extractFileStatusConsumerOptions.AutoAck = false;
		_extractFileStatusConsumerOptions.QoSPrefetchCount = 1;

		_anonFileStatusMessageConsumer = new AnyConsumer<>(ExtractFileStatusMessage.class);

		_extractFileProducerOptions = new ProducerOptions();
		_extractFileProducerOptions.ExchangeName = _inputExchName;

		_extractFileMessageProducer = _testAdapter.SetupProducer(_extractFileProducerOptions);

		_factory = new ConnectionFactory();
		_conn = _factory.newConnection();
		_channel = _conn.createChannel();

		// Setup the input exch. / queue pair

		_channel.exchangeDeclare(_inputExchName, "direct", true);
		_channel.queueDeclare(_consumerQueueName, true, false, false, null);
		_channel.queueBind(_consumerQueueName, _inputExchName, "");
		System.out.println(String.format("Bound %s -> %s", _inputExchName, _consumerQueueName));

		// Setup the output exch. / queue pair

		_channel.exchangeDeclare(_producerExchangeName, "direct", true);
		_channel.queueDeclare(_outputQueueName, true, false, false, null);
		_channel.queueBind(_outputQueueName, _producerExchangeName, "");
		System.out.println(String.format("Bound %s -> %s", _producerExchangeName, _outputQueueName));

		_channel.queuePurge(_consumerQueueName);
		_channel.queuePurge(_outputQueueName);

		// Start our test consumer for receiving the anonymised message
		_testAdapter.StartConsumer(_extractFileStatusConsumerOptions, _anonFileStatusMessageConsumer);

		// Create the host for testing
		String[] args = new String[]{"-a", _fsRoot + "/dicom-anonymizer.script"};
		_ctpHost = new CTPAnonymiserHost(_options, Program.ParseOptions(args));

		File inFile = new File(Paths.get(_fsRoot, _testFile).toString());
		assertTrue(inFile.exists());

		boolean ok = inFile.setWritable(false);
		assertTrue(ok);
	}

	protected void tearDown() throws Exception {

		super.tearDown();

		_ctpHost.Shutdown();

		_testAdapter.Shutdown();

		_channel.exchangeDelete(_inputExchName);
		_channel.exchangeDelete(_producerExchangeName);

		_channel.close();
		_conn.close();
	}

	public void testBasicAnonymise_Success() throws InterruptedException
	{
		// Start the host
		Thread hostThread = new Thread(_ctpHost);
		hostThread.start();

		doTestBasicAnonymise_Success();
	}

	private void doTestBasicAnonymise_Success() throws InterruptedException {

		_logger.info("Starting basic anonymise test - should succeed");

		// Send a test message
		ExtractFileMessage exMessage = new ExtractFileMessage();

		exMessage.ExtractionJobIdentifier = UUID.randomUUID();
		exMessage.JobSubmittedAt = "";
		exMessage.ExtractionDirectory = "";
		exMessage.DicomFilePath = _testFile;
		exMessage.OutputPath = "AnonymisedFiles/" + exMessage.DicomFilePath;
		exMessage.ProjectNumber = "123-456";

		TimeUnit.MILLISECONDS.sleep(1000);

		_logger.info("Sending extract file message to " + _extractFileProducerOptions.ExchangeName);
		_extractFileMessageProducer.SendMessage(exMessage, "", null);

		_logger.info("Waiting...");

		int timeout = 10000;
		final int deltaMs = 1000;

		while (!_anonFileStatusMessageConsumer.isMessageValid() && timeout > 0) {

			TimeUnit.MILLISECONDS.sleep(deltaMs);
			timeout -= deltaMs;
		}

		if (timeout > 0) {
			_logger.info("... message received, took " + timeout + " milliseconds");
		} else {
			fail("Message not received in " + timeout + " milliseconds");
		}

		if (_anonFileStatusMessageConsumer.isMessageValid()) {

			ExtractFileStatusMessage recvd = _anonFileStatusMessageConsumer.getMessage();

			_logger.info("Message received");
			_logger.info("\n" + recvd.toString());

			assertEquals("FilePaths do not match", exMessage.OutputPath, recvd.AnonymisedFileName);
			assertEquals("Project numbers do not match", exMessage.ProjectNumber, recvd.ProjectNumber);
			assertEquals(0, recvd.Status);
		} else {
			fail("Did not receive message");
		}
	}

	public void testBasicAnonymise_Failure() throws InterruptedException {

		_logger.info("Starting basic anonymise test - failure hadling");

		// Start the host
		Thread hostThread = new Thread(_ctpHost);
		hostThread.start();

		doTestBasicAnonymise_Success();
		_logger.info("First message processed successfully");

		// Send an invalid message - should fail
		ExtractFileMessage exMessage = new ExtractFileMessage();
		exMessage.ExtractionJobIdentifier = UUID.randomUUID();
		exMessage.JobSubmittedAt = "";
		exMessage.ExtractionDirectory = "";
		exMessage.DicomFilePath = "missing.dcm";
		exMessage.OutputPath = "AnonymisedFiles/" + exMessage.DicomFilePath;
		exMessage.ProjectNumber = "123-456";

		_logger.info("Sending extract file message to " + _extractFileProducerOptions.ExchangeName);
		_extractFileMessageProducer.SendMessage(exMessage, "", null);

		_logger.info("Waiting...");

		int timeout = 10000;
		final int deltaMs = 1000;

		while (!_anonFileStatusMessageConsumer.isMessageValid() && timeout > 0) {

			TimeUnit.MILLISECONDS.sleep(deltaMs);
			timeout -= deltaMs;
		}

		if (timeout > 0) {
			_logger.info("... message received, took " + timeout + " milliseconds");
		} else {
			fail("Message not received in " + timeout + " milliseconds");
		}

		if (_anonFileStatusMessageConsumer.isMessageValid()) {

			ExtractFileStatusMessage recvd = _anonFileStatusMessageConsumer.getMessage();

			_logger.info("Message received");
			_logger.info("\n" + recvd.toString());

			assertEquals("FilePaths do not match", null, recvd.AnonymisedFileName);
			assertEquals(2, recvd.Status);
		} else {
			fail("Did not receive message");
		}
	}
}
