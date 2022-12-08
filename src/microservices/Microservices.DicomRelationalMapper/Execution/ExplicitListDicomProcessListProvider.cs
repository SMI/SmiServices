using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;
using System;
using System.IO;
using Rdmp.Dicom.Extraction.FoDicomBased;

namespace Microservices.DicomRelationalMapper.Execution
{
    public class ExplicitListDicomFileWorklist : IDicomFileWorklist
    {
        private readonly string[] _filesAndOrDirectories;
        private int index = 0;

        public ExplicitListDicomFileWorklist(string[] filesAndOrDirectories)
        {
            _filesAndOrDirectories = filesAndOrDirectories;
        }

        public bool GetNextFileOrDirectoryToProcess(out DirectoryInfo directory, out AmbiguousFilePath file)
        {
            directory = null;
            file = null;

            if (index >= _filesAndOrDirectories.Length)
            {
                return false;
            }

            if (File.Exists(_filesAndOrDirectories[index]))
            {
                file = new AmbiguousFilePath(_filesAndOrDirectories[index]);
                index++;
                return true;
            }

            if (Directory.Exists(_filesAndOrDirectories[index]))
            {
                directory = new DirectoryInfo(_filesAndOrDirectories[index]);
                index++;
                return true;
            }

            throw new Exception("Array element " + index + " of filesAndOrDirectories was not a File or Directory (or the referenced file did not exist).  Array element is '" + _filesAndOrDirectories[index] + "'");
        }
    }
}
