
package org.smi.common.logging;

import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Static helper class to setup the SMI logging
 */
public final class SmiLogging {

	private static final String ConfigFileName = "SmiLogbackConfig.xml";

	private SmiLogging() {
	}

	public static void Setup() {
		Setup(-1);
	}

	public static void Setup(int env) throws SmiLoggingException {

		// Turn off log4j warnings from library code
		org.apache.log4j.Logger.getRootLogger().setLevel(org.apache.log4j.Level.OFF);

		Path logConfigPath;

		if (env == -1) {

			logConfigPath = Paths.get(ConfigFileName);
			
		} else {

			try {
				logConfigPath = Paths.get(SmiLogging.class.getClass().getResource("/" + ConfigFileName).toURI());

			} catch (URISyntaxException e) {

				throw new SmiLoggingException("", e);
			}
		}

		if (Files.notExists(logConfigPath) || Files.isDirectory(logConfigPath))
			throw new SmiLoggingException("Could not find logback config file " + ConfigFileName);

		System.setProperty("logback.configurationFile", ConfigFileName);
	}
}
