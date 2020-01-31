package uk.ac.dundee.hic.nerd;

import java.io.IOException;
import java.net.InetAddress;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import uk.ac.dundee.hic.nerd.Program;

public class UnitTests {
	boolean ok = false;
	
	private static void hexdump(byte[] a) {
		// Abort unless running under Eclipse, avoid spamming Travis/console
		StackTraceElement[] trace = new Throwable().getStackTrace();
		if (!trace[trace.length-1].getClassName().startsWith("org.eclipse")) return;
		
		System.out.print("new byte[] {");
		for (byte b : a) {
			if (b>='A' && b<='Z')
				System.out.print("'"+(char)b+"',");
			else if (b>='a' && b<='z')
				System.out.print("'"+(char)b+"',");
			else if (b>='0' && b<='9')
				System.out.print("'"+(char)b+"',");
			else if (b>=' ' && b<=' ')
				System.out.print("'"+(char)b+"',");
			else
				System.out.print(String.format("'\\u%04X',",b));
		}
		System.out.print("}");
	}

	@Test public void testClassifier() throws ClassCastException, ClassNotFoundException, IOException, InterruptedException {
		Program p = new Program();
		Thread t = new Thread() {
			public void run() {
				try {
					p.run();
					ok=true;
				} catch (IOException e) {
					e.printStackTrace();
				}
			}
		};
		t.start();
		Socket sock = new Socket(InetAddress.getByName("localhost"),1881);
		sock.getOutputStream().write("University of Dundee\0".getBytes(StandardCharsets.UTF_8));
		byte[] buff = new byte[1024];
		byte[] reply = Arrays.copyOf(buff,sock.getInputStream().read(buff, 0, buff.length));
		hexdump(reply);
		Assertions.assertArrayEquals(reply, new byte[] {'O','R','G','A','N','I','Z','A','T','I','O','N','\u0000','0','\u0000','U','n','i','v','e','r','s','i','t','y','\u0000','O','R','G','A','N','I','Z','A','T','I','O','N','\u0000','1','1','\u0000','o','f','\u0000','O','R','G','A','N','I','Z','A','T','I','O','N','\u0000','1','4','\u0000','D','u','n','d','e','e','\u0000','\u0000',});
		sock.close();
		p.shutdown();
		t.join();
		Assertions.assertEquals(ok, true);
	}
}
