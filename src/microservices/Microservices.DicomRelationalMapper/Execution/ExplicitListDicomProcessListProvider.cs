using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;
using System;
using System.Collections.Generic;
using System.IO;
using Rdmp.Dicom.Extraction.FoDicomBased;

namespace Microservices.DicomRelationalMapper.Execution;

public class ExplicitListDicomFileWorklist : IDicomFileWorklist
{
    private readonly IEnumerator<string> _filesAndOrDirectories;

    public ExplicitListDicomFileWorklist(IEnumerable<string> filesAndOrDirectories)
    {
        _filesAndOrDirectories = filesAndOrDirectories.GetEnumerator();
    }

    public bool GetNextFileOrDirectoryToProcess(out DirectoryInfo directory, out AmbiguousFilePath file)
    {
        directory = null;
        file = null;

        if (_filesAndOrDirectories.MoveNext())
        {
            return false;
        }

        if (File.Exists(_filesAndOrDirectories.Current))
        {
            file = new AmbiguousFilePath(_filesAndOrDirectories.Current);
            return true;
        }

        if (Directory.Exists(_filesAndOrDirectories.Current))
        {
            directory = new DirectoryInfo(_filesAndOrDirectories.Current);
            return true;
        }

        throw new Exception(
            $"Array element '{_filesAndOrDirectories.Current}' of filesAndOrDirectories was not a File or Directory (or the referenced file did not exist).");
    }
}
