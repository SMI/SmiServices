using Rdmp.Dicom.Extraction.FoDicomBased;
using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;
using System;
using System.IO;

namespace SmiServices.Microservices.DicomRelationalMapper
{
    public class ExplicitListDicomFileWorklist : IDicomFileWorklist
    {
        private readonly string[] _filesAndOrDirectories;
        private int index = 0;

        public ExplicitListDicomFileWorklist(string[] filesAndOrDirectories)
        {
            _filesAndOrDirectories = filesAndOrDirectories;
        }

        public bool GetNextFileOrDirectoryToProcess(out DirectoryInfo? directory, out AmbiguousFilePath? file)
        {
            directory = null;
            file = null;

            if (index >= _filesAndOrDirectories.Length)
            {
                return false;
            }

#pragma warning disable IO0002 // Replace File class with IFileSystem.File for improved testability
            if (File.Exists(_filesAndOrDirectories[index]))
            {
                file = new AmbiguousFilePath(_filesAndOrDirectories[index]);
                index++;
                return true;
            }
#pragma warning restore IO0002 // Replace File class with IFileSystem.File for improved testability

#pragma warning disable IO0003 // Replace Directory class with IFileSystem.Directory for improved testability
            if (Directory.Exists(_filesAndOrDirectories[index]))
            {
#pragma warning disable IO0007 // Replace DirectoryInfo class with IFileSystem.DirectoryInfo for improved testability
                directory = new DirectoryInfo(_filesAndOrDirectories[index]);
#pragma warning restore IO0007 // Replace DirectoryInfo class with IFileSystem.DirectoryInfo for improved testability
                index++;
                return true;
            }
#pragma warning restore IO0003 // Replace Directory class with IFileSystem.Directory for improved testability

            throw new Exception("Array element " + index + " of filesAndOrDirectories was not a File or Directory (or the referenced file did not exist).  Array element is '" + _filesAndOrDirectories[index] + "'");
        }
    }
}
