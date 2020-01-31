package org.smi.extractorcl.messages;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions.FieldRequired;
import org.smi.common.messages.ExtractMessage;
import org.smi.common.messages.IMessage;

/**
 * Message sent to initiate the extraction of images for a research project.
 * 
 */
public class ExtractionRequestInfoMessage extends ExtractMessage implements IMessage {
    /**
     * Project number.
     */
    @FieldRequired
    public String KeyTag;

    /**
     * The number of unique identifiers to be extracted.
     */
    @FieldRequired
    public int KeyValueCount;

    /**
     * The extraction modality. Only specified if the KeyTag is StudyInstanceUID
     */
    @FieldRequired
    public String ExtractionModality;

    /**
     * Constructor.
     */
    public ExtractionRequestInfoMessage() {
    }
}
