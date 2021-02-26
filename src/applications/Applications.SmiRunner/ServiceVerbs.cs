using CommandLine;


namespace Applications.SmiRunner
{
    public abstract class VerbBase
    {
        protected const string BaseHelpText = "See here at your release version: https://github.com/SMI/SmiServices/tree/master/";
    }

    #region Applications

    public abstract class ApplicationVerbBase : VerbBase
    {
        protected new const string BaseHelpText = VerbBase.BaseHelpText + "src/applications/Applications.";
    }

    [Verb("dicom-directory-processor", HelpText = BaseHelpText + "DicomDirectoryProcessor")]
    public sealed class DicomDirectoryProcessorVerb : ApplicationVerbBase { }

    [Verb("is-identifiable-reviewer", HelpText = BaseHelpText + "IsIdentifiableReviewer")]
    public sealed class IsIdentifiableReviewerVerb : ApplicationVerbBase { }

    [Verb("trigger-updates", HelpText = BaseHelpText + "TriggerUpdates")]
    public sealed class TriggerUpdatesVerb : ApplicationVerbBase { }

    #endregion

    #region Microservices

    public abstract class MicroservicesVerbBase : VerbBase
    {
        protected new const string BaseHelpText = VerbBase.BaseHelpText + "src/microservices/Microservices.";
    }

    [Verb("cohort-extractor", HelpText = BaseHelpText + "CohortExtractor")]
    public sealed class CohortExtractorVerb : MicroservicesVerbBase { }

    [Verb("cohort-packager", HelpText = BaseHelpText + "CohortPackager")]
    public sealed class CohortPackagerVerb : MicroservicesVerbBase { }

    [Verb("dead-letter-reprocessor", HelpText = BaseHelpText + "DicomTagReader")]
    public sealed class DeadLetterReprocessorVerb : MicroservicesVerbBase { }

    [Verb("dicom-relational-mapper", HelpText = BaseHelpText + "DicomRelationalMapper")]
    public sealed class DicomRelationalMapperVerb : MicroservicesVerbBase { }

    [Verb("dicom-reprocessor", HelpText = BaseHelpText + "DicomReprocessor")]
    public sealed class DicomReprocessorVerb : MicroservicesVerbBase { }

    [Verb("dicom-tag-reader", HelpText = BaseHelpText + "DicomTagReader")]
    public sealed class DicomTagReaderVerb : MicroservicesVerbBase { }

    [Verb("file-copier", HelpText = BaseHelpText + "FileCopier")]
    public sealed class FileCopierVerb : MicroservicesVerbBase { }

    [Verb("identifier-mapper", HelpText = BaseHelpText + "IdentifierMapper")]
    public sealed class IdentifierMapperVerb : MicroservicesVerbBase { }

    [Verb("is-identifiable", HelpText = BaseHelpText + "IsIdentifiable")]
    public sealed class IsIdentifiableVerb : MicroservicesVerbBase { }

    [Verb("mongodb-populator", HelpText = BaseHelpText + "MongoDbPopulator")]
    public sealed class MongoDbPopulatorVerb : MicroservicesVerbBase { }

    [Verb("update-values", HelpText = BaseHelpText + "UpdateValues")]
    public sealed class UpdateValuesVerb : MicroservicesVerbBase { }

    #endregion
}
