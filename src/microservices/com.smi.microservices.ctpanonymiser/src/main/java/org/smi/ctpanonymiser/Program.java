
package org.smi.ctpanonymiser;

import java.io.IOException;
import java.net.URISyntaxException;
import java.util.concurrent.TimeoutException;

import org.apache.commons.cli.CommandLine;
import org.apache.commons.cli.CommandLineParser;
import org.apache.commons.cli.DefaultParser;
import org.apache.commons.cli.HelpFormatter;
import org.apache.commons.cli.Option;
import org.apache.commons.cli.Options;
import org.apache.commons.cli.ParseException;
import org.apache.log4j.Logger;
import org.smi.common.execution.SmiShutdownHook;
import org.smi.common.logging.SmiLogging;
import org.smi.common.options.GlobalOptions;
import org.smi.ctpanonymiser.execution.CTPAnonymiserHost;
import org.yaml.snakeyaml.error.YAMLException;

/*
 * Program entry point when run from the command line
 */
public class Program {

	public static void main(String[] args) throws ParseException, YAMLException, URISyntaxException, IOException, TimeoutException {
		
		SmiLogging.Setup(false);
		Logger logger = Logger.getRootLogger();

		//TODO Make this into a helper class
		CommandLine parsedArgs = ParseOptions(args);
		String yamlFileName = parsedArgs.getOptionValue("y", "default.yaml");
		GlobalOptions options = GlobalOptions.Load(yamlFileName.replaceAll(".yaml", ""));

		logger.debug("Loaded config options:\n" + options.toString());
		
		logger.debug("creating host");
		CTPAnonymiserHost host = new CTPAnonymiserHost(options, parsedArgs);

		Runtime.getRuntime().addShutdownHook(new SmiShutdownHook(host));
	}

	public static CommandLine ParseOptions(String[] args) throws ParseException {

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
				.builder("a")
				.argName("file")
				.hasArg()
				.longOpt("anon")
				.desc("Anonymisation script")
				.required()
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

		return commandLine;
	}

	/**
	 * Print usage information and exit.
	 * 
	 * @param options
	 *            Command-line options used to display options to user.
	 * @param exitCode
	 *            Exit code.
	 */
	private static void printUsageAndExit(Options options, int exitCode) {

		HelpFormatter formatter = new HelpFormatter();

		formatter.printHelp(CTPAnonymiserHost.class.getName() + " [options] dataFile1 [dataFile2 ... dataFileN]", "Options:", options, "");

		System.exit(exitCode);
	}
}
