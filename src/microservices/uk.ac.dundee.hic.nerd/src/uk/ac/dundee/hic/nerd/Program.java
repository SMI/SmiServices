package uk.ac.dundee.hic.nerd;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.SocketException;
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;
import java.util.zip.GZIPInputStream;

import edu.stanford.nlp.ie.crf.CRFClassifier;
import edu.stanford.nlp.ling.CoreAnnotations;
import edu.stanford.nlp.ling.CoreLabel;

public class Program {
	private final ServerSocket listener;
	private final CRFClassifier<CoreLabel> c;
	private final HashSet<String> classifications=new HashSet<String>(Arrays.asList(new String[] {"PERSON","LOCATION","ORGANIZATION"}));
	private volatile static boolean shutdown=false;
	
	public void shutdown() {
		try {
			listener.close();
		} catch (IOException e) {
			// Ignore exceptions on close(), can't do anything about that anyway
		}
		shutdown=true;
	}

	Program() throws IOException, ClassCastException, ClassNotFoundException {
		this.listener = new ServerSocket(1881,255,InetAddress.getByName("127.0.0.1"));
		Runtime.getRuntime().addShutdownHook(new Thread() {
			public void run() {
				shutdown();
			}
		});
		InputStream stream = Program.class.getResourceAsStream("/english.all.3class.distsim.crf.ser.gz");
		assert(stream!=null);
		c=CRFClassifier.getClassifier(new GZIPInputStream(stream));
	}
	
	private void handlein(Socket client) throws IOException {
		BufferedReader is = new BufferedReader(new InputStreamReader(client.getInputStream()));
		BufferedWriter out = new BufferedWriter(new OutputStreamWriter(client.getOutputStream()));
		StringBuilder sb=new StringBuilder();
		int ic;
		while ((ic=is.read())>-1) {
			sb.append((char)ic);
			if (ic==0) {
				out.append(handleText(sb.toString()));
				out.flush();
				sb.setLength(0);
			}
		}
	}
	
	private String handleText(String l) {
		StringBuilder s=new StringBuilder();
		List<List<CoreLabel>> r = c.classify(l);
		for (List<CoreLabel> ls:r) {
			for (CoreLabel cl : ls) {
				String classif = cl.getString(CoreAnnotations.AnswerAnnotation.class);
				if (classifications.contains(classif)) {
					s.append(classif);
					s.append((char)0);
					s.append(Integer.toString(cl.beginPosition()));
					s.append((char)0);
					s.append(cl.value());
					s.append((char)0);
				}
			}
		}
		if (s.length()==0)
			s.append((char)0);	// Double-null if no matches.
		s.append((char)0);
		return s.toString();
	}

	void run() throws IOException {
		while(!shutdown) {
			try {
				Socket client = listener.accept();
				new Thread() {
					public void run() {
						try {
							handlein(client);
							client.close();
						} catch (IOException e) {
							// Ignore IOExceptions, since they are client disconnections
						}
					}
				}.start();
			} catch (SocketException e) {
				// This means we were shut down.
				return;
			}
		}
	}
	
	public static void main(String[] args) throws IOException, ClassCastException, ClassNotFoundException {
		Program p = new Program();
		System.err.println("nerd started OK");
		System.out.close();
		System.err.close();
		p.run();
	}

}

