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

    [Verb("dicom-loader",HelpText = "Load DICOM files into MongoDB")]
    public sealed class DicomLoaderVerb : ApplicationVerbBase { }

    [Verb("dynamic-rules-tester", HelpText = "Tester for DynamicRule configurations")]
    public sealed class DynamicRulesTesterVerb : ApplicationVerbBase { }

    [Verb("dicom-directory-processor", HelpText = "Queue dicom files on disk for ETL")]
    public sealed class DicomDirectoryProcessorVerb : ApplicationVerbBase { }

    [Verb("extract-images", HelpText = "Launch an image extraction job")]
    public sealed class ExtractImagesVerb : ApplicationVerbBase { }

    [Verb("trigger-updates", HelpText = "Queue system wide database updates to specific fields e.g. changes in PatientID, Tag Promotion etc")]
    public sealed class TriggerUpdatesVerb : ApplicationVerbBase { }

    [Verb("setup", HelpText = "Tool for assisting in the setup and checking of SmiServices configurations")]
    public sealed class SetupVerb : ApplicationVerbBase { }
    #endregion

    #region Microservices

    public abstract class MicroservicesVerbBase : VerbBase
    {
        protected new const string BaseHelpText = VerbBase.BaseHelpText + "src/microservices/Microservices.";
    }

    [Verb("cohort-extractor", HelpText = "Microservice for queuing images for extraction")]
    public sealed class CohortExtractorVerb : MicroservicesVerbBase { }

    [Verb("cohort-packager", HelpText = "Microservice for detecting when all images in an extraction have been produced/validated")]
    public sealed class CohortPackagerVerb : MicroservicesVerbBase { }

    [Verb("dicom-anonymiser", HelpText = "Microservice for anonymising DICOM files")]
    public sealed class DicomAnonymiserVerb : MicroservicesVerbBase { }

    [Verb("dicom-relational-mapper", HelpText = "Microservice for loading relational database with images queued by dicom-reprocessor")]
    public sealed class DicomRelationalMapperVerb : MicroservicesVerbBase { }

    [Verb("dicom-reprocessor", HelpText = "Queue images stored in a MongoDb unstructured database for ETL to a relational database")]
    public sealed class DicomReprocessorVerb : MicroservicesVerbBase { }

    [Verb("dicom-tag-reader", HelpText = "Microservice for loading dicom images (file path + tags) off disk and into a RabbitMQ queue for downstream microservices e.g. for loading into a database")]
    public sealed class DicomTagReaderVerb : MicroservicesVerbBase { }

    [Verb("file-copier", HelpText = "Extraction microservice that copies requested images directly to an output location (without any anonymisation).  Runs down stream from cohort-extractor")]
    public sealed class FileCopierVerb : MicroservicesVerbBase { }

    [Verb("identifier-mapper", HelpText = "Microservice for substituting PatientID for an anonymous representation e.g. before loading to a relational database")]
    public sealed class IdentifierMapperVerb : MicroservicesVerbBase { }

    [Verb("is-identifiable", HelpText = "Evaluates database table(s), flat files or dicom files for identifiable data.")]
    public sealed class IsIdentifiableVerb : MicroservicesVerbBase { }

    [Verb("mongodb-populator", HelpText = "Microservice for loading queued dicom images into a MongoDb unstructured database")]
    public sealed class MongoDbPopulatorVerb : MicroservicesVerbBase { }

    [Verb("update-values", HelpText = "Microservice for applying system wide SQL UPDATE commands for updates queued by trigger-updates")]
    public sealed class UpdateValuesVerb : MicroservicesVerbBase { }

    #endregion
}
