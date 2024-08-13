using SmiServices.Common.Messaging;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders
{
    public class PacsDirectoryFinder : DicomDirectoryFinder
    {
        // Regex that matches when we are at the yyyy\mm\dd\ directory level
        private readonly Regex _dayDirectoryRegex = new(@"(20\d{2}[\\\/]\d{2}[\\\/]\d{2})([\\\/]|$)");
        // Regex that matches when we are at the yyyy\mm\dd\xxxxx directory level
        private readonly Regex _accDirectoryRegex = new(@"(20\d{2}[\\\/]\d{2}[\\\/]\d{2}[\\\/][a-zA-Z0-9._-]+[\\\/]$)");


        public PacsDirectoryFinder(string fileSystemRoot, IFileSystem fileSystem, string dicomSearchPattern, IProducerModel directoriesProducerModel)
            : base(fileSystemRoot, fileSystem, dicomSearchPattern, directoriesProducerModel) { }

        public PacsDirectoryFinder(string fileSystemRoot, string dicomSearchPattern, IProducerModel directoriesProducerModel)
            : base(fileSystemRoot, new FileSystem(), dicomSearchPattern, directoriesProducerModel) { }


        public override void SearchForDicomDirectories(string rootDir)
        {
            Logger.Info("Starting directory scan of: " + rootDir);
            IsProcessing = true;
            TotalSent = 0;

            if (!FileSystem.Directory.Exists(rootDir))
                throw new DirectoryNotFoundException("Could not find the root directory at the start of the scan \"" + rootDir + "\"");

            // Check if we were given an accession directory
            if (_accDirectoryRegex.IsMatch(rootDir))
            {
                Logger.Debug("Given an accession directory, sending single message");
                FoundNewDicomDirectory(rootDir.Remove(0, FileSystemRoot.Length));
            }
            else
            {
                Times = [];
                for (var i = 0; i < 6; ++i)
                    Times.Add([]);

                var dirStack = new Stack<string>();
                dirStack.Push(rootDir);

                while (dirStack.Count > 0 && !TokenSource.IsCancellationRequested)
                {
                    string dir = dirStack.Pop();
                    Logger.Debug("Scanning " + dir);

                    IDirectoryInfo dirInfo = FileSystem.DirectoryInfo.New(dir);

                    if (!dirInfo.Exists)
                    {
                        Logger.Warn("Can no longer find " + dir + ", continuing");
                        continue;
                    }

                    IEnumerable<IDirectoryInfo> subDirs = dirInfo.EnumerateDirectories();

                    if (_dayDirectoryRegex.IsMatch(dir))
                    {
                        Logger.Debug("At the day level, assuming all subdirs are accession directories");
                        // At the day level, so each of the subdirectories will be accession directories
                        foreach (IDirectoryInfo accessionDir in subDirs)
                            FoundNewDicomDirectory(accessionDir.FullName);
                    }
                    else
                    {
                        Logger.Debug("Not at the day level, checking subdirectories");
                        subDirs.ToList().ForEach(x => dirStack.Push(x.FullName));
                    }
                }
            }

            IsProcessing = false;

            Logger.Info("Directory scan finished");
            Logger.Info("Total messages sent: " + TotalSent);
        }
    }
}
