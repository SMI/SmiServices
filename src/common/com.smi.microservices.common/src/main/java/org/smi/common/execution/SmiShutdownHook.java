
package org.smi.common.execution;

import java.io.IOException;

public final class SmiShutdownHook extends Thread {

	private IMicroserviceHost _host;

	public SmiShutdownHook(IMicroserviceHost host) {

		_host = host;
	}

	/**
	 * Called as JVM is exiting. Program will finish after this method runs, so use
	 * the Shutdown callback to clean up anything before then.
	 */
	@Override
	public void run() {
		if (_host != null)
			try {
				_host.Shutdown();
			} catch (IOException e) {
				e.printStackTrace();
			}
	}
}
