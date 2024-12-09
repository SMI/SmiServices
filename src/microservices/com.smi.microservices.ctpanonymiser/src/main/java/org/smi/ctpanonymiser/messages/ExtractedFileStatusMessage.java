package org.smi.ctpanonymiser.messages;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions.FieldRequired;
import org.smi.common.messages.ExtractMessage;
import org.smi.common.messages.IMessage;
import org.smi.ctpanonymiser.util.ExtractedFileStatus;

/**
 * Message indicating the path to an anonymised file
 */
public class ExtractedFileStatusMessage extends ExtractMessage implements IMessage {

	@FieldRequired
	public String DicomFilePath;

	@FieldRequired
	public String OutputFilePath;

    @FieldRequired
	public ExtractedFileStatus Status;

	@FieldRequired
	public String StatusMessage;

	/** @return the filePath */
	public String getFilePath() {
		return DicomFilePath;
	}

	/**
	 * @param filePath
	 *            the filePath to set
	 */
	public void setFilePath(String dicomFilePath) {
		DicomFilePath = dicomFilePath;
	}

	/** @return the Project Number */
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

	public ExtractedFileStatusMessage(ExtractFileMessage request) {

		ExtractionJobIdentifier = request.ExtractionJobIdentifier;
		ExtractionDirectory = request.ExtractionDirectory;
        Modality = request.Modality;
		DicomFilePath = request.DicomFilePath;
		ProjectNumber = request.ProjectNumber;
		JobSubmittedAt = request.JobSubmittedAt;
	}

	@Override
	public String toString() {

		StringBuilder sb = new StringBuilder();

		sb.append("ExtractionJobIdentifier: " + ExtractionJobIdentifier + "\n");
		sb.append("ExtractionDirectory: " + ExtractionDirectory + "\n");
		sb.append("Modality: " + Modality + "\n");
		sb.append("DicomFilePath: " + DicomFilePath + "\n");
		sb.append("ProjectNumber: " + ProjectNumber + "\n");
		sb.append("JobSubmittedAt: " + JobSubmittedAt + "\n");
		sb.append("OutputFilePath: " + OutputFilePath + "\n");
		sb.append("Status: " + Status + "\n");
		sb.append("StatusMessage: " + StatusMessage + "\n");

		return sb.toString();
	}

}
