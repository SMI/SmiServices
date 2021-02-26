using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmiRunner
{
    public static class Program
    {
        internal static int Main(string[] args)
        {
            IEnumerable<string> rest = args.Skip(1);
            int res = SmiCliInit.ParseServiceVerbAndRun(
                args.Take(1),
                new[]
                {
                    typeof(DicomTagReader),
                    typeof(TriggerUpdates),
                },
                service =>
                {
                    return service switch
                    {
                        DicomTagReader _ => Microservices.DicomTagReader.Program.Main(rest),
                        TriggerUpdates _ => Applications.TriggerUpdates.Program.Main(rest),
                        _ => throw new ArgumentException($"No case for {nameof(service)}")
                    };
                }
            );
            return res;
        }
    }
}
