package org.smi.ctpanonymiser.messaging;

import com.google.common.io.Files;
import com.google.gson.JsonSyntaxException;
import com.rabbitmq.client.AMQP.BasicProperties;
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
import java.nio.file.Paths;

public class CTPAnonymiserConsumer extends SmiConsumer {

	private final static Logger _logger = Logger.getRootLogger();
	
	private GlobalOptions _options;
	
	private String _routingKey_failure;
	private String _routingKey_success;

	private String _fileSystemRoot;
	private String _extractFileSystemRoot;

	private SmiCtpProcessor _anonTool;

	private IProducerModel _statusMessageProducer;

	private boolean _foundAFile = false;


	public CTPAnonymiserConsumer(GlobalOptions options, IProducerModel producer, SmiCtpProcessor anonTool, String fileSystemRoot,
								 String extractFileSystemRoot) {

		_routingKey_failure = options.CTPAnonymiserOptions.NoVerifyRoutingKey;
		_routingKey_success = options.CTPAnonymiserOptions.VerifyRoutingKey;

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

		if (extractFileMessage.IsIdentifiableExtraction) {
			// We should only receive these messages if the queue configuration is wrong, so ok just to crash-out
			String msg = "Received a message with IsIdentifiableExtraction set";
			_logger.error(msg);
			throw new RuntimeException(msg);
		}

		ExtractedFileStatusMessage statusMessage = new ExtractedFileStatusMessage(extractFileMessage);

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
			statusMessage.Status = ExtractedFileStatus.FileMissing;

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
			statusMessage.Status = ExtractedFileStatus.FileMissing;

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

		String routingKey;

		if (status == CtpAnonymisationStatus.Anonymised) {

			statusMessage.OutputFilePath = extractFileMessage.OutputPath;
			statusMessage.Status = ExtractedFileStatus.Anonymised;
			routingKey = _routingKey_success;

		} else {

			statusMessage.StatusMessage = _anonTool.getLastStatus();
			statusMessage.Status = ExtractedFileStatus.ErrorWontRetry;
			routingKey = _routingKey_failure;
		}

		_statusMessageProducer.SendMessage(statusMessage, routingKey, header);

		// Everything worked so acknowledge message
		AckMessage(envelope.getDeliveryTag());
	}
}
