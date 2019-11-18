
package org.smi.common.messages;

import java.nio.charset.Charset;
import java.util.HashMap;

public class MessageHeaderFactory {

	/**
	 * Static constructor
	 */
	static {

		// TODO Change this to "ProcessHandle.current().pid()" if we can use Java 9+
		// String processName =
		// java.lang.management.ManagementFactory.getRuntimeMXBean().getName();
		// ProducerProcessID = Integer.parseInt(processName.split("@")[0]);
	}

	private String ProcessName;
	private int ProcessId;


	/**
	 * @param processName
	 * @param processId
	 */
	public MessageHeaderFactory(String processName, int processId) {

		ProcessName = processName;
		ProcessId = processId;
	}


	/**
	 * @param parent
	 * @return
	 */
	public MessageHeader getHeader(IMessageHeader parent) {
		return new MessageHeader(parent, ProcessName, ProcessId);
	}

	/**
	 * @param headers
	 * @param enc
	 * @return
	 */
	public MessageHeader getHeader(HashMap<String, Object> headers, Charset enc) {
		return new MessageHeader(headers, enc);
	}

	public String getProcessName() {
		return ProcessName;
	}

	public int getProcessId() {
		return ProcessId;
	}
}
