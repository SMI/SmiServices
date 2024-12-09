
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

	/**
	 * True if this is an identifiable extraction (i.e. files should not be anonymised)
	 */
	@FieldRequired
    public boolean IsIdentifiableExtraction;

    /**
	 * True if this is a "no filters" (i.e. no file rejection filters should be applied)
	 */
	@FieldRequired
    public boolean IsNoFilterExtraction;

    /**
    * True if this extraction uses the global pool of DICOM files
    */
    @FieldRequired
    public boolean IsPooledExtraction;

	protected ExtractMessage() {
	}
}
