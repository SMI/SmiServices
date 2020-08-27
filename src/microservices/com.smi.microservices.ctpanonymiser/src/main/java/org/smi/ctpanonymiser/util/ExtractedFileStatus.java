
package org.smi.ctpanonymiser.util;

public enum ExtractedFileStatus {
    /**
	 * Unused placeholder value
	 */
    Unused,

	/**
	 * The file has been anonymised successfully
	 */
	Anonymised,

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
