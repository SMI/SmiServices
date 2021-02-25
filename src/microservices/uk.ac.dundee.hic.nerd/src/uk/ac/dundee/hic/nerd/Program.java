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
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;
import java.util.zip.GZIPInputStream;

import edu.stanford.nlp.ie.crf.CRFClassifier;
import edu.stanford.nlp.ling.CoreAnnotations;
import edu.stanford.nlp.ling.CoreLabel;

/**
 * Main program class - loads Stanford NER model data, provides a service on localhost TCP port 1881.
 * 
 * @author jas88
 *
 */
public class Program {
	private final ServerSocket listener;
	private final CRFClassifier<CoreLabel> c;
	private final HashSet<String> classifications=new HashSet<String>(Arrays.asList(new String[] {"PERSON","LOCATION","ORGANIZATION"}));
	private volatile static boolean shutdown=false;

	/**
	 * Shut down the listening socket and set the termination condition
	 */
	public void shutdown() {
		try {
			listener.close();
		} catch (IOException e) {
			// Ignore exceptions on close(), can't do anything about that anyway
		}
		shutdown=true;
	}

	/**
	 * Constructor: loads NER model data and passes to Stanford NER code, binds to TCP localhost:1881
	 * 
	 * @throws IOException If unable to bind TCP socket
	 * @throws ClassCastException Only thrown from within Stanford NER initialisation
	 * @throws ClassNotFoundException Only thrown from within Stanford NER initialisation
	 */
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
	
	/**
	 * Loop reading null-terminated queries on the client TCP socket, returning results the same way.
	 * 
	 * @param client
	 * @throws IOException If client terminates connection unexpectedly - ignore
	 */
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

	/**
	 * Process a single word/phrase query and return the response text.
	 * Returns either one or more label responses (null-delimited 3-tuples of type,offset,word) or a single null,
	 * terminated by an additional null, so the final two octets are always double-null.
	 * @param l	Input string, for example "University"
	 * @return
	 */
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

	/**
	 * Loop, accepting new service clients until shut down elsewhere.
	 */
	public void run() {
		while(!shutdown) {
			try {
				final Socket client = listener.accept();
				new Thread() {
					public void run() {
						try {
							handlein(client);
							client.close();
						} catch (IOException e) {
							// Ignore IOExceptions, since they are client disconnections
						}
						System.gc();
					}
				}.start();
			} catch (IOException e) {
				// This means we were shut down.
				return;
			}
		}
	}

	/**
	 * Command line initialisation: load the program then run disconnected as a daemon (until SIGTERM).
	 * Any early-stage exception will be reported to the console before disconnection.
	 * 
	 * @param args
	 * @throws IOException
	 * @throws ClassCastException
	 * @throws ClassNotFoundException
	 */
	public static void main(String[] args) throws IOException, ClassCastException, ClassNotFoundException {
		Program p = new Program();
		System.err.println("nerd started OK");
		System.out.close();
		System.err.close();
		p.run();
	}

}
