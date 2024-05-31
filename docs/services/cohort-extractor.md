# Cohort Extractor

This service consumes `ExtractionRequestMessage`s produced by [extract-images](../applications/extract-images.md) and identifies the images corresponding to the specified key values (e.g. each specified `SeriesInstanceUID`) by querying a database of DICOM metadata. One output message is produced per matched file in order for a downstream service to generate a de-identified file. or a full copy in the case of an identifiable extraction. One message is also produced per input identifier which specifies all files that are expected to be produced.

There can be multiple datasets in which matching images should be sourced e.g., MR / CT which could even reside on different servers. Datasets are identified and distinguished from one another through RDMP `ICatalogue`S which must already exist as part of the data load process.

## Message Flow

| Read/Write | Message Type                     | Config Property                                        |
| ---------- | -------------------------------- | ------------------------------------------------------ |
| Read       | ExtractionRequestMessage         | CohortExtractorOptions                                 |
| Write      | ExtractFileMessage               | CohortExtractorOptions.ExtractFilesProducerOptions     |
| Write      | ExtractFileCollectionInfoMessage | CohortExtractorOptions.ExtractFilesInfoProducerOptions |

## YAML Configuration

| Key                      | Purpose                                                                                                                                      |
| ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `CohortExtractorOptions` | Main configuration for this service                                                                                                          |
| `RabbitOptions`          | RabbitMQ connection options                                                                                                                  |
| `RDMPOptions`            | RDMP platform database connection options, which keeps track of load configurations, available datasets to extract images from (tables) etc. |

In the `CohortExtractorOptions` key:

-   Pick an implementation of `IAuditExtractions` e.g. `Microservices.CohortExtractor.Audit.NullAuditExtractions` and enter the full Type name into default.yaml `AuditorType`
-   Pick an implementation of `IExtractionRequestFulfiller` e.g. `Microservices.CohortExtractor.Execution.RequestFulfillers.FromCataloguesExtractionRequestFulfiller` and enter the full Type name into default.yaml `RequestFulfillerType`.
-   Specify the mapping RDMP catalogue database
-   Optionally specify a list of Catalogue IDs in CataloguesToExtractFrom (or set it to \* to use any). Depending on your `IExtractionRequestFulfiller` this value might be ignored.

### Fulfiller

The set of images that **could** be extracted is controlled by the `IExtractionRequestFulfiller`.

The current recommended implementation is FromCataloguesExtractionRequestFulfiller. This fulfiller will look up one or more tables or multi table joins (Catalogues) and search for the provided extraction key (e.g. SeriesInstanceUID = x)

The matched records are what will be reported on e.g. "for x UIDs we found y available images". From this result set a subset will be rejected (because you have made row level decisions not to extract particular images). This is handled by the [Rejector]

Configure the fulfiller in your options yaml:

```yaml
OnlyCatalogues: 1,2,3
RequestFulfillerType: Microservices.CohortExtractor.Execution.RequestFulfillers.FromCataloguesExtractionRequestFulfiller
```

### Rejector

Records matched by the Fulfiller are passed to the `IRejector` (if any are configured). This class can make last minute decisions on a row by row level to either extract or forbid (with a specific provided reason) the extraction of an image.

The currently recommended implementation is DynamicRejector. To use the dynamic rejector edit your options yaml as follows:

```yaml
RejectorType: Microservices.CohortExtractor.Execution.RequestFulfillers.Dynamic.DynamicRejector
```

Using the DynamicRejector also requires you to configure a file DynamicRules.txt in the execution directory of your binary. Rules are written in C# and can only index fields that appear in the records returned by the Fulfiller. An example DynamicRules file looks like:

```txt
// This script is run when the IRejector is set to DynamicRejector ('#' symbols are not supported in this script, use CSharp syntax)

if (!System.Convert.ToBoolean(row["IsOriginal"]))
{
    return "ImageType is not ORIGINAL";
}

if (!System.Convert.ToBoolean(row["IsPrimary"]))
{
    return "ImageType is not PRIMARY";
}

//if the image is not extractable
if (!System.Convert.ToBoolean(row["IsExtractableToDisk"]))
{
    //tell them why and reject it
    return row["IsExtractableToDisk_Reason"] as string;
}

return null;
```

## CLI Options

N/A.
