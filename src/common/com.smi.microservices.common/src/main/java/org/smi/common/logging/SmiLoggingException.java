
package org.smi.common.logging;

public class SmiLoggingException extends RuntimeException {

	/**
	 * 
	 */
	private static final long serialVersionUID = 8773812194176038038L;

    public SmiLoggingException() {
        super();
    }
    public SmiLoggingException(String s) {
        super(s);
    }
    public SmiLoggingException(String s, Throwable throwable) {
        super(s, throwable);
    }
    public SmiLoggingException(Throwable throwable) {
        super(throwable);
    }	
}
