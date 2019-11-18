
package org.smi.ctpanonymiser.util;

public class SmiAnonymisationException extends RuntimeException {

	/**
	 * 
	 */
	private static final long serialVersionUID = -8668878374995489207L;

	public SmiAnonymisationException() {
		super();
	}

	public SmiAnonymisationException(String s) {
		super(s);
	}

	public SmiAnonymisationException(String s, Throwable throwable) {
		super(s, throwable);
	}

	public SmiAnonymisationException(Throwable throwable) {
		super(throwable);
	}
}
