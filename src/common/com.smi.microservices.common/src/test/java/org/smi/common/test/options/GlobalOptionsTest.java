package org.smi.common.test.options;

import org.smi.common.options.GlobalOptions;

import junit.framework.TestCase;

public class GlobalOptionsTest extends TestCase {
	
	private GlobalOptions _options;
	
	protected void setUp() throws Exception {
		
		super.setUp();
		_options = GlobalOptions.Load(true);
	}

	protected void tearDown() throws Exception {
		
		_options = null;
		super.tearDown();
	}
	
	//TODO Add proper tests here	
	
	public void testEmpty() {
		
	}
}
