Set MessageHeader.CurrentProgramName once at the start of each test fixture (project / assembly) instead of individually in each test. Fixes cases where tests would fail if run individually.
