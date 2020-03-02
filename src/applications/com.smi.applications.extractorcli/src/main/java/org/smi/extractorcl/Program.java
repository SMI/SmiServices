package org.smi.extractorcl;

import org.apache.commons.cli.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.smi.common.logging.SmiLogging;
import org.smi.common.options.GlobalOptions;
import org.smi.extractorcl.exceptions.FileProcessingException;
import org.smi.extractorcl.exceptions.LineProcessingException;
import org.smi.extractorcl.execution.ExtractorClHost;
import org.yaml.snakeyaml.error.YAMLException;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.net.URISyntaxException;
import java.util.UUID;
import java.util.concurrent.TimeoutException;

public class Program {

	public static void main(String[] args)
			throws FileNotFoundException, FileProcessingException, LineProcessingException, IOException, ParseException,
			YAMLException, URISyntaxException, IllegalArgumentException, TimeoutException {

		SmiLogging.Setup();
		final Logger logger = LoggerFactory.getLogger(Program.class);

		CommandLine parsedArgs = ParseOptions(args);
		String yamlFileName = parsedArgs.getOptionValue("y", "default.yaml");
		GlobalOptions options = GlobalOptions.Load(yamlFileName.replaceAll(".yaml", ""));

		logger.debug("Loaded config options:\n" + options.toString());

		ExtractorClHost host = new ExtractorClHost(options, parsedArgs, UUID.randomUUID());
		
		// TODO(rkm 2020-01-30) Something funky going on here with threads not exiting cleanly
		try {
			host.process();
		} catch (Exception e) {
			logger.error("Exception in host process: " + e.getMessage());
			e.printStackTrace();
			host.shutdown();
			System.exit(1);
		}
		
		host.shutdown();
		System.exit(0);
	}

	private static CommandLine ParseOptions(String[] args) throws ParseException {

		Options options = new Options();

		options.addOption("h", "help", false, "Print this message and exit");

		CommandLineParser commLineParser = new DefaultParser();
		CommandLine commandLine = commLineParser.parse(options, args, true);

		if (commandLine.hasOption("h")) {
			printUsage(options);
			System.exit(0);
		}

		options.addOption(
			Option
			.builder("y")
			.desc("Name of the yaml file to load")
			.hasArg()
			.argName("config file")
			.longOpt("yaml-file")
			.build());

		options.addOption(
			Option
			.builder("p")
			.argName("project identifier")
			.desc("[Required] Project identifier")
			.hasArg()
			.longOpt("project")
			.required()
			.build());

		options.addOption(
			Option
			.builder("e")
			.argName("extraction directory")
			.desc("Name of subdirectory within project folder to create the files. Defaults to the generated extraction identifier")
			.hasArg()
			.longOpt("subdirectory")
			.build());

		options.addOption(
			Option        
			.builder("m")
			.argName("modality")
			.desc("Extraction modality. Should only be specified if extracting at the Study level")
			.hasArg()
			.longOpt("modality")
			.build());
		
		try {
			commandLine = commLineParser.parse(options, args);
		} catch (ParseException e) {
			System.err.println(e.getMessage());
			printUsage(options);
			System.exit(1);
		}

		if (commandLine.getArgList().size() == 0) {
			printUsage(options);
			System.err.println("No data files given to process");
			System.exit(1);
		}

		return commandLine;
	}

	/**
	 * Print usage information
	 *
	 * @param options Command-line options used to display options to user
	 */
	private static void printUsage(Options options) {
		new HelpFormatter().printHelp("[options] <id file>...", "Options:", options, "");
	}
}
