
package org.smi.common.logging;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.lang.management.ManagementFactory;
import java.lang.management.RuntimeMXBean;
import java.lang.reflect.Field;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Properties;
import java.util.Random;

import org.apache.log4j.ConsoleAppender;
import org.apache.log4j.Level;
import org.apache.log4j.LogManager;
import org.apache.log4j.Logger;
import org.apache.log4j.PatternLayout;
import org.apache.log4j.PropertyConfigurator;
import org.apache.log4j.WriterAppender;

/**
 * Static helper class to setup the SMI logging
 */
public final class SmiLogging {

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
                while (prev.indexOf('.')!=-1 && prev.indexOf('.')!=prev.lastIndexOf('.')) {
                    prev=prev.substring(prev.indexOf('.')+1);
                }
                if (prev.indexOf('.')!=-1)
                    prev=prev.substring(0,prev.indexOf('.'));
                return prev;
            }
        }
        return prev==null?null:prev.substring(prev.lastIndexOf('.')+1);
    }


    /**
     * Get the current PID (on Java 9 or later)
     * @return
     */
    public static long getPid() {
        try {
            Class<?> phclass = Class.forName("java.lang.ProcessHandle");
            Method phcurrent = phclass.getMethod("current", new Class<?>[] {});
            Method getpid=phclass.getMethod("pid", new Class<?>[] {});
            Object self=phcurrent.invoke(null, new Object[] {});
            Object pid=getpid.invoke(self, new Object[] {});
            if (pid instanceof Long) {
                Long lpid=(Long) pid;
                return lpid.longValue();
            }
        } catch (ClassNotFoundException | NoSuchMethodException | SecurityException | IllegalAccessException | IllegalArgumentException | InvocationTargetException e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }

        // Fallback for elderly JVMs:
        try {
            RuntimeMXBean rt=ManagementFactory.getRuntimeMXBean();
            Field jvm=rt.getClass().getField("jvm");
            jvm.setAccessible(true);
            Object mgmt = jvm.get(rt);
            Method getpid = mgmt.getClass().getDeclaredMethod("getProcessId");
            getpid.setAccessible(true);
            int pid=(Integer)getpid.invoke(mgmt);
            return pid;
        } catch (NoSuchFieldException | IllegalArgumentException | IllegalAccessException | NoSuchMethodException | SecurityException | InvocationTargetException e) {
            e.printStackTrace();
        }

        return new Random().nextLong();
    }

    public static void Setup(boolean testing) throws SmiLoggingException, IOException {
        DateFormat df = new SimpleDateFormat("yyyy-MM-dd-HH-mm-ss");
        String logroot = System.getenv("SMI_LOGS_ROOT");
        if (logroot==null) {
            System.err.println("WARNING: SMI_LOGS_ROOT not set, logging to cwd instead");
            logroot=".";
        }
        String caller=getCaller();
        File logdir=new File(logroot+File.separator+caller);
        File logfile=new File(logroot+File.separator+caller+File.separator+df.format(new Date())+"-"+getPid());
        if (!logdir.isDirectory()) {
            logdir.mkdirs();
        }

        Properties props = new Properties();
        props.put("log4j.logger.org.dcm4cheri", "INFO");
        props.put("log4j.logger.org.rsna", "INFO");
        PropertyConfigurator.configure(props);

        Logger l = Logger.getRootLogger();
        l.setLevel(testing ? Level.ALL : Level.DEBUG);

        PatternLayout pl = new PatternLayout("%d{HH:mm:ss.SSS}|%t|%-5p|%-15C| %m%n");

        ConsoleAppender ca = new ConsoleAppender();
        l.addAppender(ca);

        WriterAppender fa = new WriterAppender(pl,new FileWriter(logfile.getAbsolutePath(),true));
        fa.setImmediateFlush(true);
        fa.setLayout(pl);
        fa.activateOptions();
        l.addAppender(fa);
    }
}
