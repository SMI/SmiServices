# Cohort Packager

Collects all information regarding an extraction job, and monitors the filesystem for the anonymised files. Persists all information to a MongoDB collection.

Produces validation reports for each extraction suitable for review by research coordinators before the extraction files are released. Reports are created automatically when an extraction is detected as being complete, and can also be manually recreated on the CLI by passing the `-r` or `--recreate-reports` flag with the corresponding extraction GUID.

For a standard extraction, 4 files are produced:

- `README.md` - A summary file containing metadata about the extraction job
- `rejected_files.csv` - A list of any requested IDs which generated a rejection (a file was blocked etc.)
- `processing_errors.csv` - A summary of any errors from the anonymiser or other components in the pipeline. This report should be inspected by a developer before data are released
- `verification_failures.csv` - A full listing of all `Failure`s generated by IsIdentifiable when scanning files after anonymisation

For an identifiable extraction, the `verification_failures.csv` is not produced.

## Message Flow

| Read/Write | Message Type                       | Config Property                                   |
| ---------- | ---------------------------------- | ------------------------------------------------- |
| Read       | `ExtractRequestInfoMessage`        | `CohortPackagerOptions.ExtractRequestInfoOptions` |
| Read       | `FileCollectionInfoMessage`        | `CohortPackagerOptions.FileCollectionInfoOptions` |
| Read       | `ExtractedFileStatusMessage`       | `CohortPackagerOptions.NoVerifyStatusOptions`     |
| Read       | `ExtractedFileVerificationMessage` | `CohortPackagerOptions.VerificationStatusOptions` |

## YAML Configuration

| Key                                     | Purpose                                                |
| --------------------------------------- | ------------------------------------------------------ |
| `CohortPackagerOptions`                 | Main configuration for this service                    |
| `RabbitOptions`                         | RabbitMQ connection options                            |
| `MongoDatabases.ExtractionStoreOptions` | Database connection info for the extraction data store |

## CLI Options

N/A.