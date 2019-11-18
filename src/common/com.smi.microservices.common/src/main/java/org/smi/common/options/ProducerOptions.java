
package org.smi.common.options;

public class ProducerOptions {

	/**
	 * Name of the RabbitMQ exchange to send messages to
	 */
	public String ExchangeName;

	/**
	 * Maximum number of times to retry the publish confirmations
	 */
	//TODO Implement this similarly to the C# services
	public int MaxConfirmAttempts;

	//TODO VerifyPopulated & equality members	
}
