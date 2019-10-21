package org.smi.extractorcl.fileUtils;

import org.smi.extractorcl.exceptions.LineProcessingException;

/**
 * Interface for handlers that process a CSV file parsed by the CsvParser class.
 * 
 */
public interface CsvHandler 
{
    /**
     * Process the header of the CSV file.
     * 
     * @param header  column headings
     * 
     * @throws LineProcessingException if an error occurs
     */
    void processHeader(String[] header) throws LineProcessingException;
    
    /**
     * Process a line of the CSV file.
     * 
     * @param lineNum  line number
     * @param line     column data for this line
     * 
     * @throws LineProcessingException if an error occurs
     */
    void processLine(int lineNum, String[] line)  throws LineProcessingException;
    
    /**
     * The parsing of the CSV file has finished.
     */
    void finished();
    
}