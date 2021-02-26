using Smi.Common.Options;
using System;
using System.Linq;

namespace SmiRunner
{
    public class Program
    {
        internal static int Main(string[] args)
        {
            var rest = args.Skip(1);
            int res = SmiCliInit.ParseServiceVerbAndRun(
                args.Take(1),
                new[]
                {
                    typeof(DicomTagReader),
                },
                service =>
                {
                    return service switch
                    {
                        DicomTagReader _ => Microservices.DicomTagReader.Program.Main(rest),
                        _ => throw new ArgumentException($"No case for {nameof(service)}")
                    };
                }
            );
            return res;
        }
    }
}
