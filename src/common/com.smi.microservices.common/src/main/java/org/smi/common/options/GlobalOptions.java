
package org.smi.common.options;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.dataformat.yaml.YAMLFactory;
import org.yaml.snakeyaml.error.YAMLException;

import java.io.File;
import java.io.FileNotFoundException;
import java.net.URISyntaxException;
import java.nio.file.Path;
import java.nio.file.Paths;

public class GlobalOptions {

	public static GlobalOptions Load() throws FileNotFoundException, YAMLException, URISyntaxException {
		return Load("default", false);
	}

	public static GlobalOptions Load(boolean testing) throws FileNotFoundException, YAMLException, URISyntaxException {
		return Load("default", testing);
	}

	public static GlobalOptions Load(String environment) throws FileNotFoundException, YAMLException, URISyntaxException {
		return Load(environment, false);
	}

	public static GlobalOptions Load(String environment, boolean testing) throws FileNotFoundException, YAMLException, URISyntaxException {

		Path yamlConfigPath;

		try {
			if (testing) {
				yamlConfigPath = Paths.get("./target", environment + ".yaml");
			} else {
				yamlConfigPath = Paths.get(environment + ".yaml");
			}
		} catch (Exception e) {

			throw new FileNotFoundException("Could not find the config file " + environment + ".yaml");
		}

		GlobalOptions options;

		try {

			File config = new File(yamlConfigPath.toString());

			ObjectMapper mapper = new ObjectMapper(new YAMLFactory());
			mapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);

			options = mapper.readValue(config, GlobalOptions.class);

		} catch (Exception e) {

			throw new YAMLException("Exception parsing yaml config", e);
		}

		return options;
	}

	private GlobalOptions() {
	}

	@Override
	public String toString() {
		return "TODO";
	}

	// region All Options

	public RabbitOptions RabbitOptions;
	public FileSystemOptions FileSystemOptions;
	public CTPAnonymiserOptions CTPAnonymiserOptions;
	public ExtractorClOptions ExtractorClOptions;

	// endregion

	public class RabbitOptions {

		public String RabbitMqHostName;
		public int RabbitMqHostPort;
		public String RabbitMqVirtualHost;
		public String RabbitMqUserName;
		public String RabbitMqPassword;
		public String FatalLoggingExchange;
		public String RabbitMqControlExchangeName;

		public boolean Validate() {

			// TODO Validation
			// return !Utils.isNullOrWhitespace(RabbitMqHostName) &&
			return true;
		}
	}

	public class FileSystemOptions {

		private String FileSystemRoot;
		private String ExtractRoot;

		@JsonProperty("FileSystemRoot")
		public void setFileSystemRoot(String fileSystemRoot) {

			if (fileSystemRoot.endsWith("/") || fileSystemRoot.endsWith("\\"))
				fileSystemRoot = fileSystemRoot.substring(0, fileSystemRoot.length() - 1);

			FileSystemRoot = fileSystemRoot;
		}

		public String getFileSystemRoot() {
			return FileSystemRoot;
		}

		@JsonProperty("ExtractRoot")
		public void setExtractRoot(String extractRoot) {

			if (extractRoot.endsWith("/") || extractRoot.endsWith("\\"))
				extractRoot = extractRoot.substring(0, extractRoot.length() - 1);

			ExtractRoot = extractRoot;
		}

		public String getExtractRoot() {
			return ExtractRoot;
		}
	}

	public class CTPAnonymiserOptions {

		public ConsumerOptions AnonFileConsumerOptions;
		public ProducerOptions ExtractFileStatusProducerOptions;
	}

	public class ExtractorClOptions {

		public int MaxIdentifiersPerMessage = 1000;
		public ProducerOptions ExtractionRequestProducerOptions;
		public ProducerOptions ExtractionRequestInfoProducerOptions;
	}
}
