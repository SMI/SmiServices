Refactor and simplify extraction reports

-   Generate a single `FailureStoreReport` which can be further processed depending on the need
-   Removed `ReportFormat` and `ReporterType` from `CohortPackagerOptions`
-   Merge `JobReporterBase` and `FileReporter` into `JobReporter`
