﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using NLog;

namespace Applications.DicomDirectoryProcessor.Execution.DirectoryFinders
{
    /// <summary>
    /// Finds directories that contain DICOM files and outputs one AccessionDirectoryMessage for each directory it finds
    /// that contains a *.dcm file (or <see cref="SearchPattern"/>). It will search into subdirectories but will not search
    /// into subdirectories below any directory that does contain a *.dcm file (or <see cref="SearchPattern"/>).
    /// </summary>
    public abstract class DicomDirectoryFinder : IDicomDirectoryFinder
    {
        protected readonly ILogger Logger;

        protected readonly string FileSystemRoot;
        protected readonly IFileSystem FileSystem;

        private readonly IProducerModel _directoriesProducerModel;
        protected int TotalSent;

        protected bool IsProcessing;
        protected readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        protected readonly Stopwatch Stopwatch = new Stopwatch();
        protected StringBuilder StringBuilder;
        protected List<List<long>> Times;

        /// <summary>
        /// The filenames to look for in directories.  Defaults to *.dcm
        /// </summary>
        protected readonly string SearchPattern;

        protected enum TimeLabel
        {
            NewDirInfo,
            EnumFiles,
            FirstOrDef,
            FoundNewDir,
            EnumDirs,
            PushDirs
        }


        protected DicomDirectoryFinder(string fileSystemRoot, IFileSystem fileSystem, string dicomSearchPattern, IProducerModel directoriesProducerModel)
        {
            FileSystemRoot = fileSystemRoot;
            FileSystem = fileSystem;
            SearchPattern = dicomSearchPattern;
            _directoriesProducerModel = directoriesProducerModel;
            Logger = LogManager.GetLogger(GetType().Name);
        }

        public abstract void SearchForDicomDirectories(string rootDir);

        public void Stop()
        {
            if (!IsProcessing)
                return;

            Logger.Info("Stop requested while still processing, attempting to kill");
            TokenSource.Cancel();

            var timeout = 5000;
            const int delta = 500;
            while (IsProcessing && timeout > 0)
            {
                Thread.Sleep(delta);
                timeout -= delta;
            }

            if (timeout <= 0)
                throw new ApplicationException("SearchForDicomDirectories did not exit in time");

            Logger.Info("Directory scan aborted, exiting");
        }

        /// <summary>
        /// Handled when a new DICOM directory is found. Writes an AccessionDirectoryMessage to the message exchange
        /// </summary>
        /// <param name="dir">Full path to a directory that has been found to contain a DICOM file</param>
        protected void FoundNewDicomDirectory(string dir)
        {
            Logger.Debug("DicomDirectoryFinder: Found " + dir);

            string dirPath = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar);

            if (dirPath.StartsWith(FileSystemRoot))
                dirPath = dirPath.Remove(0, FileSystemRoot.Length);

            dirPath = dirPath.TrimStart(Path.DirectorySeparatorChar);

            var message = new AccessionDirectoryMessage
            {
                DirectoryPath = dirPath,
                NationalPACSAccessionNumber = Path.GetFileName(dirPath)
            };

            _directoriesProducerModel.SendMessage(message, null);
            ++TotalSent;
        }

        protected void LogTime(TimeLabel tl)
        {
            long elapsed = Stopwatch.ElapsedMilliseconds;
            StringBuilder.Append(tl + "=" + elapsed + "ms ");
            Times[(int)tl].Add(elapsed);
            Stopwatch.Restart();
        }

        protected string CalcAverages()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Averages:");

            foreach (TimeLabel label in (TimeLabel[])Enum.GetValues(typeof(TimeLabel)))
            {
                int count = Times[(int)label].Count;
                long average = count == 0 ? 0 : Times[(int)label].Sum() / count;

                sb.AppendLine(label + ":\t" + average + "ms");
            }

            return sb.ToString();
        }
    }
}
