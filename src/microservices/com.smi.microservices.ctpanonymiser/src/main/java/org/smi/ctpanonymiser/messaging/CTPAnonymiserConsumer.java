package org.smi.ctpanonymiser.messaging;

import com.google.common.io.Files;
import com.google.gson.JsonSyntaxException;
import com.rabbitmq.client.AMQP.BasicProperties;
import com.rabbitmq.client.Envelope;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.smi.common.messages.MessageHeader;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.messaging.SmiConsumer;
import org.smi.ctpanonymiser.execution.SmiCtpProcessor;
import org.smi.ctpanonymiser.messages.ExtractFileMessage;
import org.smi.ctpanonymiser.messages.ExtractFileStatusMessage;
import org.smi.ctpanonymiser.util.CtpAnonymisationStatus;
import org.smi.ctpanonymiser.util.ExtractFileStatus;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.file.Paths;

public class CTPAnonymiserConsumer extends SmiConsumer {

	private final static Logger _logger = LoggerFactory.getLogger(CTPAnonymiserConsumer.class);
	private final static String _routingKey_failure = "failure";
	private final static String _routingKey_success = "success";
	private String _fileSystemRoot;
	private String _extractFileSystemRoot;

	private SmiCtpProcessor _anonTool;

	private IProducerModel _statusMessageProducer;

	private boolean _foundAFile = false;


	public CTPAnonymiserConsumer(IProducerModel producer, SmiCtpProcessor anonTool, String fileSystemRoot,
								 String extractFileSystemRoot) {

		_statusMessageProducer = producer;
		_anonTool = anonTool;
		_fileSystemRoot = fileSystemRoot;
		_extractFileSystemRoot = extractFileSystemRoot;
	}

	@Override
	public void handleDeliveryImpl(String consumerTag, Envelope envelope, BasicProperties properties, byte[] body, MessageHeader header)
			throws IOException {

		ExtractFileMessage extractFileMessage;

		try {

			extractFileMessage = getMessageFromBytes(body, ExtractFileMessage.class);
			_logger.debug("ExtractFileMessage received:\n" + extractFileMessage.toReadableText());

		} catch (JsonSyntaxException e) {

			// Problem with the message, so Nack it
			_logger.error("Problem with message, so it will be Nacked:" + e.getMessage());

			NackMessage(envelope.getDeliveryTag());
			return;
		}

		ExtractFileStatusMessage statusMessage = new ExtractFileStatusMessage(extractFileMessage);

		// Got the message, now apply the anonymisation

		File sourceFile = new File(extractFileMessage.getAbsolutePathToIdentifiableImage(_fileSystemRoot));

		if (!sourceFile.exists()) {

			String msg = "Dicom file to anonymise does not exist: " + sourceFile.getAbsolutePath() + ". Cannot output to "
					+ extractFileMessage.OutputPath;

			_logger.error(msg);

			if (!_foundAFile) {
				_logger.error("First message has failed, possible environment error. Check the filesystem root / permissions are correct. Re-queueing the message and shutting down...");
				throw new FileNotFoundException("Could not find file for first message");
			}

			statusMessage.StatusMessage = msg;
			statusMessage.Status = ExtractFileStatus.ErrorWontRetry;

			_statusMessageProducer.SendMessage(statusMessage, _routingKey_failure, header);

			AckMessage(envelope.getDeliveryTag());
			return;
		}

		_foundAFile = true;

		File outFile = new File(extractFileMessage.getExtractionOutputPath(_extractFileSystemRoot));
		File outDirectory = outFile.getParentFile();

		if (!outDirectory.exists()) {

			_logger.debug("Creating output directory " + outDirectory);

			if (!outDirectory.mkdirs() && !outDirectory.exists()) {
				throw new FileNotFoundException("Could not create the output directory " + outDirectory.getAbsolutePath());
			}
		}

		// Create a temp. file for CTP to use

		File tempFile = new File(Paths.get(outFile.getParent(), "tmp_" + outFile.getName()).toString());

		_logger.debug("Copying source file to " + tempFile.getAbsolutePath());
		Files.copy(sourceFile, tempFile);
		tempFile.setWritable(false);

		if (!tempFile.exists()) {

			String msg = "Temp file to anonymise was not created: " + tempFile.getAbsolutePath();
			_logger.error(msg);

			statusMessage.StatusMessage = msg;
			statusMessage.Status = ExtractFileStatus.ErrorWontRetry;

			_statusMessageProducer.SendMessage(statusMessage, _routingKey_failure, header);

			AckMessage(envelope.getDeliveryTag());
			return;
		}

		_logger.debug("Extracting to file: " + outFile.getAbsolutePath());

		CtpAnonymisationStatus status = _anonTool.anonymize(tempFile, outFile);
		_logger.debug("SmiCtpProcessor returned " + status);

		_logger.debug("Deleting temp file");
		if (!tempFile.delete() || tempFile.exists())
			_logger.warn("Could not delete temp file " + tempFile.getAbsolutePath());

		String routingKey = _routingKey_failure;

		if (status == CtpAnonymisationStatus.Anonymised) {

			statusMessage.AnonymisedFileName = extractFileMessage.OutputPath;
			statusMessage.Status = ExtractFileStatus.Anonymised;
			routingKey = _routingKey_success;

		} else {

			statusMessage.StatusMessage = _anonTool.getLastStatus();
			statusMessage.Status = ExtractFileStatus.ErrorWontRetry;
			routingKey = _routingKey_failure;
		}

		_statusMessageProducer.SendMessage(statusMessage, routingKey, header);

		// Everything worked so acknowledge message
		AckMessage(envelope.getDeliveryTag());
	}
}
