﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Reporting.Reports;

namespace Microservices.IsIdentifiable.Service
{
    public abstract class Classifier : IClassifier
    {
        public DirectoryInfo DataDirectory { get; set; }


        protected Classifier(DirectoryInfo dataDirectory)
        {
            DataDirectory = dataDirectory;

            if(!DataDirectory.Exists)
                throw new DirectoryNotFoundException($"Could not find directory {DataDirectory.FullName}");
        }

        public abstract IEnumerable<Reporting.Failure> Classify(FileInfo dcm);

        /// <summary>
        /// Finds a single directory of a given name in the <see cref="DataDirectory"/> and asserts that it exists
        /// </summary>
        /// <param name="toFind"></param>
        /// <returns></returns>
        protected DirectoryInfo GetSubdirectory(string toFind)
        {
            var stanfordNerDir = DataDirectory.GetDirectories(toFind).SingleOrDefault();
            
            if(stanfordNerDir == null)
                throw new DirectoryNotFoundException($"Expected sub-directory called '{toFind}' to exist in '{DataDirectory}'");

            return stanfordNerDir;
        }
        

        /// <summary>
        /// Finds (including in subdirectories) files that match the <paramref name="searchPattern"/>.  If exactly 1 match is
        /// found then it is returned otherwise a
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        protected FileInfo FindOneFile(string searchPattern, DirectoryInfo directory)
        {
            var files = directory.GetFiles(searchPattern, SearchOption.AllDirectories).ToArray();

            if(files.Length  == 0)
                throw new FileNotFoundException($"Expected 1 file matching '{searchPattern}' to exist in {directory}");
            
            if(files.Length > 1)
                throw new Exception($"Found '{files.Length}' file matching '{searchPattern}' in {directory} (expected 1)");

            return files[0];
        }

    }
}