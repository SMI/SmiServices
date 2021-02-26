using CommandLine;

namespace SmiRunner
{
    // TODO test these all resolve
    public abstract class VerbBase
    {
        protected const string BaseHelpText = "See here at your release tag: https://github.com/SMI/SmiServices/tree/master/";
    }

    #region Applications

    [Verb("trigger-updates", HelpText = BaseHelpText + "src/applications/Applications.TriggerUpdates")]
    public sealed class TriggerUpdates : VerbBase { }

    #endregion

    [Verb("dicom-tag-reader", HelpText = BaseHelpText + "src/microservices/Microservices.DicomTagReader")]
    public sealed class DicomTagReader : VerbBase { }
}
