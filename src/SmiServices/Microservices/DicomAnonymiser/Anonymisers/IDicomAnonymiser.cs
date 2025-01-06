using SmiServices.Common.Messages.Extraction;
using System.IO.Abstractions;

namespace SmiServices.Microservices.DicomAnonymiser.Anonymisers;

public interface IDicomAnonymiser
{
    /// <summary>
    /// Anonymise the specified <paramref name="sourceFile"/> to <paramref name="destFile"></paramref>.
    /// Implementations should assume that <paramref name="sourceFile"/> already exists, and <paramref name="destFile"></paramref> does not exist. 
    /// </summary>
    /// <param name="sourceFile"></param>
    /// <param name="destFile"></param>
    /// <param name="modality"></param>
    /// <param name="anonymiserStatusMessage"></param>
    /// <returns></returns>
    ExtractedFileStatus Anonymise(IFileInfo sourceFile, IFileInfo destFile, string modality, out string? anonymiserStatusMessage);
}
