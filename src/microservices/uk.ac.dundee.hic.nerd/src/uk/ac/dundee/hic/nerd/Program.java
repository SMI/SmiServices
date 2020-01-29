package uk.ac.dundee.hic.nerd;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;

import edu.stanford.nlp.ie.crf.CRFClassifier;
import edu.stanford.nlp.ling.CoreAnnotations;
import edu.stanford.nlp.ling.CoreLabel;

public class Program {
	private final ServerSocket listener;
	private final CRFClassifier<CoreLabel> c;
	private final HashSet<String> classifications=new HashSet<String>(Arrays.asList(new String[] {"PERSON","LOCATION","ORGANIZATION"}));
	private volatile static boolean shutdown=false;
	
	public static void shutdown() {
		shutdown=true;
	}

	Program() throws IOException, ClassCastException, ClassNotFoundException {
		this.listener = new ServerSocket(1881,255,InetAddress.getByName("127.0.0.1"));
		c=CRFClassifier.getClassifier(new File("stanford-ner-2018-10-16/classifiers/english.all.3class.distsim.crf.ser.gz"));
	}
	
	private void handlein(Socket client) throws IOException {
		BufferedReader is = new BufferedReader(new InputStreamReader(client.getInputStream()));
		BufferedWriter out = new BufferedWriter(new OutputStreamWriter(client.getOutputStream()));
		StringBuilder sb=new StringBuilder();
		int ic;
		while ((ic=is.read())>-1) {
			sb.append((char)ic);
			if (ic==0) {
				System.err.println(sb);
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
		Socket client;
		while(!shutdown) {
			client=listener.accept();
			handlein(client);
			client.close();
		}
	}
	
	public static void main(String[] args) throws IOException, ClassCastException, ClassNotFoundException {
		Runtime.getRuntime().addShutdownHook(new Thread() {
			public void run() {
				shutdown();
			}
		});
		Program p = new Program();
		System.err.println("ok");
		p.run();
	}

}

