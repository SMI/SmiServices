using System;
using Dicom;

namespace Microservices.Common.Tests
{
    public interface ITagRandomiser
    {
        DicomDictionaryEntry GetRandomTag(Random r);
        object GetRandomValue(DicomDictionaryEntry dicomDictionaryEntry, Random r);
    }
}
