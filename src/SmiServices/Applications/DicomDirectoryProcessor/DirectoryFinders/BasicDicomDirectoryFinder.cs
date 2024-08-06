using SmiServices.Common.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders
{
    public class BasicDicomDirectoryFinder : DicomDirectoryFinder
    {
        /// <summary>
        /// True - Always go to bottom of directory structure
        /// False - If a directory contains dicom files do not enumerate it's subdirectories
        /// </summary>
        public bool AlwaysSearchSubdirectories { get; set; }

        public BasicDicomDirectoryFinder(string fileSystemRoot, IFileSystem fileSystem, string dicomSearchPattern, IProducerModel directoriesProducerModel)
        : base(fileSystemRoot, fileSystem, dicomSearchPattern, directoriesProducerModel) { }

        public BasicDicomDirectoryFinder(string fileSystemRoot, string dicomSearchPattern, IProducerModel directoriesProducerModel)
            : this(fileSystemRoot, new FileSystem(), dicomSearchPattern, directoriesProducerModel) { }


        public override void SearchForDicomDirectories(string topLevelDirectory)
        {
            Logger.Info("Starting directory scan of: " + topLevelDirectory);
            IsProcessing = true;
            TotalSent = 0;

            if (!FileSystem.Directory.Exists(topLevelDirectory))
                throw new DirectoryNotFoundException("Could not find the top level directory at the start of the scan \"" + topLevelDirectory + "\"");

            Times = new List<List<long>>();
            for (var i = 0; i < 6; ++i)
                Times.Add(new List<long>());

            var dirStack = new Stack<string>();
            dirStack.Push(topLevelDirectory);

            var largestStackSize = 1;

            while (dirStack.Count > 0 && !TokenSource.IsCancellationRequested)
            {
                Logger.Debug($"Start of loop, stack size is: {dirStack.Count}");

                string dir = dirStack.Pop();
                Logger.Debug($"Scanning {dir}");

                if (!FileSystem.Directory.Exists(dir))
                {
                    // Occurs too often on the VM for us to throw and exit from here, just have to log & continue for now
                    //throw new DirectoryNotFoundException("A previously seen directory can no longer be found: " + dir);

                    Logger.Warn($"Can no longer find {dir}, continuing");
                    continue;
                }

                // Lazy-evaluate the contents of the directory so we don't overwhelm the filesystem
                // and return on the first instance of a dicom file

                Stopwatch.Restart();
                StringBuilder = new StringBuilder();

                IDirectoryInfo dirInfo = FileSystem.DirectoryInfo.New(dir);
                LogTime(TimeLabel.NewDirInfo);

                IEnumerable<IFileInfo> fileEnumerator;
                try
                {
                    fileEnumerator = GetEnumerator(dirInfo);
                    LogTime(TimeLabel.EnumFiles);
                }
                catch (Exception e)
                {
                    Logger.Error($"Couldn't enumerate files: {e.Message}");
                    continue;
                }

                bool hasDicom = fileEnumerator.FirstOrDefault() != null;
                LogTime(TimeLabel.FirstOrDef);

                // If directory contains any DICOM files report and don't go any further
                if (hasDicom)
                {
                    FoundNewDicomDirectory(dir);
                    LogTime(TimeLabel.FoundNewDir);
                }

                if (!hasDicom || AlwaysSearchSubdirectories)
                {
                    Logger.Debug($"Enumerating subdirectories of {dir}");

                    IEnumerable<string> dirEnumerable = FileSystem.Directory.EnumerateDirectories(dir);
                    LogTime(TimeLabel.EnumDirs);

                    var totalSubDirs = 0;

                    foreach (string subDir in dirEnumerable)
                    {
                        dirStack.Push(subDir);
                        ++totalSubDirs;
                    }

                    if (dirStack.Count > largestStackSize)
                        largestStackSize = dirStack.Count;

                    LogTime(TimeLabel.PushDirs);
                    Logger.Debug($"Found {totalSubDirs} subdirectories");
                }

                Logger.Debug(StringBuilder.ToString);
            }

            IsProcessing = false;

            Logger.Info("Directory scan finished");
            Logger.Info($"Total messages sent: {TotalSent}");
            Logger.Info($"Largest stack size was: {largestStackSize}");

            if (TotalSent > 0)
                Logger.Info(CalcAverages());
        }
    }
}
