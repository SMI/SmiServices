package org.smi.extractorcl.test.execution;

import com.google.common.collect.ImmutableList;
import com.google.common.jimfs.Configuration;
import com.google.common.jimfs.Jimfs;
import junit.framework.TestCase;
import org.mockito.ArgumentCaptor;
import org.smi.common.messaging.IProducerModel;
import org.smi.extractorcl.exceptions.FileProcessingException;
import org.smi.extractorcl.fileUtils.CsvParser;
import org.smi.extractorcl.fileUtils.ExtractMessagesCsvHandler;
import org.smi.extractorcl.messages.ExtractionRequestInfoMessage;
import org.smi.extractorcl.messages.ExtractionRequestMessage;

import java.nio.charset.StandardCharsets;
import java.nio.file.FileSystem;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

/**
 * Test class for ExtractImages.
 */
public class ExtractImagesTest extends TestCase {

	private static final String newLine = System.getProperty("line.separator");

	private Path _file1;
	private Path _file2;
	private Path _missingFile;
	private Path _emptyFile;

	/**
	 * Creates an in memory file system used by the tests.
	 */
	public void setUp() throws Exception {

		// Mock the file system
		Configuration config = Configuration.windows().toBuilder().setWorkingDirectory("C:\\working").build();
		FileSystem fs = Jimfs.newFileSystem(config);

		_file1 = fs.getPath("file1.csv");
		StringBuilder file1Text = new StringBuilder();
		file1Text.append("SeriesInstanceUID");
		file1Text.append(newLine);
		file1Text.append("s1");
		file1Text.append(newLine);
		file1Text.append("s2");
		file1Text.append(newLine);
		file1Text.append("s3");
		file1Text.append(newLine);
		Files.write(_file1, ImmutableList.of(file1Text), StandardCharsets.UTF_8);

		_file2 = fs.getPath("file2.csv");
		StringBuilder file2Text = new StringBuilder();
		file2Text.append("SeriesInstanceUID");
		file2Text.append(newLine);
		file2Text.append("s1");
		file2Text.append(newLine);
		file2Text.append("s4");
		file2Text.append(newLine);
		file2Text.append("s5");
		file2Text.append(newLine);
		Files.write(_file2, ImmutableList.of(file2Text), StandardCharsets.UTF_8);

		_missingFile = fs.getPath("missingFile.csv");

		_emptyFile = fs.getPath("emptyFile.csv");
		StringBuilder emptyText = new StringBuilder();
		Files.write(_emptyFile, ImmutableList.of(emptyText), StandardCharsets.UTF_8);
	}

	/**
	 * Tests extracting images from a single file.
	 *
	 * @throws Exception
	 */
	public void testExtractImagesFromSingleFile() throws Exception {

		IProducerModel extractRequestMessageProducerModel = mock(IProducerModel.class);
		IProducerModel extractRequestInfoMessageProducerModel = mock(IProducerModel.class);

		UUID jobIdentifier = UUID.randomUUID();

		ExtractMessagesCsvHandler csvHandler = new ExtractMessagesCsvHandler(
				jobIdentifier,
				"MyProjectID",
				"MyProjectFolder",
				null,
				extractRequestMessageProducerModel,
				extractRequestInfoMessageProducerModel);

		CsvParser parser = new CsvParser(_file1, csvHandler);

		try {

			parser.parse();

		} catch (Throwable e) {

			throw new FileProcessingException(_file1, e);
		}

		csvHandler.sendMessages(true);

		ArgumentCaptor<Object> requestMessage = ArgumentCaptor.forClass(Object.class);
		verify(extractRequestMessageProducerModel).SendMessage(requestMessage.capture(), eq(""), eq(null));
		verifyNoMoreInteractions(extractRequestMessageProducerModel);

		ArgumentCaptor<Object> requestInfoMessage = ArgumentCaptor.forClass(Object.class);
		verify(extractRequestInfoMessageProducerModel).SendMessage(requestInfoMessage.capture(), eq(""), eq(null));
		verifyNoMoreInteractions(extractRequestInfoMessageProducerModel);

		// Check the messages had the correct details
		ExtractionRequestMessage erm = (ExtractionRequestMessage) requestMessage.getValue();
		assertEquals(jobIdentifier, erm.ExtractionJobIdentifier);
		assertEquals("MyProjectID", erm.ProjectNumber);
		assertEquals("MyProjectFolder", erm.ExtractionDirectory);
		assertEquals("SeriesInstanceUID", erm.KeyTag);
		assertEquals(3, erm.ExtractionIdentifiers.size());

		assertTrue(erm.ExtractionIdentifiers.contains("s1"));
		assertTrue(erm.ExtractionIdentifiers.contains("s2"));
		assertTrue(erm.ExtractionIdentifiers.contains("s3"));

		// Check the messages had the correct details
		ExtractionRequestInfoMessage erim = (ExtractionRequestInfoMessage) requestInfoMessage.getValue();
		assertEquals(jobIdentifier, erim.ExtractionJobIdentifier);
		assertEquals("MyProjectID", erim.ProjectNumber);
		assertEquals("MyProjectFolder", erim.ExtractionDirectory);
		assertEquals("SeriesInstanceUID", erim.KeyTag);
		assertEquals(3, erim.KeyValueCount);
	}

	/**
	 * Tests extracting image from multiple files.
	 *
	 * @throws Exception
	 */
	public void testExtractImagesFromMultipleFiles() throws Exception {

		IProducerModel extractRequestMessageProducerModel = mock(IProducerModel.class);
		IProducerModel extractRequestInfoMessageProducerModel = mock(IProducerModel.class);

		UUID jobIdentifier = UUID.randomUUID();

		ExtractMessagesCsvHandler csvHandler = new ExtractMessagesCsvHandler(
				jobIdentifier,
				"MyProjectID",
				"MyProjectFolder",
				null,
				extractRequestMessageProducerModel,
				extractRequestInfoMessageProducerModel);

		for (Path file : new Path[]{_file1, _file2}) {

			CsvParser parser = new CsvParser(file, csvHandler);

			try {

				parser.parse();

			} catch (Throwable e) {

				throw new FileProcessingException(file, e);
			}
		}

		csvHandler.sendMessages(true);

		ArgumentCaptor<Object> requestMessage = ArgumentCaptor.forClass(Object.class);
		verify(extractRequestMessageProducerModel).SendMessage(requestMessage.capture(), eq(""), eq(null));
		verifyNoMoreInteractions(extractRequestMessageProducerModel);

		ArgumentCaptor<Object> requestInfoMessage = ArgumentCaptor.forClass(Object.class);
		verify(extractRequestInfoMessageProducerModel).SendMessage(requestInfoMessage.capture(), eq(""), eq(null));
		verifyNoMoreInteractions(extractRequestInfoMessageProducerModel);

		// Check the messages had the correct details
		ExtractionRequestMessage erm = (ExtractionRequestMessage) requestMessage.getValue();
		assertEquals(jobIdentifier, erm.ExtractionJobIdentifier);
		assertEquals("MyProjectID", erm.ProjectNumber);
		assertEquals("MyProjectFolder", erm.ExtractionDirectory);
		assertEquals("SeriesInstanceUID", erm.KeyTag);
		assertEquals(5, erm.ExtractionIdentifiers.size());

		assertTrue(erm.ExtractionIdentifiers.contains("s1"));
		assertTrue(erm.ExtractionIdentifiers.contains("s2"));
		assertTrue(erm.ExtractionIdentifiers.contains("s3"));
		assertTrue(erm.ExtractionIdentifiers.contains("s4"));
		assertTrue(erm.ExtractionIdentifiers.contains("s5"));

		// Check the messages had the correct details
		ExtractionRequestInfoMessage erim = (ExtractionRequestInfoMessage) requestInfoMessage.getValue();
		assertEquals(jobIdentifier, erim.ExtractionJobIdentifier);
		assertEquals("MyProjectID", erim.ProjectNumber);
		assertEquals("MyProjectFolder", erim.ExtractionDirectory);
		assertEquals("SeriesInstanceUID", erim.KeyTag);
		assertEquals(5, erim.KeyValueCount);
	}

	/**
	 * Tests handling a missing file.
	 *
	 * @throws Exception
	 */
	public void testMissingFile() throws Exception {

		ExtractMessagesCsvHandler csvHandler = new ExtractMessagesCsvHandler(
				UUID.randomUUID(),
				"MyProjectID",
				"MyProjectFolder",
				null,
				null,
				null);

		CsvParser parser = new CsvParser(_missingFile, csvHandler);

		try {

			parser.parse();
			fail("Expected a FileProcessingException");

		} catch (Throwable e) {

			assertEquals("Cannot find file: missingFile.csv", e.getMessage());
		}
	}

	/**
	 * Test handling an empty file.
	 *
	 * @throws Exception
	 */
	public void testEmptyFile() throws Exception {

		ExtractMessagesCsvHandler csvHandler = new ExtractMessagesCsvHandler(
				UUID.randomUUID(),
				"MyProjectID",
				"MyProjectFolder",
				null,
				null,
				null);

		CsvParser parser = new CsvParser(_emptyFile, csvHandler);

		try {

			parser.parse();
			fail("Expected a FileProcessingException");

		} catch (Throwable e) {

			assertEquals("Error at line 1: Missing header: emptyFile.csv", e.getMessage());
		}
	}
}
