
using Smi.Common.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;

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
            // TODO(rkm 2020-02-12) One enhancement here could be to keep track of the list of directories we've already seen, and not publish messages for any duplicates
            // (bp 2020-02-13) This is covered by the MongoDB query for extracting the accession directories and the script that prepares the list for ingest.

            Logger.Info("Starting accession directory path listing from: " + accessionsList);
            IsProcessing = true;
            TotalSent = 0;

            using (var reader = new StreamReader(accessionsList))
            {
                // TODO(rkm 2020-02-12) Add check for early cancellation here - see BasicDicomDirectoryFinder#L39
                // (bp 2020-02-13) Added
                while (!TokenSource.IsCancellationRequested)
                {
                    while (!reader.EndOfStream)
                    {
                        // TODO(rkm 2020-02-12) Possible null reference here
                        string accessionDirectory = reader.ReadLine().Replace(",", "");

                        // TODO(rkm 2020-02-12) Check for empty string - ignore blank lines in the csv
                        // (bp 2020-02-13) Both null references and empty strings are being caught by the regex check.
			//                 This has been tested with empty and null lines as well as lines composed only of commas.

                        if (_accDirectoryRegex.IsMatch(accessionDirectory))
                        {
                            if (!FileSystem.Directory.Exists(accessionDirectory))
                            {
                                Logger.Warn("Can not find " + accessionDirectory + ", continuing");
                                continue;
                            }

                            IDirectoryInfo dirInfo = FileSystem.DirectoryInfo.FromDirectoryName(accessionDirectory);
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

                            if (fileEnumerator.FirstOrDefault() != null)
                            {
                                Logger.Debug("Sending message (" + accessionDirectory + ")");
                                FoundNewDicomDirectory(accessionDirectory.Remove(0, FileSystemRoot.Length));
                            }
			    else
                            {
                                Logger.Warn("Could not find dicom files in the given accession directory (" + accessionDirectory + "), skipping");
                                continue;
                            }
                        }
                        else
                        {
                            Logger.Warn("This path does not point to an accession directory: (" + accessionDirectory + "), continuing");
                        }
                    }
                }
            }

            IsProcessing = false;

            Logger.Info("Reading from list finished");
            Logger.Info("Total messages sent: " + TotalSent);
        }
    }
}

