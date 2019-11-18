package org.smi.extractorcl.exceptions;

import java.nio.file.Path;

/**
 * Exception used when an error occurs processing a file.
 */
public class FileProcessingException extends Exception
{
	private static final long serialVersionUID = -4210563976822799412L;

	public FileProcessingException(Path file, Throwable cause)
    {
        super(String.format("Error processing file: %s", file.getFileName()), cause);
    }
}
