package org.smi.extractorcl.exceptions;

/**
 * Exception used when an error occurs processing a file of a data file.
 */
public class LineProcessingException extends Exception
{
    /**
	 * 
	 */
	private static final long serialVersionUID = 9000432502982601820L;
	
	private int _lineNumber;
    private String[] _line;
    
    public LineProcessingException(int lineNumber, String[] line, String message)
    {
        super("Error at line " + lineNumber + ": " + message);
        _line = line;
        _lineNumber = lineNumber;
    }
    
    public String[] getLine()
    {
        return _line;
    }
    
    public int getLineNumber()
    {
        return _lineNumber;           
    }
    
}
