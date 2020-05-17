
package org.smi.common.messages;

import java.util.UUID;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions.FieldRequired;

public abstract class ExtractMessage implements IMessage {

	/**
	 * UUID for this run of the image extraction process
	 */
	@FieldRequired
	public UUID ExtractionJobIdentifier;

	/**
	 * Project number for reference and base of extracted files
	 */
	@FieldRequired
	public String ProjectNumber;

	/**
	 * The directory to which the data should be written. Specified relative to the
	 * ExtractRoot
	 */
	@FieldRequired
	public String ExtractionDirectory;

	/**
	 * DateTime at which the extraction request was submitted
	 */
	@FieldRequired
	public String JobSubmittedAt;

	protected ExtractMessage() {
	}
}
