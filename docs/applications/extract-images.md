## Extract Images

This application is used to launch a file extraction request using a file of DICOM UIDs. In addition to any common CLI options, the following arguments are supported:

```console
  -p, --project-id                 Required. The project identifier
  -c, --cohort-csv-file            Required. The CSV file containing IDs of the cohort for extraction
  -m, --modalities                 [Optional] List of modalities to extract. Any non-matching IDs from the input list are ignored
  -i, --identifiable-extraction    Extract without performing anonymisation
  -f, --no-filters                 Extract without applying any rejection filters
  -n, --non-interactive            Don't pause for manual confirmation before sending messages
```

## YAML Configuration

Uses the `ExtractImagesOptions` config key. Messages are published to two exchanges specified by:

-   `ExtractionRequestProducerOptions` - Messages containing the input identifiers for consumption by e.g., [cohort-extractor](../services/cohort-extractor.md)
-   `ExtractionRequestInfoProducerOptions` - Messages containing extraction summary info for consumption by e.g., [cohort-packager](../services/cohort-packager.md)
