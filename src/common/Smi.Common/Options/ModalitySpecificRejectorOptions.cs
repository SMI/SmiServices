using System;

namespace Smi.Common.Options
{
    /// <summary>
    /// Modality specific rejectors
    /// </summary>
    public class ModalitySpecificRejectorOptions
    {
        /// <summary>
        /// Comma separated list of modalities that this class applies to
        /// </summary>
        public string? Modalities { get; set; }

        /// <summary>
        /// True to override base modalities.  False to make both apply (i.e. this rejector should be used in addition to basic rejectors)
        /// </summary>
        public bool Overrides { get; set; }

        /// <summary>
        /// The Type of IRejector to use when evaluating the releaseability of dicom files of given <see cref="Modalities"/>
        /// </summary>
        public string? RejectorType { get; set; }

        public string[] GetModalities()
        {
            return string.IsNullOrWhiteSpace(Modalities) ? Array.Empty<string>() : Modalities.Split(new[] { ',' },StringSplitOptions.RemoveEmptyEntries);
        }

    }
}
