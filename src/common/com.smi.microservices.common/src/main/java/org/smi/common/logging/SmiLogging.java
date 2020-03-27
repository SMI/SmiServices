
package org.smi.common.logging;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.InvalidPathException;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Enumeration;
import java.util.Iterator;

import org.apache.log4j.Appender;
import org.apache.log4j.FileAppender;
import org.apache.log4j.Level;
import org.apache.log4j.Logger;

/**
 * Static helper class to setup the SMI logging
 */
public final class SmiLogging {

    private static final String ConfigFileName = "SmiLogbackConfig.xml";

    private SmiLogging() {
    }

    public static String getCaller() {
        String prev=null;
        StackTraceElement[] stElements = Thread.currentThread().getStackTrace();
        for (int i=1; i<stElements.length; i++) {
            StackTraceElement ste = stElements[i];
            if (i<3)
                prev=ste.getClassName();
            if (ste.getMethodName()=="main") {
                prev=ste.getClassName();
                return prev.substring(prev.lastIndexOf('.')+1);
            }
        }
        return prev==null?null:prev.substring(prev.lastIndexOf('.')+1);
    }
    
    public static long getPid() {
        return ProcessHandle.current().pid();
    }

    public static void Setup(boolean testing) throws SmiLoggingException {
        DateFormat df = new SimpleDateFormat("yyyy-MM-dd-HH-mm-ss");
        String logroot = System.getenv("SMI_LOGS_ROOT");
        if (logroot==null) {
            System.err.println("WARNING: SMI_LOGS_ROOT not set, logging to pwd instead");
            logroot=".";
        }
        File logdir=new File(logroot+File.pathSeparator+getCaller());
        File logfile=new File(logroot+File.pathSeparator+getCaller()+File.pathSeparator+df.format(new Date())+"-"+getPid());
        if (!logdir.getParentFile().isDirectory()) {
            logdir.mkdirs();
        }

        // Turn off log4j warnings from library code
        Logger l = Logger.getRootLogger();
        l.setLevel(Level.OFF);

        Path logConfigPath;

        if (testing) {
            logConfigPath = Paths.get(ConfigFileName);
        } else {
            try {
                logConfigPath = Paths.get("./target", ConfigFileName);
            } catch (InvalidPathException e) {
                throw new SmiLoggingException("", e);
            }
        }

        if (Files.notExists(logConfigPath) || Files.isDirectory(logConfigPath))
            throw new SmiLoggingException("Could not find logback config file " + ConfigFileName);

        System.setProperty("logback.configurationFile", ConfigFileName);

        @SuppressWarnings("unchecked")
        Iterator<Appender> appenders = l.getAllAppenders().asIterator();
        while(appenders.hasNext()) {
            Appender app = appenders.next();
            if (app instanceof FileAppender) {
                FileAppender fa = (FileAppender)app;
                fa.setFile(logfile.getAbsolutePath());
                fa.setAppend(true);
                fa.setBufferedIO(true);
                fa.setImmediateFlush(false);
                fa.activateOptions();
            }
        }
    }
}
