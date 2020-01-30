package org.smi.extractorcl.messages;

import java.util.ArrayList;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions.FieldRequired;
import org.smi.common.messages.ExtractMessage;
import org.smi.common.messages.IMessage;

/**
 * Message sent to initiate the extraction of images for a research project.
 * 
 */
public class ExtractionRequestMessage extends ExtractMessage implements IMessage {
    /**
     * Contains the name of the identifier you want to extract based on (this should
     * be a DicomTag e.g. 'SeriesInstanceUID')
     */
    @FieldRequired
    public String KeyTag;

    /**
     * The extraction modality. Only specified if the KeyTag is StudyInstanceUID
     */
    @FieldRequired
    public String ExtractionModality;

    /**
     * The unique set of identifiers of Type which should be extracted and the
     * corresponding project specific patient identifiers they should be released
     * under
     */
    @FieldRequired
    public ArrayList<String> ExtractionIdentifiers;

    /**
     * Constructor.
     */
    public ExtractionRequestMessage() {
        ExtractionIdentifiers = new ArrayList<String>();
    }
}
