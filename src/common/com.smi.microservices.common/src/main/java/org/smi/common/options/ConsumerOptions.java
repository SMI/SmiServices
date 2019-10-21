
package org.smi.common.options;

/**
 * Class for managing the options for a RabbitMQ consumer
 */
public class ConsumerOptions {

	/**
	 * Name of the queue to consume from
	 */
	public String QueueName;

	/**
	 * Max number of messages the queue will send the consumer before \ receiving an
	 * acknowledgement
	 */
	public int QoSPrefetchCount = 1;

	/**
	 * Automatically acknowledge any messages sent to the consumer
	 */
	public boolean AutoAck = false;

	//TODO VerifyPopulated
}
