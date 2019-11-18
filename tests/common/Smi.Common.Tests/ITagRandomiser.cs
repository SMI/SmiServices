using System;
using Dicom;

namespace Smi.Common.Tests
{
    public interface ITagRandomiser
    {
        DicomDictionaryEntry GetRandomTag(Random r);
        object GetRandomValue(DicomDictionaryEntry dicomDictionaryEntry, Random r);
    }
}
