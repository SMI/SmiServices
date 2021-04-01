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

		SmiLogging.Setup(false);
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
			.builder("m")
			.argName("modality")
			.desc("Extraction modality")
			.hasArg()
			.longOpt("modality")
			.build());

		options.addOption(
			Option        
			.builder("i")
			.type(boolean.class)
			.argName("identifiable extraction")
			.desc("This is an identifiable extraction")
			.longOpt("identifiable-extraction")
            .build());
            
		options.addOption(
			Option        
			.builder("f")
			.type(boolean.class)
			.argName("no-filters extraction")
			.desc("Extraction with no reject filters. True by default if --identifiable-extraction specified")
			.longOpt("no-filters-extraction")
			.build());
		
		try {
			commandLine = commLineParser.parse(options, args);
		} catch (ParseException e) {
			System.err.println(e.getMessage());
			printUsage(options);
			System.exit(1);
		}

		if (commandLine.getArgList().size() != 1) {
			printUsage(options);
			System.err.println("Need exactly one CSV file to process");
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
