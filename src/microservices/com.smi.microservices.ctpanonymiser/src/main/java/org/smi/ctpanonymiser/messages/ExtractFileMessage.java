package org.smi.ctpanonymiser.messages;

import java.nio.file.Paths;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions.FieldRequired;
import org.smi.common.messages.ExtractMessage;
import org.smi.common.messages.IMessage;

/**
 * Message indicating a DICOM file to be anonymised and extracted from the safe
 * haven
 */
public class ExtractFileMessage extends ExtractMessage implements IMessage {

	/**
	 * The subdirectory and dicom filename within the ProjectFolder to extract the
	 * anonymised image into
	 * 
	 * For example 'Series132\1234-an.dcm
	 */
	@FieldRequired
	public String OutputPath;

	/**
	 * File path relative to the fileSystemRoot of the DICOM image to be extracted
	 * and anonymised
	 */
	@FieldRequired
	public String DicomFilePath;

	/**
	 * @param fileSystemRoot
	 * @return The full path to the identifiable image being anonymised and
	 *         extracted
	 */
	public String getAbsolutePathToIdentifiableImage(String fileSystemRoot) {
		return Paths.get(fileSystemRoot, fixPath(DicomFilePath)).normalize().toString();
	}

	public String getExtractionOutputPath(String extractionRoot) {		
		return Paths.get(extractionRoot, fixPath(ExtractionDirectory), fixPath(OutputPath)).normalize().toString();
	}

	private String fixPath(String input) {
		return input.replace("\\", "/");
	}
	
	/**
	 * @return the projectNumber
	 */
	public String getProjectNumber() {
		return ProjectNumber;
	}

	/**
	 * @param projectNumber
	 *            the projectNumber to set
	 */
	public void setProjectNumber(String projectNumber) {
		ProjectNumber = projectNumber;
	}

	/**
	 * @return the projectFolder
	 */
	public String getProjectFolder() {
		return ExtractionDirectory;
	}

	/**
	 * @param projectFolder
	 *            the projectFolder to set
	 */
	public void setProjectFolder(String projectFolder) {
		ExtractionDirectory = projectFolder;
	}

	/**
	 * @return the outputPath
	 */
	public String getOutputPath() {
		return OutputPath;
	}

	/**
	 * @param outputPath
	 *            the outputPath to set
	 */
	public void setOutputPath(String outputPath) {
		OutputPath = outputPath;
	}

	/**
	 * @return the dicomFilePath
	 */
	public String getDicomFilePath() {
		return DicomFilePath;
	}

	/**
	 * @param dicomFilePath
	 *            the dicomFilePath to set
	 */
	public void setDicomFilePath(String dicomFilePath) {
		DicomFilePath = dicomFilePath;
	}

	public String getModality() {
		return Modality;
	}

	public void setModality(String modality) {
        Modality = modality;
	}

	public String toReadableText() {
		String text = new String();
		text =  "ProjectNumber:\t\t"		+ ProjectNumber + "\n";
		text += "ExtractionDirectory:\t"	+ ExtractionDirectory + "\n";
		text += "OutputPath:\t\t"			+ OutputPath + "\n";
		text += "DicomFilePath:\t\t"		+ DicomFilePath + "\n";
		text += "Modality:\t\t"	        	+ Modality + "\n";
		return text;
	}	
}
