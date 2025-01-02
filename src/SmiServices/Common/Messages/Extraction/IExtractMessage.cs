using System;

namespace SmiServices.Common.Messages.Extraction;

/// <summary>
/// Interface for all messages relating to the extract process
/// </summary>
public interface IExtractMessage : IMessage
{
    /// <summary>
    /// Unique identifier to link messages from different extract requests
    /// </summary>
    Guid ExtractionJobIdentifier { get; }

    /// <summary>
    /// Project number used by eDRIS for reference, and for the base extraction output relative to the ExtractRoot
    /// </summary>
    string ProjectNumber { get; }

    /// <summary>
    /// Directory relative to the ExtractRoot to place anonymised files into
    /// </summary>
    string ExtractionDirectory { get; }

    /// <summary>
    /// The modality to extract
    /// </summary>
    public string Modality { get; }

    /// <summary>
    /// DateTime the job was submitted at
    /// </summary>
    DateTime JobSubmittedAt { get; set; }

    /// <summary>
    /// True if this is an identifiable extraction (i.e. files should not be anonymised)
    /// </summary>
    bool IsIdentifiableExtraction { get; }

    /// <summary>
    /// True if this is a "no filters" (i.e. no file rejection filters should be applied)
    /// </summary>
    bool IsNoFilterExtraction { get; }

    /// <summary>
    /// True if this extraction uses the global pool of DICOM files
    /// </summary>
    bool IsPooledExtraction { get; }
}
