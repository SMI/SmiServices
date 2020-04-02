package org.smi.common.test.options;

import org.smi.common.options.GlobalOptions;

import junit.framework.TestCase;

public class GlobalOptionsTest extends TestCase {

    public void testLoad() throws Exception {
        GlobalOptions.Load(true);
    }
}
