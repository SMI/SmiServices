package org.smi.extractorcl.test.fileUtils;

import static org.mockito.AdditionalMatchers.aryEq;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoMoreInteractions;

import java.io.FileNotFoundException;
import java.nio.charset.StandardCharsets;
import java.nio.file.FileSystem;
import java.nio.file.Files;
import java.nio.file.Path;

import org.smi.extractorcl.exceptions.LineProcessingException;
import org.smi.extractorcl.fileUtils.CsvHandler;
import org.smi.extractorcl.fileUtils.CsvParser;

import com.google.common.collect.ImmutableList;
import com.google.common.jimfs.Configuration;
import com.google.common.jimfs.Jimfs;

import junit.framework.TestCase;


/**
 * Test class for CsvParser
 */
public class CsvParserTest extends TestCase
{
    private static final String newLine = System.getProperty("line.separator");
    
    private Path _file1;
    private Path _file2;
    private Path _missingFile;
    private Path _emptyFile;
    
    /**
     * Creates an in memory file system used by the tests.
     */
    public void setUp() throws Exception
    {
        // Mock the file system
        Configuration config = Configuration.windows().toBuilder().setWorkingDirectory("C:\\working").build();
        FileSystem fs = Jimfs.newFileSystem(config);
        
        _file1 = fs.getPath("file1.csv");
        StringBuilder file1Text = new StringBuilder();
        file1Text.append("patientID, seriesID");
        file1Text.append(newLine);
        file1Text.append("p1,s1");
        file1Text.append(newLine);
        file1Text.append("p2,s2");
        file1Text.append(newLine);
        file1Text.append("p3,s3");
        file1Text.append(newLine);
        Files.write(_file1, ImmutableList.of(file1Text), StandardCharsets.UTF_8); 
        
        _file2 = fs.getPath("file2.csv");
        StringBuilder file2Text = new StringBuilder();
        file2Text.append("patientID, seriesID");
        file2Text.append(newLine);
        file2Text.append("p1,s1");
        file2Text.append(newLine);
        file2Text.append(newLine);  // Empty line
        file2Text.append("p2,s2");
        file2Text.append(newLine);
        file2Text.append("p3,s3");
        file2Text.append(newLine);
        Files.write(_file2, ImmutableList.of(file2Text), StandardCharsets.UTF_8); 

        _missingFile = fs.getPath("missingFile.csv");
        
        _emptyFile  = fs.getPath("emptyFile.csv");
        StringBuilder emptyText = new StringBuilder();
        Files.write(_emptyFile, ImmutableList.of(emptyText), StandardCharsets.UTF_8); 
    }

    /**
     * Tests basic successful parsing.
     * 
     * @throws Exception
     */
    public void testBasicParsing() throws Exception
    {
        CsvHandler handler = mock(CsvHandler.class);
        CsvParser csvParser = new CsvParser(_file1, handler);
        csvParser.parse();
        
        verify(handler).processHeader(aryEq(new String[] {"patientID", "seriesID"}));
        verify(handler).processLine(eq(2), aryEq(new String[] {"p1","s1"}));
        verify(handler).processLine(eq(3), aryEq(new String[] {"p2","s2"}));
        verify(handler).processLine(eq(4), aryEq(new String[] {"p3","s3"}));
        verify(handler).finished();
        verifyNoMoreInteractions(handler);        
    }
    
    /**
     * Tests that missing rows are ignored.
     * 
     * @throws Exception
     */
    public void testIgnoresMissingRows() throws Exception
    {
        CsvHandler handler = mock(CsvHandler.class);
        CsvParser csvParser = new CsvParser(_file2, handler);
        csvParser.parse();
        
        verify(handler).processHeader(aryEq(new String[] {"patientID", "seriesID"}));
        verify(handler).processLine(eq(2), aryEq(new String[] {"p1","s1"}));
        verify(handler).processLine(eq(4), aryEq(new String[] {"p2","s2"}));
        verify(handler).processLine(eq(5), aryEq(new String[] {"p3","s3"}));
        verify(handler).finished();
        verifyNoMoreInteractions(handler);        
    }
    
    /**
     * Tests when the file does not exist.
     * 
     * @throws Exception
     */
    public void testFileDoesNotExist() throws Exception
    {
        CsvHandler handler = mock(CsvHandler.class);
        CsvParser csvParser = new CsvParser(_missingFile, handler);
        
        try
        {
            csvParser.parse();
            fail("Expected a FileNotFoundException");
        }
        catch(FileNotFoundException e)
        {
            assertEquals("Cannot find file: missingFile.csv",e.getMessage());
        }
        verifyNoMoreInteractions(handler);
    }
    
    /** 
     * Test handling an empty file.
     */
    public void testEmptyFile() throws Exception
    {
        CsvHandler handler = mock(CsvHandler.class);
        CsvParser csvParser = new CsvParser(_emptyFile, handler);
        
        try
        {
            csvParser.parse();
            fail("Expected a LineProcessingException");
        }
        catch(LineProcessingException e)
        {
            assertEquals("Error at line 1: Missing header: emptyFile.csv",e.getMessage());
        }
        verifyNoMoreInteractions(handler);
    }
}

