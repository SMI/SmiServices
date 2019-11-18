package org.smi.extractorcl;

import org.apache.commons.cli.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.smi.common.logging.SmiLogging;
import org.smi.common.options.GlobalOptions;
import org.smi.extractorcl.execution.ExtractorClHost;

import java.util.UUID;

public class Program {

	public static void main(String[] args) throws Exception {

		SmiLogging.Setup();
		final Logger logger = LoggerFactory.getLogger(Program.class);

		CommandLine parsedArgs = ParseOptions(args);
		String yamlFileName = parsedArgs.getOptionValue("y", "default.yaml");
		GlobalOptions options = GlobalOptions.Load(yamlFileName.replaceAll(".yaml", ""));

		logger.debug("Loaded config options:\n" + options.toString());

		ExtractorClHost host = new ExtractorClHost(options, parsedArgs, UUID.randomUUID());

		try {

			host.process();

		} catch (Exception e) {

			logger.error("Exception in host process: " + e.getMessage());
			e.printStackTrace();

			System.exit(-1);
			return;
		}

		host.shutdown();

		logger.info("Host processing finished, exiting");
		System.exit(0);
	}

	private static CommandLine ParseOptions(String[] args) throws ParseException {

		Options options = new Options();

		// Set up help options only and parse
		options.addOption("h", "help", false, "Print this message and exit");

		CommandLineParser commLineParser = new DefaultParser();
		CommandLine commandLine = commLineParser.parse(options, args, true);

		// Add options - needed for both parsing command-line
		// options and displaying help

		Option option = Option
				.builder("y")
				.argName("config file")
				.hasArg()
				.longOpt("yaml-file")
				.desc("Name of the yaml file to load")
				.build();

		options.addOption(option);

		option = Option
				.builder("p")
				.argName("project identifier")
				.hasArg()
				.longOpt("project")
				.desc("Project identifier")
				.required()
				.build();

		options.addOption(option);

		option = Option
				.builder("e")
				.argName("extraction directory")
				.hasArg()
				.longOpt("subdirectory")
				.desc("Name of subdirectory within project folder to create the files. Defaults to the generated extraction identifier")
				.build();

		options.addOption(option);

		option = Option
				.builder("c")
				.argName("extraction key column")
				.hasArg()
				.longOpt("extraction-key-column")
				.desc("Column in the csv file containing the extraction key (0-based). Defaults to 0")
				.build();

		options.addOption(option);
		if (commandLine.hasOption("h"))
			printUsageAndExit(options, 0);

		try {

			commandLine = commLineParser.parse(options, args);

		} catch (ParseException e) {

			System.err.println(e.getMessage());
			printUsageAndExit(options, 1);
		}

		if (commandLine.getArgList().size() == 0) {
			System.err.println("No data files given to process");
			printUsageAndExit(options, 1);
		}

		return commandLine;
	}

	/**
	 * Print usage information and exit.
	 *
	 * @param options  Command-line options used to display options to user.
	 * @param exitCode Exit code.
	 */
	private static void printUsageAndExit(Options options, int exitCode) {

		new HelpFormatter().printHelp(ExtractorClHost.class.getName() + " [options] dataFile1 [dataFile2 ... dataFileN]", "Options:", options, "");

		System.exit(exitCode);
	}
}
