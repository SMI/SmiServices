using Microservices.DicomRelationalMapper.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;
using System.Collections.Generic;

namespace Microservices.DicomRelationalMapper;

public static class Program
{
    public static int Main(IEnumerable<string> args) => SmiCliInit.ParseAndRun<CliOptions>(args, typeof(Program), OnParse);

    private static int OnParse(GlobalOptions globals, CliOptions opts) =>
        new MicroserviceHostBootstrapper(() => new DicomRelationalMapperHost(globals)).Main();
}
