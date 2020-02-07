using CommandLine;

namespace Microservices.IsIdentifiable.Options
{
    /// <summary>
    /// Options for any verb that operates on dicom datasets (either from mongo, from file etc).
    /// </summary>
    public abstract class IsIdentifiableDicomOptions: IsIdentifiableAbstractOptions
    {
        [Option(HelpText = "Optional. Generate a tree storage report which represents failures according to their position in the DicomDataset.")]
        public bool TreeReport { get; set; }
    }
}