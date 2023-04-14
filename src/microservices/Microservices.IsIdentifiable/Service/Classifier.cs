using IsIdentifiable.Reporting;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Microservices.IsIdentifiable.Service
{
    public abstract class Classifier : IClassifier
    {
        public IDirectoryInfo DataDirectory { get; set; }


        protected Classifier(IDirectoryInfo dataDirectory)
        {
            DataDirectory = dataDirectory;

            if(!DataDirectory.Exists)
                throw new System.IO.DirectoryNotFoundException($"Could not find directory {DataDirectory.FullName}");
        }

        public abstract IEnumerable<Failure> Classify(IFileInfo dcm);

        /// <summary>
        /// Finds a single directory of a given name in the <see cref="DataDirectory"/> and asserts that it exists
        /// </summary>
        /// <param name="toFind"></param>
        /// <returns></returns>
        protected IDirectoryInfo GetSubdirectory(string toFind)
        {
            var stanfordNerDir = DataDirectory.GetDirectories(toFind).SingleOrDefault();
            
            if(stanfordNerDir == null)
                throw new System.IO.DirectoryNotFoundException($"Expected sub-directory called '{toFind}' to exist in '{DataDirectory}'");

            return stanfordNerDir;
        }
        

        /// <summary>
        /// Finds (including in subdirectories) files that match the <paramref name="searchPattern"/>.  If exactly 1 match is
        /// found then it is returned otherwise a
        /// </summary>
        /// <param name="searchPattern"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        protected IFileInfo FindOneFile(string searchPattern, IDirectoryInfo directory)
        {
            var files = directory.GetFiles(searchPattern, System.IO.SearchOption.AllDirectories).ToArray();

            return files.Length switch
            {
                0 => throw new System.IO.FileNotFoundException(
                    $"Expected 1 file matching '{searchPattern}' to exist in {directory}"),
                > 1 => throw new Exception(
                    $"Found '{files.Length}' file matching '{searchPattern}' in {directory} (expected 1)"),
                _ => files[0]
            };
        }

    }
}
