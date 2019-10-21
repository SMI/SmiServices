package org.smi.extractorcl.fileUtils;

import java.io.BufferedWriter;
import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.file.DirectoryStream;
import java.nio.file.FileAlreadyExistsException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.util.LinkedList;
import java.util.List;

/**
 * File and directory utilities.
 */
public class FileUtilities
{
    @SuppressWarnings("unused")
	private static final String COPYRIGHT = "Copyright (c) The University of Edinburgh, 2015";

    /** Comma-separated value delimiter. */
    public static final String CSV_DELIMITER = ",";

    /**
     * Create a directory if it does not already exist.
     * @param directory
     *     Directory.
     * @return <code>true</code> if the directory was created and
     * <code>false</code> if the directory was not created because it
     * already exists.
     * @throws FileAlreadyExistsException
     *     If a file exists with that name.
     * @throws IOException
     *     If any problems arise.
     */
    public static boolean createDirectory(Path directory)
        throws IOException, FileAlreadyExistsException
    {
        if (Files.exists(directory) && Files.isDirectory(directory))
        {
            return false;
        }
        Files.createDirectory(directory);
        return true;
    }

    /**
     * List a directory.
     * @param directory
     *     Directory
     * @return list of directory contents.
     * @throws IOException
     *     If any problems arise.
     */
    public static List<Path> listDirectory(Path directory) throws IOException
    {
        List<Path> files = new LinkedList<Path>();
        try (DirectoryStream<Path> stream = Files.newDirectoryStream(directory))
        {
            for (Path path : stream)
            {
                files.add(path);
            }
        }
        return files;
    }

    /**
     * Delete a directory.
     * @param directory
     *     Directory
     * @throws IOException
     *     If any problems arise.
     */
    public static void deleteDirectory(Path directory) throws IOException
    {
        try (DirectoryStream<Path> stream = Files.newDirectoryStream(directory))
        {
            for (Path path : stream)
            {
                if (Files.isDirectory(path))
                {
                    deleteDirectory(path);
                }
                else
                {
                    Files.deleteIfExists(path);
                }
            }
        }
        Files.delete(directory);
    }

    /**
     * Append a line to a file.
     * @param file
     *     File.
     * @param line
     *     Line to append.
     * @throws IOException
     *     If any problems arise.
     */
    public static void appendLine(Path file, String line) throws IOException
    {
        try (BufferedWriter writer =
             Files.newBufferedWriter(file,
                                     Charset.defaultCharset(),
                                     StandardOpenOption.APPEND))
        {
            writer.write(line);
            writer.newLine();
        }
    }

    /**
     * Reads the specified file into a String.
     * 
     * @param file File.
     * @return contents of the file
     * @throws IOException
     *     If any problems arise.
     */
    public static String readFileToString(Path file) throws IOException
    {
        byte[] encoded = Files.readAllBytes(file);
        return new String(encoded, Charset.defaultCharset());
    }
}
