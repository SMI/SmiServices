
package org.smi.ctpanonymiser.test.util;

import java.io.File;

import org.apache.log4j.Level;
import org.apache.log4j.Logger;
import org.smi.ctpanonymiser.execution.SmiCtpProcessor;
import org.smi.ctpanonymiser.messages.ExtractFileMessage;
import org.smi.ctpanonymiser.util.CtpAnonymisationStatus;
import org.smi.ctpanonymiser.util.DicomAnonymizerToolBuilder;

import junit.framework.TestCase;

public class DicomAnonymizerToolBuilderTest extends TestCase {

	private final static Logger log = Logger.getLogger(DicomAnonymizerToolBuilderTest.class);

	private File _anonScript;
	private File _identifiableDcm;
	private File _anonDcm;
	private File _identifiableDcm2;
	private File _anonDcm2;

	private String _fileSystemRoot;

	protected void setUp() throws Exception {

		super.setUp();

		_fileSystemRoot = System.getProperty("user.dir") + "/src/test/resources/";

		_anonScript = new File(_fileSystemRoot + "dicom-anonymizer.script");
		_identifiableDcm = new File(_fileSystemRoot + "image-000001.dcm");
		if (!_identifiableDcm.exists()) {
			log.error("Unable to find identifiable DICOM file:" + _identifiableDcm.getAbsolutePath());
		}
		_identifiableDcm2 = new File(_fileSystemRoot + "image-000002.dcm");
		if (!_identifiableDcm2.exists()) {
			log.error("Unable to find identifiable DICOM file:" + _identifiableDcm2.getAbsolutePath());
		}

		_anonDcm = new File(_fileSystemRoot + "anon-" + _identifiableDcm.getName());
		_anonDcm2 = new File(_fileSystemRoot + "anon-" + _identifiableDcm2.getName());

		cleanupAnonFiles();
	}

	protected void tearDown() throws Exception {

		super.tearDown();

		cleanupAnonFiles();
	}

	private void cleanupAnonFiles() {

		if (_anonDcm.exists())
			_anonDcm.delete();

		if (_anonDcm2.exists())
			_anonDcm2.delete();
	}

	public void testExtractFileMessage() {

		ExtractFileMessage extractFileMessage = new ExtractFileMessage();

		extractFileMessage.ExtractionDirectory = "";
		extractFileMessage.DicomFilePath = "image-000001.dcm";
		extractFileMessage.OutputPath = "AnonymisedFiles/" + extractFileMessage.DicomFilePath;
		extractFileMessage.ProjectNumber = "1235-4556";

		final SmiCtpProcessor anonTool = new DicomAnonymizerToolBuilder().tagAnonScriptFile(_anonScript).check(null).buildDat();

		// Got the message, now apply the anonymisation
		File in = new File(extractFileMessage.getAbsolutePathToIdentifiableImage(_fileSystemRoot));
		assertTrue("Input file does not exist:" + in.getAbsolutePath(), in.exists());
		assertTrue("Input file read-only", in.setWritable(false, false));

		File out = new File(extractFileMessage.getExtractionOutputPath(_fileSystemRoot));

		Level level = Logger.getRootLogger().getLevel();
		Logger.getRootLogger().setLevel(Level.INFO);

		CtpAnonymisationStatus status = anonTool.anonymize(in, out);
		if (status != CtpAnonymisationStatus.Anonymised) {
			log.error("Anonymisation failed with status "+status+"; last was "+anonTool.getLastStatus());
		}

		Logger.getRootLogger().setLevel(level);

		assertTrue("Anonymised file does not exist:" + out.getAbsolutePath(), out.exists());
		log.info("Anonymised file produced: " + out.getName());
	}
}
