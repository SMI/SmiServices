package org.smi.extractorcl.fileUtils;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.smi.common.messaging.IProducerModel;
import org.smi.extractorcl.exceptions.LineProcessingException;
import org.smi.extractorcl.execution.ExtractionKey;
import org.smi.extractorcl.messages.ExtractionRequestInfoMessage;
import org.smi.extractorcl.messages.ExtractionRequestMessage;

import java.time.Instant;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.*;
import java.util.regex.Pattern;

/**
 * Handles events from the CSV parser and constructs the Extract Request Message
 * and Extract Request Info Message.
 */
public class ExtractMessagesCsvHandler implements CsvHandler {

	private static Logger _logger = LoggerFactory.getLogger(ExtractMessagesCsvHandler.class);

	private UUID _extractionJobID;
	private HashSet<String> _identifierSet = new HashSet<>();
	private String _projectID;
	private String _extractionDir;
	private ExtractionKey _extractionKey;
	private int _extractionKeyColumnIndex;
	private static final Pattern _chiPattern=Pattern.compile("^\\d{10}$");
	private static final Pattern _eupiPattern = Pattern.compile("^([A-Z]|[0-9]){32}$");
	private IProducerModel _extractRequestMessageProducerModel;
	private IProducerModel _extractRequestInfoMessageProducerModel;

	/**
	 * Constructor
	 *
	 * @param extractionJobID                        Extraction job ID
	 * @param projectID                              Project ID
	 * @param extractionDir                          Folder to write extracted DICOM files
	 * @param extractionKeyColumnIndex               Index of series ID column (0 based)
	 * @param extractRequestMessageProducerModel     Producer model used to write ExtractRequest messages
	 * @param extractRequestInfoMessageProducerModel Producer model used to write ExtractRequestInfo messages
	 */
	public ExtractMessagesCsvHandler(UUID extractionJobID, String projectID, String extractionDir,
									 int extractionKeyColumnIndex, IProducerModel extractRequestMessageProducerModel,
									 IProducerModel extractRequestInfoMessageProducerModel) {

		_extractionJobID = extractionJobID;
		_projectID = projectID;
		_extractionDir = extractionDir;
		_extractionKeyColumnIndex = extractionKeyColumnIndex;
		_extractRequestMessageProducerModel = extractRequestMessageProducerModel;
		_extractRequestInfoMessageProducerModel = extractRequestInfoMessageProducerModel;

		// TODO Use regexes to detect what looks like CHI numbers
	}

	@Override
	public void processHeader(String[] header) throws LineProcessingException {

		if (_extractionKeyColumnIndex >= header.length) {

			String errorMessage = String.format(
					"Data header line has fewer columns (%d) than the series ID column index (%d)",
					header.length,
					_extractionKeyColumnIndex + 1);

			throw new LineProcessingException(1, header, errorMessage);
		}

		parseExtractionKey(header[_extractionKeyColumnIndex]);
		_logger.debug("extractionKey: " + _extractionKey);
	}

	@Override
	public void processLine(int lineNum, String[] line) throws LineProcessingException {

		if (_extractionKeyColumnIndex >= line.length) {
			String errorMessage = String
					.format("Line has fewer columns (%d) than the series ID column index (%d)", line.length, _extractionKeyColumnIndex + 1);

			throw new LineProcessingException(lineNum, line, errorMessage);
		}

		_identifierSet.add(line[_extractionKeyColumnIndex]);
	}

	@Override
	public void finished() {

	}

	public void sendMessages(boolean autoRun) throws IllegalArgumentException {
		sendMessages(autoRun, 1000);
	}

	public void sendMessages(int maxIdentifiersPerMessage) throws IllegalArgumentException {
		sendMessages(false, maxIdentifiersPerMessage);
	}

	/**
	 * Sends the messages to the message queue.
	 */
	public void sendMessages(boolean autoRun, int maxIdentifiersPerMessage) throws IllegalArgumentException {

		if (maxIdentifiersPerMessage < 1000) {
			throw new IllegalArgumentException(String.format("MaxIdentifiersPerMessage must be at least 1000 (given %s)", maxIdentifiersPerMessage));
		}

		if (_identifierSet.isEmpty()) {
			_logger.error("No identifiers added");
			return;
		}

		String now = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mmX").withZone(ZoneOffset.UTC).format(Instant.now());

		// Split identifiers into subsets

		List<Set<String>> split = new ArrayList<>();
		Set<String> subset = null;

		for (String value : _identifierSet) {
			if (subset == null || subset.size() == maxIdentifiersPerMessage)
				split.add(subset = new HashSet<>());
			subset.add(value);
		}

		ExtractionRequestMessage erm = new ExtractionRequestMessage();
		erm.ExtractionJobIdentifier = _extractionJobID;
		erm.ProjectNumber = _projectID;
		erm.ExtractionDirectory = _extractionDir;
		erm.JobSubmittedAt = now;
		erm.KeyTag = _extractionKey.toString();

		// Only need to send 1 of these
		ExtractionRequestInfoMessage erim = new ExtractionRequestInfoMessage();
		erim.ExtractionJobIdentifier = _extractionJobID;
		erim.ProjectNumber = _projectID;
		erim.ExtractionDirectory = _extractionDir;
		erim.JobSubmittedAt = now;
		erim.KeyValueCount = _identifierSet.size();
		erim.KeyTag = _extractionKey.toString();

		System.out.println("ExtractionJobIdentifier:\t" + _extractionJobID);
		System.out.println("ProjectNumber:\t\t\t\t" + _projectID);
		System.out.println("ExtractionDirectory:\t\t" + _extractionDir);
		System.out.println("ExtractionKey:\t\t\t\t" + _extractionKey);
		System.out.println("KeyValueCount:\t\t\t\t" + _identifierSet.size());
		System.out.println("Number of ExtractionRequestMessage(s):\t" + split.size());

		if (!autoRun) {

			Scanner s = new Scanner(System.in);
			System.out.print("Confirm you want to start an extract job with this information (y/n): ");
			String str = s.nextLine();
			s.close();

			if (!str.toLowerCase().equals("y")) {
				_logger.info("Exiting...");
				return;
			}
		}

		_logger.debug("Sending messages");

		for (Set<String> set : split) {
			erm.ExtractionIdentifiers = new ArrayList<>(set);
			_extractRequestMessageProducerModel.SendMessage(erm, "", null);
		}

		_extractRequestInfoMessageProducerModel.SendMessage(erim, "", null);
		_logger.info("Messages sent (Job " + _extractionJobID + ")");
	}

	/**
	 * Checks if the given patient ID looks like a CHI.
	 *
	 * @param patientID
	 * @return true if ID looks like a CHI, false otherwise.
	 */
	@SuppressWarnings("unused")
	private boolean looksLikeCHI(String patientID) {
		return _chiPattern.matcher(patientID.trim()).matches();
	}

	/**
	 * Checks if the given patient ID looks like a EUPI.
	 *
	 * @param patientID
	 * @return true if ID looks like a EUPI, false otherwise.
	 */
	@SuppressWarnings("unused")
	private boolean looksLikeEUPI(String patientID) {
		return _eupiPattern.matcher(patientID.trim()).matches();
	}

	/**
	 * Parses the extraction key from the csv header
	 *
	 * @param keyStr
	 * @throws IllegalArgumentException
	 */
	private void parseExtractionKey(String keyStr) throws IllegalArgumentException {

		if (keyStr == null) {

			_logger.warn("Extraction key not specified, using SOPInstanceUID");
			_extractionKey = ExtractionKey.SOPInstanceUID;

		} else {

			try {

				_extractionKey = ExtractionKey.valueOf(keyStr);

			} catch (IllegalArgumentException e) {

				_logger.error(
						"Couldn't parse '" + keyStr + "' to an ExtractionKey value. Possible values are: "
								+ Arrays.asList(ExtractionKey.values()));
				throw e;
			}
		}
	}
}
