package org.smi.common.util;

import java.net.URL;
import java.net.URLClassLoader;

public class Utils {

	public static boolean isNullOrWhitespace(String s) {
		return s == null || s.length() == 0 || isWhitespace(s);
	}

	private static boolean isWhitespace(String s) {
		return s.matches("\\s");
	}
	
	public static void printClasspath() {
		
        ClassLoader cl = ClassLoader.getSystemClassLoader();

        URL[] urls = ((URLClassLoader)cl).getURLs();

        for(URL url: urls){
        	System.out.println(url.getFile());
        }
	}
}
