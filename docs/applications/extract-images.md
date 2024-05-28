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

It publishes images to two exchanges specified by the `ExtractionRequestProducerOptions` and `ExtractionRequestInfoProducerOptions` config options.
