using NLog;
using Smi.Common.Execution;
using Smi.Common.Options;
using SmiServices.Microservices.DicomTagReader.Execution;
using System;
using System.Collections.Generic;

namespace SmiServices.Microservices.DicomTagReader
{
    public static class Program
    {
        /// <summary>
        /// Program entry point when run from the command line
        /// </summary>
        /// <param name="args"></param>
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit.ParseAndRun<DicomTagReaderCliOptions>(args, typeof(Program), OnParse);
            return ret;
        }

        private static int OnParse(GlobalOptions globals, DicomTagReaderCliOptions opts)
        {
            if (opts.File != null)
            {
                try
                {
                    var host = new DicomTagReaderHost(globals);
                    host.AccessionDirectoryMessageConsumer.RunSingleFile(opts.File);
                    return 0;
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);
                    return 1;
                }
            }

            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomTagReaderHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
