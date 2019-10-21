
package org.smi.common.messages;

import org.smi.common.messageSerialization.JsonDeserializerWithOptions.FieldRequired;

//TODO Ensure this is sent to FatalLoggingExchange on crashes

/**
 * Object representing a fatal log message
 */
public class FatalLogMessage implements IMessage {

	@FieldRequired
	public String Message;
	
	@FieldRequired
	public Exception Exception;
	
	
	public FatalLogMessage(String message, Exception exception) {
		
		Message = message;
		Exception = exception;
	}	
}
