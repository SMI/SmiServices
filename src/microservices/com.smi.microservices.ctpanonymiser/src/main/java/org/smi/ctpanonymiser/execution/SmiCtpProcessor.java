package org.smi.ctpanonymiser.execution;

import java.awt.image.BufferedImage;
import java.io.File;
import java.util.Properties;

import org.apache.log4j.Logger;
import org.rsna.ctp.objects.DicomObject;
import org.rsna.ctp.stdstages.anonymizer.AnonymizerStatus;
import org.rsna.ctp.stdstages.anonymizer.dicom.DAScript;
import org.rsna.ctp.stdstages.anonymizer.dicom.DICOMAnonymizer;
import org.rsna.ctp.stdstages.anonymizer.dicom.DICOMDecompressor;
import org.rsna.ctp.stdstages.anonymizer.dicom.DICOMPixelAnonymizer;
import org.rsna.ctp.stdstages.anonymizer.dicom.PixelScript;
import org.rsna.ctp.stdstages.anonymizer.dicom.Regions;
import org.rsna.ctp.stdstages.anonymizer.dicom.Signature;
import org.rsna.ctp.stdstages.anonymizer.dicom.Transcoder;
import org.smi.ctpanonymiser.util.CtpAnonymisationStatus;
import org.smi.ctpanonymiser.util.SmiAnonymisationException;

public class SmiCtpProcessor {

	private static final Logger _logger = Logger.getRootLogger();

	private static final String JPEGBaseline = "1.2.840.10008.1.2.4.50";
	private static final String JPEGLossLess = "1.2.840.10008.1.2.4.70";

	private boolean _decompress;
	private boolean _recompress;
	private boolean _setBIRElement;
	private boolean _testmode;
	private String _check;

	private PixelScript _pixelScript;
	private boolean performPixelAnon = false;

	private Properties _tagAnonScriptProps;

	private Transcoder _transcoder = new Transcoder();

	private String _lastStatus;

	public SmiCtpProcessor(File tagAnonScriptFile, File pixelAnonScriptFile, boolean decompress, boolean recompress, boolean setBIRElement,
			boolean testmode, String check) {

		_decompress = decompress;
		_recompress = recompress;
		_setBIRElement = setBIRElement;
		_testmode = testmode;
		_check = check;

		if (pixelAnonScriptFile != null) {

			_pixelScript = new PixelScript(pixelAnonScriptFile);

			if (_pixelScript == null)
				throw new NullPointerException("Pixel script was null");

			performPixelAnon = true;
		}

		_tagAnonScriptProps = DAScript.getInstance(tagAnonScriptFile).toProperties();

		_transcoder.setTransferSyntax(JPEGLossLess);
	}

	public String getLastStatus() {
		return _lastStatus;
	}

	@SuppressWarnings("unused")
	public CtpAnonymisationStatus anonymize(File inFile, File outFile) {

		// This breaks when running under sudo. We need to run as sudo for now since the lustre permissions are
		// borked, but this should be un-commented ASAP as it prevents source files being edited
		//if (inFile.canWrite()) {
		if(false) {

			_lastStatus = "Input file " + inFile + " was writeable";
			_logger.error(_lastStatus);
			return CtpAnonymisationStatus.InputFileException;
		}

		DicomObject dObj = null;

		try {

			dObj = new DicomObject(inFile);

		} catch (Exception e) {

			_logger.error("Could not create dicom object from inFile", e);

			_lastStatus = e.getMessage();
			return CtpAnonymisationStatus.InputFileException;
		}

		_logger.debug("Anonymising " + inFile);

		// Run the DICOMPixelAnonymizer first before the elements used in signature
		// matching are modified by the DicomAnonymizer.
		if (performPixelAnon && dObj.isImage()) {

			try {

				inFile = DoPixelAnon(inFile, outFile, dObj);

			} catch (SmiAnonymisationException e) {

				_logger.error("Pixel anon failed", e);

				_lastStatus = e.getMessage();
				return CtpAnonymisationStatus.PixelAnonFailed;
			}

		} else {

			_logger.debug("Pixel anonymisation skipped");
		}

		// Ref:
		// https://github.com/johnperry/CTP/blob/master/source/java/org/rsna/ctp/stdstages/anonymizer/dicom/DICOMAnonymizer.java
		AnonymizerStatus status = DICOMAnonymizer.anonymize(inFile, outFile, _tagAnonScriptProps, null, null, false, false);

		if (!status.isOK()) {

			_logger.error("DICOMAnonymizer returned status " + status.getStatus() + ", with message " + status.getMessage());

			_lastStatus = status.getStatus();
			return CtpAnonymisationStatus.TagAnonFailed;
		}

		if (_check != null) {

			try {

				DoChecks(outFile);

			} catch (SmiAnonymisationException e) {

				_logger.error("Checking of output file failed", e);

				_lastStatus = e.getMessage();
				return CtpAnonymisationStatus.OutputFileChecksFailed;
			}
		}

		_logger.debug("Anonymised file " + outFile);

		_lastStatus = null;
		return CtpAnonymisationStatus.Anonymised;
	}

	private File DoPixelAnon(File inFile, File outFile, DicomObject dObj) {

		Signature signature = _pixelScript.getMatchingSignature(dObj);

		if (signature == null) {

			_logger.debug("No signature found for pixel anonymization");
			return inFile;
		}

		Regions regions = signature.regions;

		if (regions == null || regions.size() == 0) {

			_logger.debug("No regions found for pixel anonymization in " + inFile);
			return inFile;
		}

		boolean decompressed = false;

		if (_decompress && dObj.isEncapsulated() && !dObj.getTransferSyntaxUID().equals(JPEGBaseline)) {

			if (DICOMDecompressor.decompress(inFile, outFile).isOK()) {

				try {

					dObj = new DicomObject(outFile);

				} catch (Exception e) {

					throw new SmiAnonymisationException("Exception creating DicomObject from " + outFile, e);
				}

				decompressed = true;

			} else {

				outFile.delete();
				throw new SmiAnonymisationException("Decompression failure");
			}
		}

		// Ref:
		// https://github.com/johnperry/CTP/blob/master/source/java/org/rsna/ctp/stdstages/anonymizer/dicom/DICOMPixelAnonymizer.java
		AnonymizerStatus status = DICOMPixelAnonymizer.anonymize(dObj.getFile(), outFile, regions, _setBIRElement, _testmode);

		if (!status.isOK()) {

			outFile.delete();
			throw new SmiAnonymisationException("Pixel anonymisation failure: " + status.getStatus() + "\n" + status.getMessage());
		}

		if (decompressed && _recompress)
			_transcoder.transcode(outFile, outFile);

		return outFile;
	}

	private void DoChecks(File outFile) {

		try {

			DicomObject dObj = new DicomObject(outFile);

			if (dObj.isImage()) {

				int numberOfFrames = dObj.getNumberOfFrames();

				if (numberOfFrames == 0)
					numberOfFrames++;

				BufferedImage frame = null;

				if (_check.equals("all")) {

					for (int k = 0; k < numberOfFrames; k++)
						frame = dObj.getBufferedImage(k, false);

				} else  if (_check.equals("") || _check.equals("last")) {

					frame = dObj.getBufferedImage(numberOfFrames - 1, false);

				} else if (_check.equals("first")) {

					frame = dObj.getBufferedImage(0, false);
				}

				if (frame == null)
					throw new SmiAnonymisationException("Frame checking failed (a frame was null)");
			}

		} catch (Exception e) {

			throw new SmiAnonymisationException("Frame checking failed", e);
		}
	}
}
