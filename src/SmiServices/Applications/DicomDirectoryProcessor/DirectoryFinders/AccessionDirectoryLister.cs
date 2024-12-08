using SmiServices.Common.Messaging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using SmiServices.Common.Messages;

namespace SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders
{
    public class AccessionDirectoryLister : DicomDirectoryFinder
    {
        // Regex that matches when we are at the yyyy\mm\dd\xxxxx directory level
        private static readonly Regex _accDirectoryRegex = new(@"(20\d{2}[\\\/]\d{2}[\\\/]\d{2}[\\\/][a-zA-Z0-9._-]+[\\\/]$)");

        public AccessionDirectoryLister(string fileSystemRoot, IFileSystem fileSystem, string dicomSearchPattern, IProducerModel<AccessionDirectoryMessage> directoriesProducerModel)
            : base(fileSystemRoot, fileSystem, dicomSearchPattern, directoriesProducerModel) { }

        public AccessionDirectoryLister(string fileSystemRoot, string dicomSearchPattern, IProducerModel<AccessionDirectoryMessage> directoriesProducerModel)
            : this(fileSystemRoot, new FileSystem(), dicomSearchPattern, directoriesProducerModel) { }


        public override void SearchForDicomDirectories(string accessionsList)
        {
            Logger.Info($"Starting accession directory path listing from: {accessionsList}");
            IsProcessing = true;
            TotalSent = 0;

            using var reader = FileSystem.File.OpenText(accessionsList);
            while (!reader.EndOfStream && !TokenSource.IsCancellationRequested)
            {

                var accessionDirectory = reader.ReadLine()?.Replace(",", "");

                if (accessionDirectory is null || !_accDirectoryRegex.IsMatch(accessionDirectory))
                {
                    Logger.Warn($"This path does not point to an accession directory: ({accessionDirectory}), continuing");
                    continue;
                }

                if (!FileSystem.Directory.Exists(accessionDirectory))
                {
                    Logger.Warn($"Can not find {accessionDirectory}, continuing");
                    continue;
                }

                var dirInfo = FileSystem.DirectoryInfo.New(accessionDirectory);
                IEnumerable<IFileInfo> fileEnumerator;

                try
                {
                    fileEnumerator = dirInfo.EnumerateFiles(SearchPattern);
                }
                catch (Exception e)
                {
                    Logger.Error($"Could not enumerate files: {e.Message}");
                    continue;
                }

                if (fileEnumerator.FirstOrDefault() == null)
                {
                    Logger.Warn(
                        $"Could not find dicom files in the given accession directory ({accessionDirectory}), skipping");
                    continue;
                }

                Logger.Debug($"Sending message ({accessionDirectory})");
                FoundNewDicomDirectory(accessionDirectory.Remove(0, FileSystemRoot.Length));
            }

            IsProcessing = false;

            Logger.Info("Reading from list finished");
            Logger.Info($"Total messages sent: {TotalSent}");
        }
    }
}
