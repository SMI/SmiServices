package org.smi.ctpanonymiser.execution;

import org.apache.commons.cli.CommandLine;
import org.apache.log4j.Logger;
import org.smi.common.execution.IMicroserviceHost;
import org.smi.common.messaging.IProducerModel;
import org.smi.common.options.GlobalOptions;
import org.smi.common.rabbitMq.RabbitMqAdapter;
import org.smi.ctpanonymiser.messaging.CTPAnonymiserConsumer;
import org.smi.ctpanonymiser.util.DicomAnonymizerToolBuilder;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.file.Paths;
import java.util.concurrent.TimeoutException;

public class CTPAnonymiserHost implements IMicroserviceHost {

	private static final Logger _logger = Logger.getRootLogger();

	private final RabbitMqAdapter _rabbitMqAdapter;
	private final CTPAnonymiserConsumer _consumer;
	private IProducerModel _producer;

private final GlobalOptions _options;

	public CTPAnonymiserHost(GlobalOptions options, CommandLine cliOptions) throws IOException, TimeoutException {

		_options = options;

		_logger.trace("Setting up CTPAnonymiserHost");

		File anonScriptFile = new File(Paths.get(cliOptions.getOptionValue("a")).toString());
		_logger.debug("anonScriptFile: " + anonScriptFile.getPath());

		if (!anonScriptFile.exists() || anonScriptFile.isDirectory())
			throw new IllegalArgumentException("Cannot find anonymisation script file: " + anonScriptFile.getPath());

		String fsRoot = options.FileSystemOptions.getFileSystemRoot();
		if (!CheckValidDirectory(fsRoot)) {
			throw new FileNotFoundException("Given filesystem root is not valid: " + fsRoot);
		}

		String exRoot = options.FileSystemOptions.getExtractRoot();
		if (!CheckValidDirectory(exRoot)) {
			throw new FileNotFoundException("Given extraction root is not valid: " + exRoot);
		}

		_rabbitMqAdapter = new RabbitMqAdapter(options.RabbitOptions, "CTPAnonymiserHost");
		_logger.debug("Connected to RabbitMQ server version " + _rabbitMqAdapter.getRabbitMqServerVersion());

		try {

			_producer = _rabbitMqAdapter.SetupProducer(options.CTPAnonymiserOptions.ExtractFileStatusProducerOptions);

		} catch (IllegalArgumentException e) {

			e.printStackTrace();
		}

		// Build the SMI Anonymiser tool
		SmiCtpProcessor anonTool = new DicomAnonymizerToolBuilder().tagAnonScriptFile(anonScriptFile).check(null).buildDat();

		_consumer = new CTPAnonymiserConsumer(
				_options,
				_producer,
				anonTool,
				fsRoot,
				exRoot);

		_logger.info("CTPAnonymiserHost created successfully");

		// Start the consumer
		_rabbitMqAdapter.StartConsumer(_options.CTPAnonymiserOptions.AnonFileConsumerOptions, _consumer);
	}

	public IProducerModel getProducer() {
		return _producer;
	}

	public void Shutdown() {

		_logger.info("Host shutdown called");

		_rabbitMqAdapter.Shutdown();
	}

	private boolean CheckValidDirectory(String path) {

		File f = new File(Paths.get(path).toString());

		return (f.exists() && f.isDirectory() && f.canRead());
	}
}
