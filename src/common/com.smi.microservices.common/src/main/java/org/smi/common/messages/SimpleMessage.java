
package org.smi.common.messages;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions.FieldRequired;

/**
 * Message wrapper for a {@link String}
 */
public class SimpleMessage implements IMessage {

	/**
	 * @param testMessage The string to be sent
	 */
	public SimpleMessage(String testMessage) {
		this.Message = testMessage;
	}

	@FieldRequired
	public String Message;
}
