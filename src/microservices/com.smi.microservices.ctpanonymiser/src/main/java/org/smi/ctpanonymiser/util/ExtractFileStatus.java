
package org.smi.ctpanonymiser.util;

public enum ExtractFileStatus {

	/**
	 * The file has been anonymised successfully
	 */
	Anonymised(0),

	/**
	 * The file could not be anonymised but will be retried later
	 */
	ErrorWillRetry(1),

	/**
	 * The file could not be anonymised and will not be retired
	 */
	ErrorWontRetry(2);

	public final int status;

	private ExtractFileStatus(int status) {

		this.status = status;
	}
}
