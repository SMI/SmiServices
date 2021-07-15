package org.smi.ctpanonymiser.messaging;

import com.rabbitmq.client.AMQP.BasicProperties;
import com.rabbitmq.client.Channel;
import com.rabbitmq.client.Envelope;

import org.apache.log4j.Logger;
import org.smi.common.messages.MessageHeader;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.messaging.SmiConsumer;
import org.smi.ctpanonymiser.execution.SmiCtpProcessor;
import org.smi.ctpanonymiser.messages.ExtractFileMessage;
import org.smi.ctpanonymiser.messages.ExtractedFileStatusMessage;
import org.smi.ctpanonymiser.util.CtpAnonymisationStatus;
import org.smi.common.options.GlobalOptions;
import org.smi.ctpanonymiser.util.ExtractedFileStatus;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;

public class CTPAnonymiserConsumer extends SmiConsumer<ExtractFileMessage> {

	private final static Logger _logger = Logger.getRootLogger();
	
	private String _routingKey_failure;
	private String _routingKey_success;

	private String _fileSystemRoot;
	private String _extractFileSystemRoot;

	private SmiCtpProcessor _anonTool;

	private IProducerModel _statusMessageProducer;

	private boolean _foundAFile = false;


	public CTPAnonymiserConsumer(GlobalOptions options, IProducerModel producer, SmiCtpProcessor anonTool, String fileSystemRoot,
								 String extractFileSystemRoot, Channel channel) {
		super(channel,ExtractFileMessage.class);
		_routingKey_failure = options.CTPAnonymiserOptions.NoVerifyRoutingKey;
		_routingKey_success = options.CTPAnonymiserOptions.VerifyRoutingKey;

		_statusMessageProducer = producer;
		_anonTool = anonTool;
		_fileSystemRoot = fileSystemRoot;
		_extractFileSystemRoot = extractFileSystemRoot;
	}

	@Override
	public void handleDeliveryImpl(String consumerTag, Envelope envelope, BasicProperties properties, ExtractFileMessage body, MessageHeader header)
			throws IOException {
		if (body.IsIdentifiableExtraction) {
			// We should only receive these messages if the queue configuration is wrong, so ok just to crash-out
			String msg = "Received a message with IsIdentifiableExtraction set";
			_logger.error(msg);
			throw new RuntimeException(msg);
		}

		ExtractedFileStatusMessage statusMessage = new ExtractedFileStatusMessage(body);

		// Got the message, now apply the anonymisation
		File sourceFile = new File(body.getAbsolutePathToIdentifiableImage(_fileSystemRoot));

		if (!sourceFile.exists()) {
			String msg = "Dicom file to anonymise does not exist: " + sourceFile.getAbsolutePath() + ". Cannot output to "
					+ body.OutputPath;

			_logger.error(msg);

			if (!_foundAFile) {
				_logger.error("First message has failed, possible environment error. Check the filesystem root / permissions are correct. Re-queueing the message and shutting down...");
				throw new FileNotFoundException("Could not find file for first message");
			}

			statusMessage.StatusMessage = msg;
			statusMessage.OutputFilePath = "";
			statusMessage.Status = ExtractedFileStatus.FileMissing;

			_statusMessageProducer.SendMessage(statusMessage, _routingKey_failure, header);
			AckMessage(envelope.getDeliveryTag());
			return;
		}

		_foundAFile = true;

		File outFile = new File(body.getExtractionOutputPath(_extractFileSystemRoot));
		File outDirectory = outFile.getParentFile();

		if (!outDirectory.exists()) {
			_logger.debug("Creating output directory " + outDirectory);
			if (!outDirectory.mkdirs() && !outDirectory.exists()) {
				throw new FileNotFoundException("Could not create the output directory " + outDirectory.getAbsolutePath());
			}
		}

		_logger.debug("Extracting to file: " + outFile.getAbsolutePath());

		CtpAnonymisationStatus status = _anonTool.anonymize(sourceFile, outFile);
		_logger.debug("SmiCtpProcessor returned " + status);

		String routingKey;

		if (status == CtpAnonymisationStatus.Anonymised) {
			statusMessage.StatusMessage = "";
			statusMessage.OutputFilePath = body.OutputPath;
			statusMessage.Status = ExtractedFileStatus.Anonymised;
			routingKey = _routingKey_success;
		} else {
			statusMessage.StatusMessage = _anonTool.getLastStatus();
			statusMessage.OutputFilePath = "";
			statusMessage.Status = ExtractedFileStatus.ErrorWontRetry;
			routingKey = _routingKey_failure;
		}

		_statusMessageProducer.SendMessage(statusMessage, routingKey, header);

		// Everything worked so acknowledge message
		AckMessage(envelope.getDeliveryTag());

		// Finally force a garbage collection run to avoid wasting host RAM
		System.gc();
	}
}
