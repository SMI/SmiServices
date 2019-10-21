
package org.smi.common.messages;

import java.nio.charset.Charset;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.stream.Collectors;

import com.rabbitmq.client.LongString;

public class MessageHeader implements IMessageHeader {

	private UUID MessageGuid;

	public long OriginalPublishTimestamp;

	private String ProducerExecutableName;

	private int ProducerProcessID;

	private UUID[] Parents;

	public static final String Splitter = "->";

	protected MessageHeader(IMessageHeader parent, String producerExecutableName, int producerProcessID) {

		ProducerExecutableName = producerExecutableName;
		ProducerProcessID = producerProcessID;
		MessageGuid = UUID.randomUUID();

		if (parent == null) {

			Parents = new UUID[0];
			OriginalPublishTimestamp = UnixTimeNow();

		} else {

			List<UUID> p = new ArrayList<UUID>();

			for (UUID par : parent.getParents())
				p.add(par);

			p.add(parent.getMessageGuid());
			Parents = p.toArray(new UUID[0]);
			OriginalPublishTimestamp = parent.getOriginalPublishTimestamp();
		}
	}
	
	public MessageHeader(Map<String, Object> headers, Charset enc) {

		MessageGuid = GetUuidFromEncodedHeader(headers.get("MessageGuid"), enc);
		ProducerExecutableName = new String(((LongString) headers.get("ProducerExecutableName")).getBytes(), enc);
		ProducerProcessID = (int) headers.get("ProducerProcessID");
		Parents = GetUuidArrayFromEncodedHeader(headers.get("Parents"), enc);
		
		// Allow backwards compatibility
        if (!headers.containsKey("OriginalPublishTimestamp"))
        	headers.put("OriginalPublishTimestamp", (long)0);
        else
            OriginalPublishTimestamp = (long)headers.get("OriginalPublishTimestamp");

        headers.remove("RetryCount");
	}

	private UUID GetUuidFromEncodedHeader(Object o, Charset enc) {

		String s = new String(((LongString) o).getBytes(), enc);
		return UUID.fromString(s);
	}

	private UUID[] GetUuidArrayFromEncodedHeader(Object o, Charset enc) {

		String s = new String(((LongString) o).getBytes(), enc);
		String[] split = s.split("->", -1);

		if (s.isEmpty() || split.length == 0)
			return new UUID[0];

		// I hate Java...
		return Arrays
				.asList(split)
				.stream()
				.map(x -> UUID.fromString(x))
				.collect(Collectors.toList())
				.toArray(new UUID[0]);
	}

	@Override
	public void Populate(Map<String, Object> headers) {

		headers.put("MessageGuid", MessageGuid.toString());
		headers.put("ProducerProcessID", ProducerProcessID);
		headers.put("ProducerExecutableName", ProducerExecutableName);
		headers.put("Parents", Arrays.stream(Parents).map(String::valueOf).collect(Collectors.joining(Splitter)));
		headers.put("OriginalPublishTimestamp", OriginalPublishTimestamp);
	}

	@Override
	public String toString() {

		StringBuilder sb = new StringBuilder();

		sb.append("\nMessageGuid: " + MessageGuid);
		sb.append("\nProducerExecutableName: " + ProducerExecutableName);
		sb.append("\nProducerProcessID: " + ProducerProcessID);
		sb.append("\nParents: " + Arrays.toString(Parents));
		sb.append("\nOriginalPublishTimestamp: " + OriginalPublishTimestamp);
		
		return sb.toString();
	}

	public long UnixTimeNow() {
		return System.currentTimeMillis() / 1000L;
	}

	public UUID getMessageGuid() {
		return MessageGuid;
	}

	public void setMessageGuid(UUID messageGuid) {
		MessageGuid = messageGuid;
	}

	public int getProducerProcessID() {
		return ProducerProcessID;
	}

	public void setProducerProcessID(int producerProcessID) {
		ProducerProcessID = producerProcessID;
	}

	public String getProducerExecutableName() {
		return ProducerExecutableName;
	}

	public void setProducerExecutableName(String producerExecutableName) {
		ProducerExecutableName = producerExecutableName;
	}

	public long getOriginalPublishTimestamp() {
		return OriginalPublishTimestamp;
	}

	public UUID[] getParents() {
		return Parents;
	}

	public void setParents(UUID[] parents) {
		Parents = parents;
	}
}
