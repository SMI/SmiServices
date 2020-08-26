
package org.smi.ctpanonymiser.util;

public enum ExtractFileStatus {

    Unknown,

	/**
	 * The file has been anonymised successfully
	 */
	Anonymised,

	/**
	 * The file could not be anonymised but will be retried later
	 */
	ErrorWillRetry,

	/**
	 * The file could not be anonymised and will not be retired
	 */
	ErrorWontRetry,

	/**
	 * The source file could not be found under the given filesystem root
	 */
	FileMissing,

	/**
	 * The source file was successfully copied to the destination
	 */
	Copied,
}
