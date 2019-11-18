package org.smi.extractorcl.fileUtils;

import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.file.Files;
import java.nio.file.Path;

import org.smi.extractorcl.exceptions.LineProcessingException;

/**
 * CSV Parser.
 */
public class CsvParser {

	private Path _filename;
	private CsvHandler _csvHandler;
	private int _lineNumber;

	/**
	 * Constructor
	 * 
	 * @param filename
	 *            file to be processed
	 * @param handler
	 *            handler to used to process the data
	 */
	public CsvParser(Path filename, CsvHandler handler) {
		this._filename = filename;
		this._csvHandler = handler;
	}

	/**
	 * Parses the CSV file and calls the handler for each line.
	 * 
	 * @throws LineProcessingException
	 *             if an error occurs processing a line
	 * @throws FileNotFoundException
	 *             if file cannot be found
	 * @throws IOException
	 */
	public void parse() throws LineProcessingException, FileNotFoundException, IOException {

		if (Files.notExists(_filename) || Files.isDirectory(_filename))
			throw new FileNotFoundException(String.format("Cannot find file: %s", _filename));

		BufferedReader reader = Files.newBufferedReader(_filename, Charset.defaultCharset());
		_lineNumber = 1;

		String header = reader.readLine();

		if ((header == null) || ("".equals(header.trim())))
			throw new LineProcessingException(1, new String[] {}, "Missing header: " + _filename);

		_csvHandler.processHeader(splitRow(header));

		String line;

		while ((line = reader.readLine()) != null) {

			++_lineNumber;
			parseRow(line, _lineNumber);
		}

		this._csvHandler.finished();
	}

	/**
	 * Parses a row and calls the handler. Empty rows are ignored.
	 * 
	 * @param row
	 *            row string
	 * @param lineNumber
	 *            line number
	 * 
	 * @throws LineProcessingException
	 *             if the handler throws this exception
	 */
	private void parseRow(String row, int lineNumber) throws LineProcessingException {

		if ("".equals(row.trim()))
			return;

		_csvHandler.processLine(lineNumber, splitRow(row));
	}

	/**
	 * Splits a row and trims each field.
	 * 
	 * @param row
	 *            row to split
	 * 
	 * @return array of field strings
	 */
	private String[] splitRow(String row) {
		String[] fields = row.split(FileUtilities.CSV_DELIMITER);
		for (int i = 0; i < fields.length; i++) {
			fields[i] = fields[i].trim();
		}
		return fields;
	}
}
