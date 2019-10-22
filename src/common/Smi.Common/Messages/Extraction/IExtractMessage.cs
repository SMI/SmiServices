
using System;

namespace Smi.Common.Messages.Extraction
{
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
        /// DateTime the job was submitted at
        /// </summary>
        DateTime JobSubmittedAt { get; set; }
    }
}
