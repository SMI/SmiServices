using Smi.Common.Messages.Extraction;
using System.IO.Abstractions;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    public interface IDicomAnonymiser
    {
        /// <summary>
        /// Anonymise the specified <paramref name="sourceFile"/> to <paramref name="destFile"></paramref> based on the provided <paramref name="message"/> modality.
        /// Implementations should assume that <paramref name="sourceFile"/> already exists, and <paramref name="destFile"></paramref> does not exist. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        // ExtractedFileStatus Anonymise(IFileInfo sourceFile, IFileInfo destFile);

        ExtractedFileStatus Anonymise(ExtractFileMessage message, IFileInfo sourceFile, IFileInfo destFile);
    }
}
