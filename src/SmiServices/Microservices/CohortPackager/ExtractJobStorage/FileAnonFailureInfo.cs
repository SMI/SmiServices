using System;


namespace SmiServices.Microservices.CohortPackager.ExtractJobStorage;

/// <summary>
/// Provides information on why a requested file could not be anonymised
/// </summary>
public class FileAnonFailureInfo
{
    /// <summary>
    /// The path of the original DICOM file which could not be extracted
    /// </summary>
    public readonly string DicomFilePath;

    /// <summary>
    /// The reason for the file not being extracted
    /// </summary>
    public readonly string Reason;

    public FileAnonFailureInfo(
        string dicomFilePath,
        string reason
    )
    {
        DicomFilePath = string.IsNullOrWhiteSpace(dicomFilePath) ? throw new ArgumentException(null, nameof(dicomFilePath)) : dicomFilePath;
        Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException(null, nameof(reason)) : reason;
    }
}
