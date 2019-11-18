
package org.smi.common.messages;

import java.util.Map;
import java.util.UUID;

public interface IMessageHeader {

	UUID getMessageGuid();

	long getOriginalPublishTimestamp();
	
	int getProducerProcessID();

	String getProducerExecutableName();

	UUID[] getParents();

	void Populate(Map<String, Object> headers);
}
