Add support for extraction processing failures
-	Remove `IsIdentifiable` field from `ExtractedFileVerificationMessage` and replace with new `VerifiedFileStatus` enum
-	Update extraction job classes in `CohortPackager` to store this new field, and handle backwards-incompatibility when reading older extraction logs