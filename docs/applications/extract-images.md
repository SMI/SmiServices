## Extract Images

This application is used to launch a file extraction request using a file of DICOM UIDs.

## Message Flow

| Read/Write | Message Type                   | Config Property                                             |
| ---------- | ------------------------------ | ----------------------------------------------------------- |
| Write      | `ExtractionRequestMessage`     | `ExtractImagesOptions.ExtractionRequestProducerOptions`     |
| Write      | `ExtractionRequestInfoMessage` | `ExtractImagesOptions.ExtractionRequestInfoProducerOptions` |

## YAML Configuration

| Key                    | Purpose                             |
| ---------------------- | ----------------------------------- |
| `ExtractImagesOptions` | Main configuration for this service |
| `RabbitOptions`        | RabbitMQ connection options         |

## CLI Options

In addition to any common options:

```console
  -p, --project-id                 Required. The project identifier
  -c, --cohort-csv-file            Required. The CSV file containing IDs of the cohort for extraction
  -m, --modalities                 [Optional] List of modalities to extract. Any non-matching IDs from the input list are ignored
  -i, --identifiable-extraction    Extract without performing anonymisation
  -f, --no-filters                 Extract without applying any rejection filters
  -n, --non-interactive            Don't pause for manual confirmation before sending messages
```

## Example Usage

```console
./smi extract-images \
    -y config.yaml \
    -p 1234-5678 \
    -m CT \
    -n \
    -c proj_ids.csv
```
