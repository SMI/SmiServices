
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Smi.Common.Messaging;

namespace Applications.DicomDirectoryProcessor.Execution.DirectoryFinders
{
    public class AccessionDirectoryLister : DicomDirectoryFinder
    {
        // Regex that matches when we are at the yyyy\mm\dd\xxxxx directory level
        private readonly Regex _accDirectoryRegex = new Regex(@"(20\d{2}[\\\/]\d{2}[\\\/]\d{2})[\\\/]\w+[^.]*?$");

        public AccessionDirectoryLister(string fileSystemRoot, IFileSystem fileSystem, string dicomSearchPattern, IProducerModel directoriesProducerModel)
        : base(fileSystemRoot, fileSystem, dicomSearchPattern, directoriesProducerModel) { }

        public AccessionDirectoryLister(string fileSystemRoot, string dicomSearchPattern, IProducerModel directoriesProducerModel)
            : this(fileSystemRoot, new FileSystem(), dicomSearchPattern, directoriesProducerModel) { }


        public override void SearchForDicomDirectories(string accessionsList)
        {
            Logger.Info("Starting accession directory path listing from: " + accessionsList);
            IsProcessing = true;
            TotalSent = 0;

            using (var reader = new StreamReader(accessionsList))
            {
                // TODO(rkm 2020-02-12) Add check for early cancellation here - see BasicDicomDirectoryFinder#L39
                while (!reader.EndOfStream)
                {
                    // TODO(rkm 2020-02-12) Possible null reference here
                    string accessionDirectory = reader.ReadLine().Replace(",", "");

                    // TODO(rkm 2020-02-12) Check for empty string - ignore blank lines in the csv

                    if (_accDirectoryRegex.IsMatch(accessionDirectory))
                    {
                        if (!FileSystem.Directory.Exists(accessionDirectory))
                        {
                            Logger.Warn("Can not find " + accessionDirectory + ", continuing");
                            continue;
                        }

                        Logger.Debug("Sending message (" + accessionDirectory + ")");
                        FoundNewDicomDirectory(accessionDirectory.Remove(0, FileSystemRoot.Length));
                    }
                    else
                    {
                        Logger.Warn("This path does not point to an accession directory: (" + accessionDirectory + "), continuing");
                    }
                }
            }

            IsProcessing = false;

            Logger.Info("Reading from list finished");
            Logger.Info("Total messages sent: " + TotalSent);
        }
    }
}

